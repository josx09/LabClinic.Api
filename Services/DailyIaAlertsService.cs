using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LabClinic.Api.Data;
using LabClinic.Api.Services;

namespace LabClinic.Api.Services
{
    /// <summary>
    /// Revisa insumos bajos por sucursal y envía un correo diario a la(s) dirección(es) configuradas.
    /// Corre a la hora y minuto definidos en appsettings (por defecto 08:00).
    /// </summary>
    public class DailyIaAlertsService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<DailyIaAlertsService> _log;

        public DailyIaAlertsService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<DailyIaAlertsService> log)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = _config.GetValue<bool?>("IaJobs:DailyAlerts:Enabled") ?? true;
            if (!enabled)
            {
                _log.LogInformation("[DailyIaAlerts] Deshabilitado por configuración.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var (nextRun, delay) = GetNextRunUtc();
                _log.LogInformation("[DailyIaAlerts] Próxima ejecución: {NextRunLocal} (local), en {Delay}.",
                    nextRun.ToLocalTime(), delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException) { break; }

                // Ejecutar job
                try
                {
                    await RunOnce(stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[DailyIaAlerts] Error en ejecución.");
                }
            }
        }

        private (DateTime nextRunUtc, TimeSpan delay) GetNextRunUtc()
        {
            var tzNow = DateTime.Now;
            var hour = _config.GetValue<int?>("IaJobs:DailyAlerts:Hour") ?? 8;
            var minute = _config.GetValue<int?>("IaJobs:DailyAlerts:Minute") ?? 0;

            var nextLocal = new DateTime(tzNow.Year, tzNow.Month, tzNow.Day, hour, minute, 0);
            if (nextLocal <= tzNow) nextLocal = nextLocal.AddDays(1);

            var nextUtc = nextLocal.ToUniversalTime();
            var delay = nextUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(5);
            return (nextUtc, delay);
        }

        private async Task RunOnce(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // 1) Sucursales a notificar
            //   a) Primero usamos el mapeo de appsettings (Recipients)
            //   b) Si no hay mapeo, intentamos deducir sucursales desde la BD (Clinicas o Insumos)
            var recipSection = _config.GetSection("IaJobs:DailyAlerts:Recipients");
            var recipientsMap = recipSection.GetChildren()
                                            .ToDictionary(s => s.Key, s => (s.Value ?? "").Trim());

            var fallbackTo = _config["IaJobs:DailyAlerts:FallbackTo"] ?? "";
            var subjectPrefix = _config["IaJobs:DailyAlerts:SubjectPrefix"] ?? "Alerta de Insumos";

            List<int> sucursales;
            if (recipientsMap.Count > 0)
            {
                // Claves del mapeo (ej. "1", "2", ...)
                sucursales = recipientsMap.Keys
                    .Select(k => int.TryParse(k, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // Deducción por BD: usar sucursales presentes en Insumos / Clinicas
                sucursales = await db.Insumos
                    .Select(i => i.IdSucursal)
                    .Distinct()
                    .Where(id => id != null)
                    .Select(id => id!.Value)
                    .ToListAsync(ct);

                if (!sucursales.Any())
                {
                    // Fallback: si no hay datos, asumimos sucursal 1
                    sucursales = new List<int> { 1 };
                }
            }

            foreach (var sucursalId in sucursales)
            {
                try
                {
                    var low = await db.Insumos
                        .Where(i => i.IdSucursal == sucursalId
                                    && i.StockMinimo > 0
                                    && i.Stock <= i.StockMinimo)
                        .Select(i => new { i.Nombre, i.Stock, i.StockMinimo })
                        .OrderBy(i => i.Stock - i.StockMinimo)
                        .ToListAsync(ct);

                    // Armar correo (siempre enviamos, haya o no low stock)
                    var (plain, html) = BuildEmail(low, sucursalId);

                    // Resolver destinatarios
                    var to = ResolveRecipients(recipientsMap, fallbackTo, sucursalId);
                    if (string.IsNullOrWhiteSpace(to))
                    {
                        _log.LogWarning("[DailyIaAlerts] No se configuró destinatario para sucursal {SucursalId}.", sucursalId);
                        continue;
                    }

                    var subject = $"{subjectPrefix} – Sucursal {sucursalId}";
                    foreach (var single in SplitEmails(to))
                    {
                        await email.SendAsync(single, subject, plain, html, ct);
                        _log.LogInformation("[DailyIaAlerts] Enviado a {To} (suc {SucursalId}).", single, sucursalId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "[DailyIaAlerts] Error enviando alerta para sucursal {SucursalId}.", sucursalId);
                }
            }
        }

        private static (string plain, string html) BuildEmail<T>(
    IEnumerable<T> low, int sucursalId)
        {
            var list = low.Cast<dynamic>().ToList();

            if (list.Count == 0)
            {
                var p = $"✅ Sin insumos con stock bajo.\nSucursal: {sucursalId}\nFecha: {DateTime.Now:dd/MM/yyyy HH:mm}";
                var h = $@"<p>✅ <b>Sin insumos con stock bajo</b></p>
                <p><b>Sucursal:</b> {sucursalId}<br/>
                <b>Fecha:</b> {DateTime.Now:dd/MM/yyyy HH:mm}</p>";
                return (p, h);
            }

            var sbPlain = new StringBuilder()
                .AppendLine($"⚠️ Alerta de insumos con stock bajo – Sucursal {sucursalId}")
                .AppendLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .AppendLine();

            foreach (var i in list)
                sbPlain.AppendLine($"- {i.Nombre}: {i.Stock}/{i.StockMinimo}");

            var rows = string.Join("",
                list.Select(i => $"<tr><td>{i.Nombre}</td><td>{i.Stock}</td><td>{i.StockMinimo}</td></tr>"));

            var html = $@"
            <h3>⚠️ Alerta de Insumos con Stock Bajo – Sucursal {sucursalId}</h3>
            <p><b>Fecha:</b> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            <table border='1' cellpadding='6' cellspacing='0'>
              <tr><th>Insumo</th><th>Stock</th><th>Mínimo</th></tr>
              {rows}
            </table>";

            return (sbPlain.ToString(), html);
        }


        private static string ResolveRecipients(
            Dictionary<string, string> map, string fallback, int sucursalId)
        {
            // Primero buscar clave exacta
            var key = sucursalId.ToString();
            if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            // Si no hay mapeo, usar fallback
            return fallback ?? string.Empty;
        }

        private static IEnumerable<string> SplitEmails(string value)
        {
            return value.Split(',', ';')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x));
        }
    }
}
