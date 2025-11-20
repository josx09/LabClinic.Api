using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/proveedor")]   
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly LabDbContext _db;
    public ProveedoresController(LabDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedQuery? q)
    {
       
        q ??= new PagedQuery();
        if (q.Page <= 0) q.Page = 1;
        if (q.PageSize <= 0) q.PageSize = 50;

        var set = _db.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            set = set.Where(x =>
                (x.Nombre != null && x.Nombre.Contains(q.Search)) ||
                (x.Empresa != null && x.Empresa.Contains(q.Search)) ||
                (x.Email != null && x.Email.Contains(q.Search)) ||
                (x.Telefono != null && x.Telefono.Contains(q.Search)) ||
                (x.Descripcion != null && x.Descripcion.Contains(q.Search))
            );
        }

        var total = await set.CountAsync();
        var items = await set
            .OrderBy(x => x.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var x = await _db.Suppliers.FindAsync(id);
        return x is null ? NotFound() : Ok(x);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Supplier x)
    {
        _db.Suppliers.Add(x);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = x.Id }, x);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Supplier x)
    {
        if (id != x.Id) return BadRequest();
        _db.Entry(x).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await _db.Suppliers.FindAsync(id);
        if (x == null) return NotFound();
        _db.Remove(x);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
