using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentTestsController : ControllerBase
{
  private readonly LabDbContext _db;
  public AppointmentTestsController(LabDbContext db){ _db=db; }

  [HttpGet("{appointmentId:int}")]
  public Task<List<AppointmentTest>> GetForAppointment(int appointmentId) =>
    _db.AppointmentTests.Where(x => x.AppointmentId == appointmentId).ToListAsync();

  [HttpPost]
  public async Task<ActionResult<AppointmentTest>> Add(AppointmentTest x){
    _db.AppointmentTests.Add(x); await _db.SaveChangesAsync();
    return CreatedAtAction(nameof(GetForAppointment), new{ appointmentId = x.AppointmentId }, x);
  }

  [HttpDelete("{id:int}")]
  public async Task<IActionResult> Delete(int id){
    var x = await _db.AppointmentTests.FindAsync(id); if(x==null) return NotFound();
    _db.Remove(x); await _db.SaveChangesAsync(); return NoContent();
  }
}