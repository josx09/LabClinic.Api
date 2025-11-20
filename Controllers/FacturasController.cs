using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/facturas")]
[Authorize]
public class FacturasController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx; 

    public FacturasController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx;
    }

    // ==========================================================
    //🔹 Obtener todas las facturas (paginadas y filtradas por sucursal)
    // ==========================================================
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Facturas
            .Include(f => f.Pago)
            .ThenInclude(p => p.Persona)
            .WhereSucursal(_sucCtx) 
            .OrderByDescending(f => f.FechaFactura);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id,
                f.IdPago,
                f.FechaFactura,
                f.MontoTotal,
                Paciente = f.Pago != null && f.Pago.Persona != null
                    ? f.Pago.Persona.Nombre + " " + f.Pago.Persona.Apellido
                    : "(Sin paciente)",
                MetodoPago = f.Pago != null
                    ? (f.Pago.IdTipoPago == 1 ? "Efectivo" :
                       f.Pago.IdTipoPago == 2 ? "Tarjeta" :
                       f.Pago.IdTipoPago == 3 ? "Transferencia" : "Otro")
                    : "(Desconocido)"
            })
            .ToListAsync();

        return Ok(new { total, items });
    }

    // ==========================================================
    //  Obtener factura por ID (con detalles de pago)
    // ==========================================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _db.Facturas
            .Include(f => f.Pago)
            .ThenInclude(p => p.Persona)
            .WhereSucursal(_sucCtx) //  asegura que pertenezca a la sucursal actual
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null)
            return NotFound(new { message = "❌ Factura no encontrada o no pertenece a esta sucursal." });

        return Ok(new
        {
            factura.Id,
            factura.FechaFactura,
            factura.MontoTotal,
            Pago = factura.Pago != null ? new
            {
                factura.Pago.Id,
                factura.Pago.MontoPagado,
                factura.Pago.Concepto,
                MetodoPago = factura.Pago.IdTipoPago == 1 ? "Efectivo" :
                             factura.Pago.IdTipoPago == 2 ? "Tarjeta" :
                             factura.Pago.IdTipoPago == 3 ? "Transferencia" : "Otro",
                Persona = factura.Pago.Persona != null
                    ? $"{factura.Pago.Persona.Nombre} {factura.Pago.Persona.Apellido}"
                    : "(Sin paciente)"
            } : null
        });
    }

    // ==========================================================
    //Crear factura manual (opcional)
    // ==========================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FacturaPago model)
    {
        if (model == null || model.IdPago <= 0)
            return BadRequest(new { message = "Debe especificar un pago válido." });

        // Verifica si el pago pertenece a la sucursal actual
        var pago = await _db.Pagos
            .WhereSucursal(_sucCtx)
            .FirstOrDefaultAsync(p => p.Id == model.IdPago);

        if (pago == null)
            return BadRequest(new { message = "El pago no existe o no pertenece a esta sucursal." });

        model.FechaFactura = DateTime.Now;

        //  asignar automáticamente la sucursal actual
        _db.StampSucursal(_sucCtx);

        _db.Facturas.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Factura registrada correctamente.", model.Id });
    }
}
