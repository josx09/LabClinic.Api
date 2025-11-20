using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/ingresos-insumo")]
[Authorize]
public class IngresosInsumoController : ControllerBase {
  private readonly LabDbContext _db;
  public IngresosInsumoController(LabDbContext db){ _db=db; }

  [HttpGet]
  public async Task<IActionResult> Get([FromQuery] PagedQuery q){
    var set = _db.IngresosInsumo.AsQueryable();
    var total = await set.CountAsync();
    var items = await set.OrderByDescending(x=>x.FechaIngreso).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToListAsync();
    return Ok(new { total, items });
  }

  [HttpPost]
  public async Task<IActionResult> Create(IngresoInsumo x){
    _db.IngresosInsumo.Add(x);
    var insumo = await _db.Insumos.FirstOrDefaultAsync(i=>i.Id==x.IdInsumo);
    if(insumo==null) return BadRequest("Insumo no existe");
    insumo.Stock += x.Cantidad;
    await _db.SaveChangesAsync();
    return Created($"api/ingresos-insumo/{x.Id}", x);
  }
}