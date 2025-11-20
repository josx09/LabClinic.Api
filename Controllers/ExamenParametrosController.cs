using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/examenes/{idExamen:int}/parametros")]
[Authorize]
public class ExamenParametrosController : ControllerBase
{
    private readonly LabDbContext _db;
    public ExamenParametrosController(LabDbContext db) => _db = db;

    // GET: api/examenes/{idExamen}/parametros
    [HttpGet]
    public async Task<IActionResult> GetByExamen(int idExamen)
    {
        var list = await _db.Set<ParametroExamen>()
            .Where(p => p.IdExamen == idExamen)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                unidad = p.Unidad,
                rangoReferencia = p.RangoReferencia,
                resultado = p.Resultado,
                observaciones = p.Observaciones
            })
            .ToListAsync();

        return Ok(list);
    }

    public class BulkCreateFromTemplateRequest
    {
        public int IdTipoExamen { get; set; }
        public List<ParametroPayload> Parametros { get; set; } = new();
    }

    public class ParametroPayload
    {
        public string Nombre { get; set; } = "";
        public string? Unidad { get; set; }
        public string? RangoReferencia { get; set; }
        public string? Resultado { get; set; }
        public string? Observaciones { get; set; }
    }

    // POST: api/examenes/{idExamen}/parametros/from-base-template
    [HttpPost("from-base-template")]
    public async Task<IActionResult> CreateFromTemplate(int idExamen)
    {
        var examen = await _db.Examenes.FindAsync(idExamen);
        if (examen == null)
            return NotFound("Examen no encontrado.");

        //  Usamos el tipo de examen real del examen
        var template = await _db.ParametrosTipoExamen
            .Where(t => t.IdTipoExamen == examen.IdTipoExamen)
            .OrderBy(t => t.Id)
            .ToListAsync();

        if (!template.Any())
            return BadRequest("El tipo de examen no tiene parámetros definidos.");

        // Evitar duplicados si ya existen
        var yaHay = await _db.Set<ParametroExamen>().AnyAsync(p => p.IdExamen == idExamen);
        if (yaHay)
            return BadRequest("Este examen ya tiene parámetros cargados.");

        var nuevos = template.Select(t => new ParametroExamen
        {
            IdExamen = idExamen,
            Nombre = t.Nombre,
            Unidad = t.Unidad,
            RangoReferencia = t.RangoReferencia,
            Resultado = null,
            Observaciones = t.Observaciones
        }).ToList();

        _db.Set<ParametroExamen>().AddRange(nuevos);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Parámetros creados desde plantilla.", count = nuevos.Count });
    }


    // POST: api/examenes/{idExamen}/parametros
  
    [HttpPost]
    public async Task<IActionResult> BulkCreate(int idExamen, [FromBody] List<ParametroPayload> parametros)
    {
        var examen = await _db.Examenes.FindAsync(idExamen);
        if (examen == null) return NotFound("Examen no encontrado.");

        var items = parametros.Select(p => new ParametroExamen
        {
            IdExamen = idExamen,
            Nombre = p.Nombre,
            Unidad = p.Unidad,
            RangoReferencia = p.RangoReferencia,
            Resultado = p.Resultado,
            Observaciones = p.Observaciones
        }).ToList();

        _db.Set<ParametroExamen>().AddRange(items);
        await _db.SaveChangesAsync();
        return Ok(new { message = "✅ Parámetros creados.", count = items.Count });
    }

    // PUT: api/examenes/{idExamen}/parametros/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int idExamen, int id, [FromBody] ParametroPayload body)
    {
        var item = await _db.Set<ParametroExamen>().FirstOrDefaultAsync(p => p.Id == id && p.IdExamen == idExamen);
        if (item == null) return NotFound();

        item.Resultado = body.Resultado ?? item.Resultado;
        item.Observaciones = body.Observaciones ?? item.Observaciones;

        _db.Update(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "✏️ Parámetro actualizado." });
    }

    // DELETE: api/examenes/{idExamen}/parametros/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int idExamen, int id)
    {
        var item = await _db.Set<ParametroExamen>().FirstOrDefaultAsync(p => p.Id == id && p.IdExamen == idExamen);
        if (item == null) return NotFound();

        _db.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "🗑️ Parámetro eliminado." });
    }
}
