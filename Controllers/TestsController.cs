using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestsController : ControllerBase
{
  private readonly LabDbContext _db;
  public TestsController(LabDbContext db){ _db = db; }

  [HttpGet] public async Task<IEnumerable<Test>> Get() => await _db.Tests.Where(t=>t.Status==1 || t.Status==0).ToListAsync();
  [HttpGet("{id:int}")] public async Task<ActionResult<Test>> GetById(int id){ var x=await _db.Tests.FindAsync(id); return x is null? NotFound(): x; }
  [HttpPost] public async Task<ActionResult<Test>> Create(Test t){ _db.Tests.Add(t); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new{id=t.Id}, t); }
  [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, Test t){ if(id!=t.Id) return BadRequest(); _db.Entry(t).State=EntityState.Modified; await _db.SaveChangesAsync(); return NoContent(); }
  [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id){ var x=await _db.Tests.FindAsync(id); if(x==null) return NotFound(); _db.Remove(x); await _db.SaveChangesAsync(); return NoContent(); }
}