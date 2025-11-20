using LabClinic.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/tipoexamenview")]
public class TipoExamenViewController : ControllerBase
{
    private readonly LabDbContext _db;
    public TipoExamenViewController(LabDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.TipoExamenView.ToListAsync();
        return Ok(data);
    }
}
