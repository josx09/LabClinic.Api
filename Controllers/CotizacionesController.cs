using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/cotizaciones")]
[Authorize]
public class CotizacionesController : ControllerBase
{
    private readonly LabDbContext _db;
    public CotizacionesController(LabDbContext db)
    {
        _db = db;
    }

    //  Listar cotizaciones con examen y paciente
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var set = _db.Cotizaciones
            .Include(c => c.Examen)
            .ThenInclude(e => e.Persona)
            .AsQueryable();

        var total = await set.CountAsync();

        var items = await set
            .OrderByDescending(c => c.FechaCreacion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                id_cotizacion = c.Id,
                paciente = c.Examen.Persona != null
                    ? c.Examen.Persona.Nombre + " " + c.Examen.Persona.Apellido
                    : "(Sin paciente)",
                examen = c.Examen != null
                    ? c.Examen.TipoExamen.Nombre
                    : "(Sin examen)",
                precio = c.Precio,
                fecha = c.FechaCreacion
            })
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Cotizacion c)
    {
        c.FechaCreacion = DateTime.Now;
        _db.Cotizaciones.Add(c);
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Cotizacion req)
    {
        var x = await _db.Cotizaciones.FindAsync(id);
        if (x == null) return NotFound();

        x.Precio = req.Precio;
        x.IdExamen = req.IdExamen;
        await _db.SaveChangesAsync();
        return Ok(x);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await _db.Cotizaciones.FindAsync(id);
        if (x == null) return NotFound();
        _db.Remove(x);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
