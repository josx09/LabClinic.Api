using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/pacientes")]
    [Authorize]
    public class PacientesController : ControllerBase
    {
        private readonly LabDbContext _db;
        public PacientesController(LabDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetPacientes()
        {
            var pacientes = await _db.Persons
                .Where(p => p.Estado == 1)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombre + " " + p.Apellido,
                    p.Correo,
                    p.Telefono,
                    p.Sexo
                })
                .ToListAsync();

            return Ok(pacientes);
        }
    }
}
