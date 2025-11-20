using LabClinic.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/direcciones")]
public class DireccionController : ControllerBase
{
    private readonly LabDbContext _db;
    public DireccionController(LabDbContext db) => _db = db;

    // =====================================================
    // GET: api/direcciones
    // =====================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Direccion>>> List()
    {
        var direcciones = await _db.Direcciones
            .Include(d => d.Municipio)
                .ThenInclude(m => m.Departamento)
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .ToListAsync();

        return Ok(direcciones);
    }

    // =====================================================
    //  GET: api/direcciones/{id}
    // =====================================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Direccion>> Get(int id)
    {
        var direccion = await _db.Direcciones
            .Include(d => d.Municipio)
                .ThenInclude(m => m.Departamento)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (direccion == null)
            return NotFound(new { message = "Dirección no encontrada." });

        return Ok(direccion);
    }

    // =====================================================
    // POST: api/direcciones
    // =====================================================
    [HttpPost]
    public async Task<ActionResult<Direccion>> Create([FromBody] Direccion model)
    {
        try
        {
            _db.Direcciones.Add(model);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al guardar dirección: {ex.InnerException?.Message ?? ex.Message}" });
        }
    }

    // =====================================================
    //  PUT: api/direcciones/{id}
    // =====================================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Direccion model)
    {
        if (id != model.Id)
            return BadRequest(new { message = "El ID enviado no coincide." });

        var direccion = await _db.Direcciones.FindAsync(id);
        if (direccion == null)
            return NotFound(new { message = "Dirección no encontrada." });

        direccion.Calle = model.Calle;
        direccion.Numero = model.Numero;
        direccion.Zona = model.Zona;
        direccion.Referencia = model.Referencia;
        direccion.IdMunicipio = model.IdMunicipio;

        await _db.SaveChangesAsync();
        return Ok(direccion);
    }

    // =====================================================
    // DELETE: api/direcciones/{id}
    // =====================================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var direccion = await _db.Direcciones.FindAsync(id);
        if (direccion == null)
            return NotFound(new { message = "Dirección no encontrada." });

        _db.Direcciones.Remove(direccion);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Dirección eliminada correctamente." });
    }
}
