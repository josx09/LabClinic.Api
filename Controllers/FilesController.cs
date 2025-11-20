using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
  private readonly LabDbContext _db;
  private readonly IWebHostEnvironment _env;
  public FilesController(LabDbContext db, IWebHostEnvironment env){ _db=db; _env=env; }

  [HttpPost("appointment/{id:int}/prescription")]
  public async Task<IActionResult> UploadPrescription(int id, IFormFile file)
  {
    var ap = await _db.Appointments.FirstOrDefaultAsync(a=>a.Id==id);
    if(ap==null) return NotFound();

    var dir = Path.Combine(_env.ContentRootPath, "uploads", "prescriptions");
    Directory.CreateDirectory(dir);
    var fname = $"{id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{file.FileName}".Replace(" ", "_");
    var path = Path.Combine(dir, fname);
    using(var fs = new FileStream(path, FileMode.Create)){
      await file.CopyToAsync(fs);
    }
    // Guardar ruta relativa
    ap.GetType().GetProperty("PrescriptionPath")?.SetValue(ap, $"/uploads/prescriptions/{fname}");
    await _db.SaveChangesAsync();
    return Ok(new { path = $"/uploads/prescriptions/{fname}" });
  }
}