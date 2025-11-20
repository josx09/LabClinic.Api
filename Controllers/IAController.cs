using Microsoft.AspNetCore.Mvc;
using LabClinic.Api.Common;
using LabClinic.Api.Data;
using LabClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using LabClinic.Api.Services;
using LabClinic.Api.Models.Dtos;


namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/ia")]
    [Authorize]
    public class IAController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx;
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly IEmailService _email;

        public IAController(
            LabDbContext db,
            ISucursalContext sucCtx,
            IHttpClientFactory factory,
            IConfiguration config,
            IEmailService email)
        {
            _db = db;
            _sucCtx = sucCtx;
            _http = factory.CreateClient();
            _apiKey = config["GoogleGemini:ApiKey"] ?? "";
            _email = email;
        }

        // ============================
        // 1) CHAT (solo lectura)
        // ============================
        [HttpPost("chat")]
        [Authorize(Policy = "IA.Read")]
        public async Task<IActionResult> Chat([FromBody] System.Text.Json.JsonElement data, [FromServices] IEmailService emailService)
        {
            string prompt = data.TryGetProperty("prompt", out var p) ? (p.GetString() ?? "") : "";

            if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest(new { message = "Escribe una pregunta o solicitud." });

            var lower = prompt.ToLower();

            // ===========================================
            // 🔹 DETECCIÓN INTELIGENTE DE INTENCIONES
            // ===========================================
            bool pideReporteInsumos =
                (lower.Contains("reporte") || lower.Contains("informe") || lower.Contains("resumen")) &&
                (lower.Contains("insumo") || lower.Contains("reactivo") || lower.Contains("consumo")) &&
                (lower.Contains("hoy") || lower.Contains("día") || lower.Contains("diario"));

            bool pideEnvioCorreo =
                (lower.Contains("envi") || lower.Contains("mand") || lower.Contains("correo") || lower.Contains("email"));

            // ===========================================
            // 🔹 CASO: Solicitud de reporte de insumos
            // ===========================================
            if (pideReporteInsumos)
            {
                string correo = "josx09@gmail.com"; // ✅ Valor por defecto
                try
                {
                    // Detectar posible correo en el prompt
                    var match = System.Text.RegularExpressions.Regex.Match(prompt, @"[\w\.-]+@[\w\.-]+\.\w+");
                    if (match.Success)
                        correo = match.Value;
                }
                catch { }

                var registros = await _db.RegistroInsumoUsos
                    .Include(r => r.Insumo)
                    .Include(r => r.Examen).ThenInclude(e => e.TipoExamen)
                    .Where(r => r.Fecha.Date == DateTime.Today)
                    .Where(r => r.IdSucursal == _sucCtx.CurrentSucursalId)
                    .OrderByDescending(r => r.Fecha)
                    .ToListAsync();

                if (!registros.Any())
                    return Ok(new { respuesta = "📭 No se encontraron insumos usados hoy." });

                var body = new EmailReporteDto
                {
                    correo = correo,
                    asunto = "Reporte diario de insumos usados",
                    titulo = $"Insumos utilizados el {DateTime.Now:dd/MM/yyyy}",
                    items = registros.Select(r => new EmailItemDto
                    {
                        fecha = r.Fecha.ToString("yyyy-MM-dd HH:mm"),
                        insumo = r.Insumo?.Nombre ?? "(Desconocido)",
                        cantidad = (int)r.CantidadUsada,
                        tipoExamen = r.Examen?.TipoExamen?.Nombre ?? "(Uso manual)",
                        justificacion = r.Justificacion ?? "—"
                    }).ToList()
                };

                await emailService.SendAsync(
                    body.correo,
                    body.asunto,
                    $"Reporte de insumos: {body.titulo}",
                    $@"
            <h2 style='color:#0d6efd;font-family:Segoe UI'>📦 {body.titulo}</h2>
            <table style='border-collapse:collapse;width:100%;font-size:13px;font-family:Arial;'>
                <thead>
                    <tr style='background:#0d6efd;color:white;text-align:left;'>
                        <th style='padding:6px;border:1px solid #ccc;'>Fecha</th>
                        <th style='padding:6px;border:1px solid #ccc;'>Insumo</th>
                        <th style='padding:6px;border:1px solid #ccc;'>Cantidad</th>
                        <th style='padding:6px;border:1px solid #ccc;'>Examen</th>
                        <th style='padding:6px;border:1px solid #ccc;'>Justificación</th>
                    </tr>
                </thead>
                <tbody>
                    {string.Join("", body.items.Select(i => $@"
                        <tr style='background:#f8f9fa;'>
                            <td style='padding:6px;border:1px solid #ddd;'>{i.fecha}</td>
                            <td style='padding:6px;border:1px solid #ddd;'>{i.insumo}</td>
                            <td style='padding:6px;border:1px solid #ddd;'>{i.cantidad}</td>
                            <td style='padding:6px;border:1px solid #ddd;'>{i.tipoExamen}</td>
                            <td style='padding:6px;border:1px solid #ddd;'>{i.justificacion}</td>
                        </tr>
                    "))}
                </tbody>
            </table>
            <p style='font-size:12px;color:gray;margin-top:12px;'>📨 Reporte generado automáticamente por <b>LabClinic</b>.</p>",
                    CancellationToken.None
                );

                return Ok(new { respuesta = $"📤 Reporte de insumos enviado a {correo}" });
            }

            // ===========================================
            // 🔹 Caso general: Enviar al modelo Gemini
            // ===========================================
            string respuestaIA = await LlamarGemini(prompt);

            _db.HistorialIA.Add(new HistorialIA
            {
                Prompt = prompt,
                Respuesta = respuestaIA,
                Fecha = DateTime.Now,
                SucursalId = _sucCtx.CurrentSucursalId
            });
            await _db.SaveChangesAsync();

            return Ok(new { respuesta = respuestaIA });
        }


        [HttpPost("test-email")]
        [Authorize(Policy = "IA.Admin")] // 🔒 Solo administradores
        public async Task<IActionResult> TestEmail()
        {
            await _email.SendAsync(
                "josx009@gmail.com",
                "✅ Prueba SMTP Gmail",
                "Prueba desde LabClinic.API (texto plano)",
                "<b>Prueba desde LabClinic.API con Gmail funcionando correctamente ✅</b>"
            );

            return Ok(new { message = "Correo enviado correctamente desde Gmail ✅" });
        }
        // ============================
        // 2) Resumen semanal (JSON)
        // ============================
        [HttpGet("resumen-semanal")]
        [Authorize(Policy = "IA.Read")]
        public async Task<IActionResult> ResumenSemanalJson([FromQuery] int dias = 7)
        {
            var sucursalId = _sucCtx.CurrentSucursalId;
            if (dias < 1) dias = 7;
            var desde = DateTime.Today.AddDays(-dias);
            var hasta = DateTime.Today.AddDays(1);

            int personas = await _db.Personas.CountAsync(p => p.IdSucursal == sucursalId);

            var examenesQ = _db.Examenes
                .Include(e => e.TipoExamen)
                .Include(e => e.Persona)
                .Where(e => e.IdSucursal == sucursalId && e.FechaRegistro >= desde && e.FechaRegistro < hasta);

            int examenesTotal = await examenesQ.CountAsync();

            var topTipos = await examenesQ
                .GroupBy(e => e.TipoExamen!.Nombre)
                .Select(g => new { Tipo = g.Key, Total = g.Count() })
                .OrderByDescending(g => g.Total)
                .Take(7)
                .ToListAsync();

            var topPaciente = await examenesQ
                .GroupBy(e => (e.Persona!.Nombre + " " + e.Persona!.Apellido).Trim())
                .Select(g => new { Paciente = string.IsNullOrWhiteSpace(g.Key) ? "(Sin nombre)" : g.Key, Total = g.Count() })
                .OrderByDescending(g => g.Total)
                .FirstOrDefaultAsync() ?? new { Paciente = "(sin datos)", Total = 0 };

            var pagosQ = _db.Pagos.Where(p => p.IdSucursal == sucursalId && p.FechaPago >= desde && p.FechaPago < hasta);
            decimal montoPagado = await pagosQ.SumAsync(p => (decimal?)p.MontoPagado) ?? 0;

            int citasTotal = await _db.Citas.CountAsync(c => c.IdSucursal == sucursalId && c.Fecha >= desde && c.Fecha < hasta);
            int clinicas = await _db.Clinicas.CountAsync(c => c.IdSucursal == sucursalId);

            var insumosBajoStock = await _db.Insumos
                .Where(i => i.IdSucursal == sucursalId && i.StockMinimo > 0 && i.Stock <= i.StockMinimo)
                .Select(i => new { i.Nombre, i.Stock, i.StockMinimo })
                .ToListAsync();

            return Ok(new
            {
                sucursalId,
                periodo = new { desde, hasta = hasta.AddSeconds(-1) },
                personas,
                examenes = new { total = examenesTotal, topTipos, topPaciente },
                pagos = new { monto = montoPagado },
                citas = new { total = citasTotal },
                clinicas = new { total = clinicas },
                insumos = new { alerta = insumosBajoStock.Count, bajoStock = insumosBajoStock }
            });
        }

        // ============================
        // 3) Acciones (escritura controlada)
        // ============================
        public record IAActionRequest(string action, JsonElement? data);
        public record EmailTarget(string to);

        [HttpPost("acciones")]
        [Authorize(Policy = "IA.Write")]
        public async Task<IActionResult> Acciones([FromBody] IAActionRequest req)
        {
            var sucursalId = _sucCtx.CurrentSucursalId;
            var action = (req.action ?? "").Trim().ToLowerInvariant();

            switch (action)
            {
                case "enviar-resumen-semanal-ahora":
                    {
                        // destinatario en body: { "action": "...", "data": { "to": "destino@correo.com", "dias": 7 } }
                        var to = req.data is { } && req.data.Value.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? "" : "";
                        var dias = req.data is { } && req.data.Value.TryGetProperty("dias", out var dEl) && dEl.TryGetInt32(out var d) ? Math.Max(1, d) : 7;
                        if (string.IsNullOrWhiteSpace(to))
                            return BadRequest(new { message = "Falta 'to' en data." });

                        var (plain, html) = await BuildResumenSemanalEmailAsync(sucursalId, dias);
                        await _email.SendAsync(to, $"Resumen semanal – Sucursal {sucursalId}", plain, html);
                        return Ok(new { ok = true, message = $"Resumen enviado a {to}" });
                    }

                case "alertar-insumos-bajos-ahora":
                    {
                        var to = req.data is { } && req.data.Value.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(to))
                            return BadRequest(new { message = "Falta 'to' en data." });

                        var (plain, html) = await BuildAlertaInsumosEmailAsync(sucursalId);
                        await _email.SendAsync(to, $"Alerta de insumos – Sucursal {sucursalId}", plain, html);
                        return Ok(new { ok = true, message = $"Alerta de insumos enviada a {to}" });
                    }

                default:
                    return BadRequest(new { message = $"Acción no soportada: {action}" });
            }
        }

        // ============================
        // Helpers (construcción de resúmenes)
        // ============================
        private async Task<(string plain, string html)> BuildResumenSemanalEmailAsync(int sucursalId, int dias)
        {
            var desde = DateTime.Today.AddDays(-dias);
            var hasta = DateTime.Today.AddDays(1);

            int personas = await _db.Personas.CountAsync(p => p.IdSucursal == sucursalId);

            var examenesQ = _db.Examenes
                .Include(e => e.TipoExamen)
                .Include(e => e.Persona)
                .Where(e => e.IdSucursal == sucursalId && e.FechaRegistro >= desde && e.FechaRegistro < hasta);

            int examenesTotal = await examenesQ.CountAsync();

            var topTipos = await examenesQ
                .GroupBy(e => e.TipoExamen!.Nombre)
                .Select(g => new { Tipo = g.Key, Total = g.Count() })
                .OrderByDescending(g => g.Total)
                .Take(5)
                .ToListAsync();

            var pagosQ = _db.Pagos.Where(p => p.IdSucursal == sucursalId && p.FechaPago >= desde && p.FechaPago < hasta);
            decimal montoPagado = await pagosQ.SumAsync(p => (decimal?)p.MontoPagado) ?? 0;

            int citasTotal = await _db.Citas.CountAsync(c => c.IdSucursal == sucursalId && c.Fecha >= desde && c.Fecha < hasta);

            var plain = new StringBuilder()
                .AppendLine($"Resumen semanal – Sucursal {sucursalId}")
                .AppendLine($"Periodo: {desde:yyyy-MM-dd} a {hasta.AddDays(-1):yyyy-MM-dd}")
                .AppendLine($"👥 Personas: {personas}")
                .AppendLine($"🧪 Exámenes: {examenesTotal}")
                .AppendLine($"💰 Pagos: Q{montoPagado:0.00}")
                .AppendLine($"📅 Citas: {citasTotal}")
                .AppendLine("🏆 Top tipos:")
                .AppendLine(string.Join(Environment.NewLine, topTipos.Select(t => $"- {t.Tipo}: {t.Total}")))
                .ToString();

            var html = $@"
<h3>Resumen semanal – Sucursal {sucursalId}</h3>
<p><b>Periodo:</b> {desde:yyyy-MM-dd} a {hasta.AddDays(-1):yyyy-MM-dd}</p>
<ul>
  <li>👥 Personas: <b>{personas}</b></li>
  <li>🧪 Exámenes: <b>{examenesTotal}</b></li>
  <li>💰 Pagos: <b>Q{montoPagado:0.00}</b></li>
  <li>📅 Citas: <b>{citasTotal}</b></li>
</ul>
<p><b>🏆 Top tipos:</b></p>
<ul>
  {string.Join("", topTipos.Select(t => $"<li>{t.Tipo}: <b>{t.Total}</b></li>"))}
</ul>";

            return (plain, html);
        }

        private async Task<(string plain, string html)> BuildAlertaInsumosEmailAsync(int sucursalId)
        {
            var low = await _db.Insumos
                .Where(i => i.IdSucursal == sucursalId && i.StockMinimo > 0 && i.Stock <= i.StockMinimo)
                .Select(i => new { i.Nombre, i.Stock, i.StockMinimo })
                .OrderBy(i => i.Stock - i.StockMinimo)
                .ToListAsync();

            if (!low.Any())
            {
                var p = "No hay insumos con stock bajo.";
                var h = "<p>No hay insumos con stock bajo.</p>";
                return (p, h);
            }

            var plain = "⚠️ Alerta de insumos con stock bajo:\n" +
                        string.Join("\n", low.Select(i => $"- {i.Nombre}: {i.Stock}/{i.StockMinimo}"));

            var html = "<h3>⚠️ Alerta de insumos con stock bajo</h3><ul>" +
                       string.Join("", low.Select(i => $"<li>{i.Nombre}: <b>{i.Stock}/{i.StockMinimo}</b></li>")) +
                       "</ul>";

            return (plain, html);
        }

        // ============================
        // Analítica "rápida"
        // ============================
        private async Task<string> EjecutarAnalisisAmpliada(string prompt, int sucursalId)
        {
            var p = (prompt ?? "").Trim().ToLowerInvariant();

            if (p.Contains("personas") || p.Contains("pacientes"))
            {
                if (p.Contains("cuánt"))
                {
                    int count = await _db.Personas.CountAsync(x => x.IdSucursal == sucursalId);
                    return $"👥 Total de personas registradas: {count}";
                }
                if (p.Contains("hoy"))
                {
                    var hoy = DateTime.Today;
                    int hoyCount = await _db.Personas.CountAsync(x => x.IdSucursal == sucursalId && x.FechaRegistro >= hoy);
                    return $"👤 Altas de personas hoy: {hoyCount}";
                }
            }

            if (p.Contains("examen"))
            {
                if (p.Contains("cuánt"))
                {
                    int count = await _db.Examenes.CountAsync(e => e.IdSucursal == sucursalId);
                    return $"🧪 Total de exámenes registrados: {count}";
                }
            }

            if (p.Contains("pago") || p.Contains("ingres"))
            {
                decimal total = await _db.Pagos
                    .Where(x => x.IdSucursal == sucursalId)
                    .SumAsync(x => (decimal?)x.MontoPagado) ?? 0;
                return $"💵 Total pagado: Q{total:0.00}";
            }

            if (p.Contains("cita"))
            {
                int count = await _db.Citas.CountAsync(x => x.IdSucursal == sucursalId);
                return $"📅 Citas registradas: {count}";
            }

            if (p.Contains("insumo"))
            {
                int count = await _db.Insumos.CountAsync(x => x.IdSucursal == sucursalId);
                return $"🧴 Insumos registrados: {count}";
            }

            return "";
        }

        private static string GetSystemMessageForRole(string rol)
        {
            rol = (rol ?? "").Trim().ToLowerInvariant();
            return rol switch
            {
                "médico" or "medico" => "Eres un asistente médico del laboratorio. Explica resultados con criterio clínico.",
                "recepcionista" => "Eres un asistente de recepción. Ayuda con atención a pacientes y citas.",
                "administrador" => "Eres un asistente administrativo. Analizas ingresos, citas, personal y productividad.",
                _ => "Eres un asistente general de LabClinic. Ayudas con análisis, soporte y reportes."
            };
        }

        private async Task<string> BuildEstadoSucursalResumenAsync(int sucursalId)
        {
            var personas = await _db.Personas.CountAsync(p => p.IdSucursal == sucursalId);
            var examenes = await _db.Examenes.CountAsync(e => e.IdSucursal == sucursalId);
            var pagos = await _db.Pagos.CountAsync(p => p.IdSucursal == sucursalId && p.FechaPago != null);
            var citas = await _db.Citas.CountAsync(c => c.IdSucursal == sucursalId);
            var insumos = await _db.Insumos.CountAsync(i => i.IdSucursal == sucursalId);
            var clinicas = await _db.Clinicas.CountAsync(c => c.IdSucursal == sucursalId);

            return $@"
            📋 Estado actual de la Sucursal {sucursalId}:
            👥 Personas: {personas}
            🧪 Exámenes: {examenes}
            💰 Pagos: {pagos}
            📅 Citas: {citas}
            🧴 Insumos: {insumos}
            🏥 Clínicas: {clinicas}
            ";
        }

        // ============================================================
        // 🔹 MÉTODO: Generar y enviar el reporte de insumos (sin logo)
        // ============================================================
        private async Task<string> GenerarYEnviarReporteIA(IEmailService emailService)
        {
            var hoy = DateTime.Today;
            var registros = await _db.RegistroInsumoUsos
                .Include(r => r.Insumo)
                .Include(r => r.Examen).ThenInclude(e => e.TipoExamen)
                .Where(r => r.Fecha >= hoy && r.Fecha < hoy.AddDays(1))
                .WhereSucursal(_sucCtx)
                .ToListAsync();

            if (!registros.Any())
                return "📭 No se encontraron insumos usados hoy.";

            var body = new EmailReporteDto
            {
                correo = "josx009@gmail.com", // 📧 destinatario por defecto
                asunto = "Reporte diario de insumos",
                titulo = $"Insumos usados el {DateTime.Now:dd/MM/yyyy}",
                items = registros.Select(r => new EmailItemDto
                {
                    fecha = r.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    insumo = r.Insumo?.Nombre ?? "(Sin nombre)",
                    cantidad = (int)r.CantidadUsada,
                    tipoExamen = r.Examen?.TipoExamen?.Nombre ?? "(Manual)",
                    justificacion = r.Justificacion ?? "—"
                }).ToList()
            };

            string html = GenerarHtmlCorreo(body);

            await emailService.SendAsync(
                body.correo,
                body.asunto,
                $"Reporte de insumos: {body.titulo}",
                html
            );

            return $"✅ Se envió el reporte de insumos del día a {body.correo}.";
        }
        // ==============================================
        // 🔹 Método auxiliar para generar respuesta IA
        // ==============================================
        private async Task<string> LlamarGemini(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "❌ Falta clave de API de Google Gemini.";

            var payload = new
            {
                contents = new[]
                {
            new { parts = new[] { new { text = prompt } } }
        }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var response = await _http.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(result);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Sin respuesta generada.";
            }
            catch
            {
                return "⚠️ No se pudo procesar la respuesta de la IA.";
            }
        }
        // ============================================================
        // 🔹 HTML limpio y profesional (sin logo)
        // ============================================================
        private string GenerarHtmlCorreo(EmailReporteDto body)
        {
            return $@"
        <style>
            body {{
                font-family: 'Segoe UI', Arial, sans-serif;
                background-color: #f4f6f9;
                margin: 0;
                padding: 20px;
            }}
            h2 {{
                color: #0d6efd;
                font-weight: 600;
            }}
            table {{
                border-collapse: collapse;
                width: 100%;
                margin-top: 12px;
                font-size: 13px;
            }}
            th, td {{
                border: 1px solid #dee2e6;
                padding: 8px;
                text-align: left;
            }}
            th {{
                background: #0d6efd;
                color: white;
                text-transform: uppercase;
                letter-spacing: 0.4px;
            }}
            tr:nth-child(even) {{ background-color: #f8f9fa; }}
            tr:hover {{ background-color: #eef3ff; }}
            .footer {{
                text-align: center;
                color: #6c757d;
                margin-top: 20px;
                font-size: 12px;
            }}
        </style>
        <h2>{body.titulo}</h2>
        <table>
            <thead>
                <tr>
                    <th>Fecha</th>
                    <th>Insumo</th>
                    <th>Cantidad</th>
                    <th>Examen</th>
                    <th>Justificación</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", body.items.Select(i => $@"
                    <tr>
                        <td>{i.fecha}</td>
                        <td>{i.insumo}</td>
                        <td>{i.cantidad}</td>
                        <td>{i.tipoExamen}</td>
                        <td>{i.justificacion}</td>
                    </tr>
                "))}
            </tbody>
        </table>
        <div class='footer'>
            Reporte generado automáticamente por <b>LabClinic</b> • {DateTime.Now:dd/MM/yyyy HH:mm}
        </div>";
        }


    }


}
