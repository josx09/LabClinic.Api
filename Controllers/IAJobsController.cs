using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

using LabClinic.Api.Data;
using LabClinic.Api.Common;
using LabClinic.Api.Services;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/ia/jobs")]
    [Authorize(Policy = "IA.Write")] // requiere permiso IA.Write
    public class IAJobsController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx;
        private readonly IEmailService _email;

        public IAJobsController(LabDbContext db, ISucursalContext sucCtx, IEmailService email)
        {
            _db = db;
            _sucCtx = sucCtx;
            _email = email;
        }

        // ============================================================
        // 🔹 Job: Enviar alerta de insumos bajos
        //     POST /api/ia/jobs/alerta-insumos?correo=alguien@dominio.com
        // ============================================================
        [HttpPost("alerta-insumos")]
        public async Task<IActionResult> EnviarAlertaInsumos([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest(new { message = "Debes proporcionar el parámetro 'correo'." });

            var sucursalId = _sucCtx.CurrentSucursalId;

            var insumosBajos = await _db.Insumos
                .Where(i => i.IdSucursal == sucursalId && i.StockMinimo > 0 && i.Stock <= i.StockMinimo)
                .OrderBy(i => i.Stock - i.StockMinimo)
                .Select(i => new { i.Nombre, i.Stock, i.StockMinimo })
                .ToListAsync();

            if (!insumosBajos.Any())
            {
                await _email.SendAsync(correo,
                    $"Prueba de alerta de insumos – Sucursal {sucursalId}",
                    "No hay insumos con stock bajo, pero este correo confirma que la función SMTP funciona correctamente.",
                    "<p>✅ Prueba de envío exitosa.<br>No hay insumos con stock bajo actualmente.</p>");
                return Ok(new { message = $"📧 Correo de prueba enviado a {correo} (sin insumos bajos)" });
            }


            // Texto plano
            var plainLines = new List<string>
            {
                $"⚠️ Alerta de Insumos con Stock Bajo – Sucursal {sucursalId}",
                $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
                ""
            };
            plainLines.AddRange(insumosBajos.Select(i => $"- {i.Nombre}: {i.Stock}/{i.StockMinimo}"));
            var plainText = string.Join(Environment.NewLine, plainLines);

            // HTML
            var html = $@"
<h2>⚠️ Alerta de Insumos con Stock Bajo</h2>
<p><b>Sucursal:</b> {sucursalId} &nbsp;|&nbsp; <b>Fecha:</b> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse; font-family:Arial, sans-serif;'>
  <thead style='background:#f2f2f2'>
    <tr>
      <th align='left'>Insumo</th>
      <th align='right'>Stock</th>
      <th align='right'>Mínimo</th>
    </tr>
  </thead>
  <tbody>
    {string.Join("", insumosBajos.Select(i => $"<tr><td>{i.Nombre}</td><td align='right'>{i.Stock}</td><td align='right'>{i.StockMinimo}</td></tr>"))}
  </tbody>
</table>";

            await _email.SendAsync(
                toEmail: correo,
                subject: $"⚠️ Alerta de Insumos – Sucursal {sucursalId}",
                plainTextBody: plainText,
                htmlBody: html
            );

            return Ok(new { message = $"📧 Alerta enviada a {correo}", total = insumosBajos.Count });
        }

        // ============================================================
        // 🔹 (Opcional) Job: Enviar reporte semanal
        //     POST /api/ia/jobs/reporte-semanal?correo=...&dias=7
        //     Borra este endpoint si no lo necesitas.
        // ============================================================
        [HttpPost("reporte-semanal")]
        public async Task<IActionResult> ReporteSemanal([FromQuery] string correo, [FromQuery] int dias = 7)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest(new { message = "Debes proporcionar el parámetro 'correo'." });

            dias = Math.Max(1, dias);

            var sucursalId = _sucCtx.CurrentSucursalId;
            var desde = DateTime.Today.AddDays(-dias);
            var hasta = DateTime.Today.AddDays(1);

            int personas = await _db.Personas.CountAsync(p => p.IdSucursal == sucursalId);
            int examenes = await _db.Examenes.CountAsync(e => e.IdSucursal == sucursalId && e.FechaRegistro >= desde && e.FechaRegistro < hasta);
            int citas = await _db.Citas.CountAsync(c => c.IdSucursal == sucursalId && c.Fecha >= desde && c.Fecha < hasta);
            decimal totalPagos = await _db.Pagos
                .Where(p => p.IdSucursal == sucursalId && p.FechaPago >= desde && p.FechaPago < hasta)
                .SumAsync(p => (decimal?)p.MontoPagado) ?? 0;

            // Texto plano
            var plain = $@"
📊 Reporte semanal – Sucursal {sucursalId}
Periodo: {desde:yyyy-MM-dd} a {hasta.AddDays(-1):yyyy-MM-dd}
👥 Personas: {personas}
🧪 Exámenes: {examenes}
📅 Citas: {citas}
💰 Total Pagado: Q{totalPagos:0.00}
".Trim();

            // HTML
            var html = $@"
<h2>📊 Reporte semanal – Sucursal {sucursalId}</h2>
<p><b>Periodo:</b> {desde:yyyy-MM-dd} a {hasta.AddDays(-1):yyyy-MM-dd}</p>
<ul>
  <li>👥 Personas: <b>{personas}</b></li>
  <li>🧪 Exámenes: <b>{examenes}</b></li>
  <li>📅 Citas: <b>{citas}</b></li>
  <li>💰 Total Pagado: <b>Q{totalPagos:0.00}</b></li>
</ul>";

            await _email.SendAsync(
                toEmail: correo,
                subject: $"Reporte Semanal – Sucursal {sucursalId}",
                plainTextBody: plain,
                htmlBody: html
            );

            return Ok(new { message = $"✅ Reporte semanal enviado a {correo}" });
        }
    }
}
