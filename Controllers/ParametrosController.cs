using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/parametros")]
[Authorize]
public class ParametrosController : ControllerBase
{
    private readonly LabDbContext _db;
    public ParametrosController(LabDbContext db) => _db = db;

    //  Obtener todos los parámetros
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Parametros
            .Include(p => p.TipoExamen)
            .ToListAsync();
        return Ok(items);
    }

    // Obtener parámetros por tipo de examen
    [HttpGet("tipo/{idTipoExamen:int}")]
    public async Task<IActionResult> GetByTipo(int idTipoExamen)
    {
        var items = await _db.Parametros
            .Where(p => p.IdTipoExamen == idTipoExamen)
            .ToListAsync();
        return Ok(items);
    }

    //  Crear parámetro
    [HttpPost]
    public async Task<IActionResult> Create(Parametro p)
    {
        _db.Parametros.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = p.Id }, p);
    }

    // Actualizar parámetro
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Parametro p)
    {
        if (id != p.Id) return BadRequest();
        _db.Entry(p).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Eliminar parámetro
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await _db.Parametros.FindAsync(id);
        if (x == null) return NotFound();
        _db.Parametros.Remove(x);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
