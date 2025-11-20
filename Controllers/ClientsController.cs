using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
  private readonly LabDbContext _db;
  public ClientsController(LabDbContext db){ _db=db; }

  [HttpGet] public Task<List<Client>> Get() => _db.Clients.OrderBy(c=>c.Lastname).ToListAsync();
  [HttpGet("{id:int}")] public async Task<ActionResult<Client>> GetById(int id){ var c=await _db.Clients.FindAsync(id); return c is null? NotFound(): c; }
  [HttpPost] public async Task<ActionResult<Client>> Create(Client c){ _db.Clients.Add(c); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new{id=c.Id}, c); }
  [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, Client c){ if(id!=c.Id) return BadRequest(); _db.Entry(c).State=EntityState.Modified; await _db.SaveChangesAsync(); return NoContent(); }
  [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){ var c=await _db.Clients.FindAsync(id); if(c==null) return NotFound(); _db.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
}