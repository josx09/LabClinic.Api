using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
  private readonly LabDbContext _db;
  public AppointmentsController(LabDbContext db){ _db = db; }

  [HttpGet]
  public async Task<IEnumerable<Appointment>> Get() =>
    await _db.Appointments.OrderByDescending(a => a.Schedule).ToListAsync();

  [HttpGet("{id:int}")]
  public async Task<ActionResult<Appointment>> GetById(int id)
  {
    var a = await _db.Appointments.FindAsync(id);
    return a is null ? NotFound() : a;
  }

  [HttpPost]
  public async Task<ActionResult<Appointment>> Create(Appointment a)
  {
    _db.Appointments.Add(a);
    await _db.SaveChangesAsync();
    return CreatedAtAction(nameof(GetById), new { id = a.Id }, a);
  }

  [HttpPut("{id:int}")]
  public async Task<IActionResult> Update(int id, Appointment a)
  {
    if (id != a.Id) return BadRequest();
    _db.Entry(a).State = EntityState.Modified;
    await _db.SaveChangesAsync();
    return NoContent();
  }

  [HttpDelete("{id:int}")]
  public async Task<IActionResult> Delete(int id)
  {
    var a = await _db.Appointments.FindAsync(id);
    if (a == null) return NotFound();
    _db.Remove(a);
    await _db.SaveChangesAsync();
    return NoContent();
  }
}