using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using LabClinic.Api.Models.Dtos;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize]
public class PagosController : ControllerBase
{
  
        private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx; 

    public PagosController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx; 
    }
   
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedQuery q, [FromQuery] int? personaId, [FromQuery] bool soloConExamenes = true)
    {
        var set = _db.Pagos
    .Include(p => p.Persona)
    .WhereSucursal(_sucCtx) //  filtra por sucursal actual
    .AsQueryable();


        //  Filtro por paciente específico (cuando viene desde Exámenes)
        if (personaId.HasValue && personaId.Value > 0)
            set = set.Where(x => x.IdPersona == personaId.Value);

        //  Filtro opcional: mostrar solo pagos de personas con exámenes
        if (soloConExamenes && !personaId.HasValue)
        {
            set = set.Where(p => _db.Examenes.Any(e => e.IdPersona == p.IdPersona));
        }

        //  Búsqueda general
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            set = set.Where(x =>
                x.Concepto.Contains(q.Search) ||
                (x.Persona != null && (
                    x.Persona.Nombre.Contains(q.Search) ||
                    x.Persona.Apellido.Contains(q.Search) ||
                    x.Persona.Telefono.Contains(q.Search)
                )));
        }

        var total = await set.CountAsync();

        var items = await set
            .OrderByDescending(x => x.FechaPago)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => new
            {
                id_pago = x.Id,
                id_persona = x.IdPersona,
                paciente = x.Persona != null
                    ? x.Persona.Nombre + " " + x.Persona.Apellido
                    : "(sin paciente)",
                telefono = x.Persona != null
                    ? x.Persona.Telefono
                    : "(sin teléfono)",
                monto_pagado = x.MontoPagado,
                concepto = x.Concepto,
                tipo_pago_nombre = x.IdTipoPago == 1 ? "Efectivo" :
                                   x.IdTipoPago == 2 ? "Tarjeta" :
                                   x.IdTipoPago == 3 ? "Transferencia" :
                                   "Otro",
                fecha_generado = x.FechaGenerado,
                fecha_pago = x.FechaPago,
                nota = x.Nota,
                estado = x.Estado
            })
            .ToListAsync();

        return Ok(new { total, items });
    }




    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var x = await _db.Pagos.FindAsync(id);
        return x is null ? NotFound() : Ok(x);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Pago x)
    {
        _db.Pagos.Add(x);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = (x as dynamic).Id }, x);
    }

    // Controllers/PagosController.cs
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePagoRequest req)
    {
        var pago = await _db.Pagos.FindAsync(id);
        if (pago == null) return NotFound();

        //  No tocar: IdPersona, FechaGenerado
        pago.MontoPagado = req.MontoPagado;
        pago.IdTipoPago = req.IdTipoPago;
        pago.Concepto = req.Concepto;
        pago.Nota = req.Nota;
        pago.FechaPago = req.FechaPago ?? pago.FechaPago;
        pago.Estado = req.Estado;

        if (!_db.TiposPago.Any(t => t.Id == req.IdTipoPago))

            return BadRequest("Tipo de pago no válido.");



        await _db.SaveChangesAsync();
        return Ok(pago);
    }

    public sealed class UpdatePagoRequest
    {
        public decimal MontoPagado { get; set; }
        public int IdTipoPago { get; set; }
        public string? Concepto { get; set; }
        public string? Nota { get; set; }
        public DateTime? FechaPago { get; set; }
        public int Estado { get; set; } = 1;
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await _db.Pagos.FindAsync(id);
        if (x == null) return NotFound();
        _db.Remove(x);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ==========================================================
    //  MÉTODOS ADICIONALES
    // ==========================================================

    // Resumen de exámenes pendientes de pago por paciente
    [HttpGet("resumen/{idPersona:int}")]
    public async Task<IActionResult> ResumenPaciente(int idPersona)
    {
        //  Verifica existencia del paciente
        var persona = await _db.Persons.FindAsync(idPersona);
        if (persona == null)
            return NotFound(new { message = "❌ Paciente no encontrado." });

        //  Obtiene todos los exámenes sin pago (individuales o creados desde perfil)
        var examenes = await _db.Examenes
            .Include(e => e.TipoExamen)
            .Where(e => e.IdPersona == idPersona && e.IdPago == null)
            .WhereSucursal(_sucCtx) 
            .Select(e => new
            {
                id = e.Id,
                nombre = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Examen sin tipo)",
                monto = e.PrecioAplicado,
                estado = e.Estado
            })
            .OrderBy(e => e.nombre)
            .ToListAsync();


        //  Calcula el total de los pendientes
        var total = examenes.Sum(x => x.monto);

        return Ok(new
        {
            idPersona,
            paciente = $"{persona.Nombre} {persona.Apellido}",
            cantidad = examenes.Count,
            total,
            examenes
        });
    }


        // ==========================================================
        //  FUNCIÓN REUTILIZABLE DE PAGO (Optimizada + Multisucursal)
        // ==========================================================
        private async Task<(int idPago, int cantidad, decimal total)> ProcesarPagoAsync(
            int idPersona,
            IEnumerable<int> idsExamenes,
            int? idTipoPago,
            string? concepto,
            string? nota
        )
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                //  Obtener exámenes pendientes del paciente
                var exs = await _db.Examenes
                    .Where(e => e.IdPersona == idPersona && e.IdPago == null && idsExamenes.Contains(e.Id))
                    .WhereSucursal(_sucCtx) //  Filtro por sucursal activa
                    .ToListAsync();

                if (!exs.Any())
                    return (0, 0, 0m);

                // Calcular total
                var total = exs.Sum(e => e.PrecioAplicado);

                //  Crear registro de pago
                var pago = new Pago
                {
                    IdPersona = idPersona,
                    IdUsuario = int.TryParse(User?.FindFirst("uid")?.Value, out var uid) ? uid : 1,
                    IdTipoPago = idTipoPago ?? 1,
                    Concepto = string.IsNullOrWhiteSpace(concepto) ? "Pago de exámenes" : concepto,
                    Nota = nota,
                    MontoPagado = total,
                    FechaGenerado = DateTime.Now,
                    FechaPago = DateTime.Now,
                    Estado = 1
                };

            // Asignar automáticamente la sucursal actual
            _db.StampSucursal(_sucCtx);



            _db.Pagos.Add(pago);
                await _db.SaveChangesAsync();

                // 4Asignar el pago a los exámenes del paciente
                foreach (var e in exs)
                    e.IdPago = pago.Id;

                _db.Examenes.UpdateRange(exs);
                await _db.SaveChangesAsync();

                // Crear factura asociada al pago (opcional)
                _db.Facturas.Add(new FacturaPago
                {
                    IdPago = pago.Id,
                    FechaFactura = DateTime.Now,
                    MontoTotal = total
                });

                await _db.SaveChangesAsync();

                //  Confirmar transacción
                await tx.CommitAsync();

                return (pago.Id, exs.Count, total);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"❌ Error en ProcesarPagoAsync: {ex.Message}");
                return (0, 0, 0m);
            }
        }



    // ==========================================================
    //  ENDPOINT: PAGO TOTAL
    // ==========================================================
    [HttpPost("pagar-paciente")]
    public async Task<IActionResult> PagarPaciente([FromBody] PagarPacienteRequest req)
    {
        if (req == null || req.id_persona <= 0)
            return BadRequest("Datos inválidos.");

        var pendientes = await _db.Examenes
            .Where(e => e.IdPersona == req.id_persona)
            .Select(e => new { e.Id, e.IdPago, e.PrecioAplicado })
            .ToListAsync();

        var ids = (req.examenes != null && req.examenes.Count > 0)
            ? req.examenes
            : pendientes.Where(x => x.IdPago == null).Select(x => x.Id).ToList();

        if (ids.Count == 0)
            return BadRequest("No hay exámenes pendientes.");

        var (idPago, cantidad, total) = await ProcesarPagoAsync(
            req.id_persona, ids, req.id_tipo_pago, req.concepto, req.nota
        );

        if (idPago == 0)
            return BadRequest("Error al registrar el pago.");

        // ✔️ MISMA LÓGICA QUE PAGO PARCIAL
        return Ok(new
        {
            message = "Pago total registrado",
            id_pago = idPago,
            cantidad,
            total
        });
    }


    // ==========================================================
    //  ENDPOINT: PAGO PARCIAL
    // ==========================================================
    [HttpPost("pagar-parcial")]
    public async Task<IActionResult> PagarParcial([FromBody] PagoParcialRequest req)
    {
        if (req == null || req.id_persona <= 0 || req.examenes == null || req.examenes.Count == 0)
            return BadRequest("Debe indicar los exámenes a pagar.");

        var (idPago, cantidad, total) = await ProcesarPagoAsync(
            req.id_persona, req.examenes, req.id_tipo_pago, req.concepto, req.nota
        );

        if (idPago == 0) return BadRequest("No se procesó ningún examen.");

        return Ok(new { message = "Pago parcial registrado", id_pago = idPago, cantidad, total });
    }

    // ==========================================================
    //  CLASES AUXILIARES (DTO)
    // ==========================================================
    public class PagarPacienteRequest
    {
        public int id_persona { get; set; }
        public int? id_tipo_pago { get; set; }
        public string? concepto { get; set; }
        public string? nota { get; set; }
        public List<int>? examenes { get; set; }
    }

    public class PagoParcialRequest
    {
        public int id_persona { get; set; }
        public List<int> examenes { get; set; } = new();
        public int? id_tipo_pago { get; set; }
        public string? concepto { get; set; }
        public string? nota { get; set; }
    }

    // ==========================================================
    //  GENERAR COMPROBANTE PDF DE PAGO
    // ==========================================================
    [HttpGet("{idPago}/comprobante")]
    [AllowAnonymous]
    public IActionResult GenerarComprobantePago(int idPago)
    {
        var pago = _db.Pagos
            .Include(p => p.Persona)
            .FirstOrDefault(p => p.Id == idPago);

        if (pago == null)
            return NotFound(new { message = "❌ Pago no encontrado." });

        var examenes = _db.Examenes
            .Include(e => e.TipoExamen)
            .Where(e => e.IdPago == idPago)
            .Select(e => new
            {
                e.Id,
                Nombre = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                e.PrecioAplicado
            })
            .ToList();

        var total = examenes.Sum(e => e.PrecioAplicado);
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo_lab.png");

        //  Activar modo debug para diagnosticar futuros errores (opcional)
        QuestPDF.Settings.EnableDebugging = false;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(40);

                // ======================================================
                //  ENCABEZADO
                // ======================================================
                page.Header().PaddingBottom(10).Row(row =>
                {
                    if (System.IO.File.Exists(logoPath))
                    {
                        row.ConstantItem(70).Height(45).AlignMiddle().Image(logoPath).FitArea();
                    }

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Laboratorio Clínico CDC").FontSize(15).Bold();
                        col.Item().AlignCenter().Text("Comprobante de Pago").FontSize(11).Italic();
                    });
                });

                // ======================================================
                //  CONTENIDO PRINCIPAL
                // ======================================================
                page.Content().PaddingVertical(5).Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Text($"Paciente: {pago.Persona?.Nombre} {pago.Persona?.Apellido}")
                        .FontSize(12);
                    col.Item().Text($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}")
                        .FontSize(12);
                    col.Item().Text($"Método de pago: {GetTipoPagoNombre(pago.IdTipoPago)}")
                        .FontSize(12);

                    col.Item().PaddingTop(10).LineHorizontal(1);

                    foreach (var e in examenes)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(3).Text(e.Nombre);
                            r.RelativeItem(1).AlignRight().Text($"Q{e.PrecioAplicado:0.00}");
                        });
                    }

                    col.Item().PaddingTop(5).LineHorizontal(1);
                    col.Item().AlignRight().PaddingTop(5)
                        .Text($"Total pagado: Q{total:0.00}")
                        .FontSize(13).Bold();
                });

                // ======================================================
                //  PIE DE PÁGINA
                // ======================================================
                page.Footer().PaddingTop(10).Column(footer =>
                {
                    footer.Spacing(3);

                    footer.Item().AlignCenter().Text("Gracias por su pago")
                        .FontSize(10);

                    if (System.IO.File.Exists(logoPath))
                    {
                        //  Logo pequeño centrado (sin forzar espacio)
                        footer.Item().AlignCenter().Height(35).Width(60).Image(logoPath).FitArea();
                    }

                    footer.Item().AlignCenter().Text("© Laboratorio Clínico CDC — Todos los derechos reservados")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"comprobante_pago_{idPago}.pdf");
    }

    private byte[] GenerarPdfDeComprobante(int idPago)
    {
        var pago = _db.Pagos
            .Include(p => p.Persona)
            .FirstOrDefault(p => p.Id == idPago);

        if (pago == null) return null;

        var examenes = _db.Examenes
            .Include(e => e.TipoExamen)
            .Where(e => e.IdPago == idPago)
            .Select(e => new {
                e.Id,
                Nombre = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                e.PrecioAplicado
            })
            .ToList();

        var total = examenes.Sum(e => e.PrecioAplicado);
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo_lab.png");

        QuestPDF.Settings.EnableDebugging = false;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(40);

                page.Header().PaddingBottom(10).Row(row =>
                {
                    if (System.IO.File.Exists(logoPath))
                    {
                        row.ConstantItem(70).Height(45).AlignMiddle().Image(logoPath).FitArea();
                    }

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Laboratorio Clínico CDC").FontSize(15).Bold();
                        col.Item().AlignCenter().Text("Comprobante de Pago").FontSize(11).Italic();
                    });
                });

                page.Content().PaddingVertical(5).Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Text($"Paciente: {pago.Persona?.Nombre} {pago.Persona?.Apellido}").FontSize(12);
                    col.Item().Text($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}").FontSize(12);
                    col.Item().Text($"Método de pago: {GetTipoPagoNombre(pago.IdTipoPago)}").FontSize(12);

                    col.Item().PaddingTop(10).LineHorizontal(1);

                    foreach (var e in examenes)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem(3).Text(e.Nombre);
                            r.RelativeItem(1).AlignRight().Text($"Q{e.PrecioAplicado:0.00}");
                        });
                    }

                    col.Item().PaddingTop(5).LineHorizontal(1);
                    col.Item().AlignRight().PaddingTop(5)
                        .Text($"Total pagado: Q{total:0.00}")
                        .FontSize(13).Bold();
                });

                page.Footer().PaddingTop(10).Column(footer =>
                {
                    footer.Spacing(3);
                    footer.Item().AlignCenter().Text("Gracias por su pago").FontSize(10);

                    if (System.IO.File.Exists(logoPath))
                    {
                        footer.Item().AlignCenter().Height(35).Width(60).Image(logoPath).FitArea();
                    }

                    footer.Item().AlignCenter().Text("© Laboratorio Clínico CDC — Todos los derechos reservados")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();

        return pdf;
    }


    // Controllers/PagosController.cs
    [HttpPost("from-exams")]
        public async Task<IActionResult> CreateFromExams([FromBody] CreatePagoFromExamsRequest req)
        {
        // Validaciones de flujo Persona → Examen
        var personaExiste = await _db.Personas.AnyAsync(p => p.Id == req.IdPersona && p.Estado == 1);

        if (!personaExiste) return BadRequest("Paciente no válido o inactivo.");

            // Exámenes seleccionados del paciente (pendientes de pago: sin id_pago)
            var examenes = await _db.Examenes
                .Where(e => e.IdPersona == req.IdPersona && req.ExamenIds.Contains(e.Id) && e.IdPago == null)
                .ToListAsync();

            if (examenes.Count == 0)
                return BadRequest("No hay exámenes seleccionados pendientes de pago para este paciente.");

            //  Verificación de montos
            var totalSeleccionado = examenes.Sum(e => e.PrecioAplicado);
            if (req.MontoPagado <= 0 || req.MontoPagado > totalSeleccionado)
                return BadRequest($"Monto inválido. Total seleccionados: {totalSeleccionado:0.00}");

            //  Crear el pago
            var pago = new Pago {
                IdPersona     = req.IdPersona,
                IdUsuario     = req.IdUsuario,     
                MontoPagado   = req.MontoPagado,
                Concepto      = string.IsNullOrWhiteSpace(req.Concepto) ? 
                                (req.MontoPagado == totalSeleccionado ? "Pago total de exámenes" : "Pago parcial de examen clínico") 
                                : req.Concepto,
                IdTipoPago    = req.IdTipoPago,    
                FechaGenerado = DateTime.Now,
                FechaPago     = req.FechaPago ?? DateTime.Now,
                Nota          = req.Nota,
                Estado        = 1
            };
        _db.StampSucursal(_sucCtx);



        _db.Pagos.Add(pago);
                await _db.SaveChangesAsync(); 

                
                decimal restante = req.MontoPagado;
                foreach (var ex in examenes.OrderBy(e => e.FechaRegistro))
                {
                    if (restante <= 0) break;
                ex.IdPago = pago.Id;
                restante -= ex.PrecioAplicado;
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    pago.Id,
                    TotalSeleccionado = totalSeleccionado,
                    Cubierto = req.MontoPagado,
                    Pendiente = Math.Max(0, totalSeleccionado - req.MontoPagado),
                    ExamenesAfectados = examenes.Select(e => e.Id)
                });

        }

    public sealed class CreatePagoFromExamsRequest {
            public int IdPersona { get; set; }
            public int IdUsuario { get; set; }   
            public int IdTipoPago { get; set; }  
            public decimal MontoPagado { get; set; }
            public List<int> ExamenIds { get; set; } = new();
            public string? Concepto { get; set; }
            public string? Nota { get; set; }
            public DateTime? FechaPago { get; set; }
        }


 
    private string GetTipoPagoNombre(int? tipo)
    {
        return tipo switch
        {
            1 => "Efectivo",
            2 => "Tarjeta",
            3 => "Transferencia",
            _ => "Otro"
        };
    }

    [HttpGet("historial/{idPersona:int}")]
    public async Task<IActionResult> HistorialPagos(int idPersona)
    {
        var pagos = await _db.Pagos
    .Where(p => p.IdPersona == idPersona)
            .WhereSucursal(_sucCtx) 
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new {
                p.Id,
                p.MontoPagado,
                p.FechaPago,
                p.IdTipoPago,
                Metodo = p.IdTipoPago == 1 ? "Efectivo" :
                         p.IdTipoPago == 2 ? "Tarjeta" :
                         p.IdTipoPago == 3 ? "Transferencia" : "Otro"
            })
            .ToListAsync();


        return Ok(pagos);
    }

    [HttpGet("con-facturas")]
    public async Task<IActionResult> GetPagosConFacturas()
    {
        var pagos = await _db.Pagos
            .Include(p => p.Persona)
            .Include(p => p.Facturas)
            .WhereSucursal(_sucCtx) //  filtro sucursal
            .OrderByDescending(p => p.FechaPago)
    .Take(100)
            .Select(p => new PagoConFacturasDto
            {
                IdPago = p.Id,
                Concepto = p.Concepto,
                MontoPagado = p.MontoPagado,
                FechaPago = p.FechaPago,
                Metodo = _db.TiposPago
                    .Where(t => t.Id == p.IdTipoPago)
                    .Select(t => t.Nombre)
                    .FirstOrDefault(),
                Nota = p.Nota,
                IdPersona = p.IdPersona,
                Paciente = p.Persona != null
                    ? (p.Persona.Nombre + " " + p.Persona.Apellido)
                    : "(Sin paciente)",
                Facturas = p.Facturas.Select(f => new FacturaDto
                {
                    IdFactura = f.Id,
                    MontoTotal = f.MontoTotal,
                    Nit = f.Nit,
                    FechaFactura = f.FechaFactura,
                    Detalle = f.Detalle
                }).ToList()
            })
            .ToListAsync();

        return Ok(pagos);
    }


}
