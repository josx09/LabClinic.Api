using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/municipios")]
[Authorize]
public class MunicipiosController : ControllerBase {
  private readonly LabDbContext _db;
  public MunicipiosController(LabDbContext db){ _db=db; }

  [HttpGet]
  public async Task<IActionResult> Get([FromQuery] PagedQuery q){
    var set = _db.Municipios.AsQueryable();
    if(!string.IsNullOrWhiteSpace(q.Search)){
      set = set.Where(x => (x.Nombre.Contains(q.Search)));
    }
    var total = await set.CountAsync();
    var items = await set.Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToListAsync();
    return Ok(new { total, items });
  }

  [HttpGet("{id:int}")]
  public async Task<IActionResult> GetById(int id){
    var x = await _db.Municipios.FindAsync(id);
    return x is null ? NotFound() : Ok(x);
  }

  [HttpPost]
  public async Task<IActionResult> Create(Municipio x){
    _db.Municipios.Add(x); await _db.SaveChangesAsync();
    return CreatedAtAction(nameof(GetById), new{id = (x as dynamic).Id}, x);
  }

  [HttpPut("{id:int}")]
  public async Task<IActionResult> Update(int id, Municipio x){
    if(id != (x as dynamic).Id) return BadRequest();
    _db.Entry(x).State = EntityState.Modified;
    await _db.SaveChangesAsync();
    return NoContent();
  }

  [HttpDelete("{id:int}")]
  public async Task<IActionResult> Delete(int id){
    var x = await _db.Municipios.FindAsync(id);
    if(x==null) return NotFound();
    _db.Remove(x); await _db.SaveChangesAsync();
    return NoContent();
  }

    [HttpGet("departamento/{id:int}")]
    public async Task<IActionResult> GetByDepartamento(int id)
    {
        var municipios = await _db.Municipios
            .Where(m => m.IdDepartamento == id)
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .ToListAsync();

        if (municipios == null || municipios.Count == 0)
            return NotFound(new { message = "No hay municipios para este departamento." });

        return Ok(municipios);
    }

}