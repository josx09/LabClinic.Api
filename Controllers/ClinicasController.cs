using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabClinic.Api.Common;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicasController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx; //  contexto de sucursal

        public ClinicasController(LabDbContext db, ISucursalContext sucCtx)
        {
            _db = db;
            _sucCtx = sucCtx;
        }

        //  Obtener todas las clínicas filtradas por sucursal
        [HttpGet]
        public IActionResult GetAll()
        {
            var clinicas = _db.Clinicas
                .WhereSucursal(_sucCtx) // filtro por sucursal activa
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Telefono,
                    c.Contacto,
                    c.Direccion,
                    Estado = c.Activo == 1
                })
                .ToList();

            return Ok(clinicas);
        }

        //  Obtener clínica por ID (solo si pertenece a la sucursal)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var clinica = await _db.Clinicas
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clinica == null)
                return NotFound(new { message = "Clínica no encontrada en esta sucursal." });

            return Ok(new
            {
                clinica.Id,
                clinica.Nombre,
                clinica.Direccion,
                clinica.Telefono,
                clinica.Contacto,
                Estado = clinica.Activo == 1
            });
        }

        //  Crear nueva clínica (asignando sucursal actual)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Clinica model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //  Asigna automáticamente la sucursal actual
            _db.StampSucursal(_sucCtx);
            _db.Clinicas.Add(model);

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Clínica registrada correctamente en la sucursal actual." });
        }

        //  Editar clínica existente (solo si pertenece a esta sucursal)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Clinica model)
        {
            var clinica = await _db.Clinicas
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clinica == null)
                return NotFound(new { message = "❌ No se puede editar: la clínica no pertenece a esta sucursal." });

            clinica.Nombre = model.Nombre;
            clinica.Direccion = model.Direccion;
            clinica.Telefono = model.Telefono;
            clinica.Contacto = model.Contacto;
            clinica.Activo = (byte)(model.Activo == 1 ? 1 : 0);

            await _db.SaveChangesAsync();
            return Ok(new { message = "✏️ Clínica actualizada correctamente." });
        }

        // Eliminar clínica (lógica o física)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool hard = false)
        {
            var clinica = await _db.Clinicas
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (clinica == null)
                return NotFound(new { message = "❌ Clínica no encontrada en esta sucursal." });

            if (hard)
            {
                _db.Clinicas.Remove(clinica);
                await _db.SaveChangesAsync();
                return Ok(new { message = "💣 Clínica eliminada permanentemente." });
            }

            clinica.Activo = 0;
            await _db.SaveChangesAsync();
            return Ok(new { message = "🚫 Clínica desactivada correctamente." });
        }
    }
}
