using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BonosController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx;

        public BonosController(LabDbContext db, ISucursalContext sucCtx)
        {
            _db = db;
            _sucCtx = sucCtx;
        }

        // ==========================================================
        // Obtener todos los bonos (solo de la sucursal actual)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.Bonos
                .Include(b => b.Medico)
                .Include(b => b.Persona)
                .WhereSucursal(_sucCtx)
                .AsQueryable();

            if (desde.HasValue && hasta.HasValue)
                query = query.Where(b => b.FechaRegistro >= desde && b.FechaRegistro <= hasta);

            var bonos = await query
                .Include(b => b.Medico)
                .Include(b => b.Persona)
                .OrderByDescending(b => b.FechaRegistro)
                .AsNoTracking()
                .ToListAsync();

            var resultado = bonos.Select(b => new
            {
                b.IdBono,
                b.IdMedico,
                b.IdPersona,
                MedicoNombre = b.Medico != null ? $"{b.Medico.Firstname} {b.Medico.Lastname}" : "(Sin médico)",
                PersonaNombre = b.Persona != null ? $"{b.Persona.Nombre} {b.Persona.Apellido}" : "(Sin paciente)",
                b.Porcentaje,
                b.MontoBono,
                b.Estado,
                b.Pagado,
                FechaRegistro = b.FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss")
            });


            return Ok(resultado);
        }


        // ==========================================================
        //  Crear nuevo bono (sucursal actual)
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Bono bono)
        {
            if (bono == null || bono.IdMedico == null)
                return BadRequest(new { message = "❌ Debe seleccionar un médico." });

            //  Verifica que la persona pertenezca a la misma sucursal
            var persona = await _db.Persons
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(p => p.Id == bono.IdPersona);
            if (persona == null)
                return BadRequest(new { message = "❌ El paciente no pertenece a esta sucursal." });

            //  Calcular el total de exámenes del paciente en esta sucursal
            var totalExamenes = await _db.Examenes
                .Where(e => e.IdPersona == bono.IdPersona)
                .WhereSucursal(_sucCtx)
                .Join(_db.TiposExamen,
                    e => e.IdTipoExamen,
                    t => t.Id,
                    (e, t) => t.Precio ?? 0)
                .SumAsync();

            bono.MontoBono = totalExamenes * (bono.Porcentaje / 100);
            bono.NombreBono = $"Bono {bono.Porcentaje}% Médico {bono.IdMedico}";
            bono.FechaRegistro = DateTime.Now;
            bono.Pagado = bono.Pagado;


            //  Registrar la sucursal actual
            _db.StampSucursal(_sucCtx);

            _db.Bonos.Add(bono);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "✅ Bono calculado y registrado correctamente.",
                bono.MontoBono,
                bono.Pagado,
                Fecha = bono.FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalExamenes = totalExamenes
            });
        }

        // ==========================================================
        //  Actualizar bono
        // ==========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Bono bono)
        {
            var existing = await _db.Bonos
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(b => b.IdBono == id);

            if (existing == null)
                return NotFound(new { message = "❌ Bono no encontrado o pertenece a otra sucursal." });

            _db.Entry(existing).CurrentValues.SetValues(bono);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✏️ Bono actualizado correctamente." });
        }

        // ==========================================================
        //  Eliminar bono
        // ==========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var bono = await _db.Bonos
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(b => b.IdBono == id);

            if (bono == null)
                return NotFound(new { message = "❌ Bono no encontrado o pertenece a otra sucursal." });

            _db.Bonos.Remove(bono);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Bono eliminado correctamente." });
        }

        // ==========================================================
        //  Filtrar bonos por rango de fechas
        // ==========================================================
        [HttpGet("filtrar")]
        public async Task<IActionResult> Filtrar([FromQuery] DateTime? inicio, [FromQuery] DateTime? fin)
        {
            var query = _db.Bonos
                .Include(b => b.Medico)
                .Include(b => b.Persona)
                .WhereSucursal(_sucCtx)
                .AsQueryable();

            if (inicio.HasValue && fin.HasValue)
                query = query.Where(b => b.FechaRegistro >= inicio && b.FechaRegistro <= fin);

            var bonos = await query.Select(b => new
            {
                b.IdBono,
                b.IdMedico,
                b.IdPersona,
                MedicoNombre = b.Medico != null ? $"{b.Medico.Firstname ?? ""} {b.Medico.Lastname ?? ""}".Trim() : "(Sin médico)",
                PersonaNombre = b.Persona != null ? $"{b.Persona.Nombre ?? ""} {b.Persona.Apellido ?? ""}".Trim() : "(Sin paciente)",
                b.Porcentaje,
                b.MontoBono,
                b.Estado,
                b.Pagado,
                Fecha = b.FechaRegistro.ToString("yyyy-MM-dd")
            }).ToListAsync();

            return Ok(bonos);
        }

        // ==========================================================
        //  Marcar un bono como pagado
        // ==========================================================
        [HttpPut("{id}/pagado")]
        public async Task<IActionResult> MarcarComoPagado(int id)
        {
            var bono = await _db.Bonos
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(b => b.IdBono == id);

            if (bono == null)
                return NotFound(new { message = "❌ Bono no encontrado o pertenece a otra sucursal." });

            bono.Pagado = true;
            bono.FechaPago = DateTime.Now;

            _db.Bonos.Update(bono);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✅ Bono marcado como pagado correctamente." });
        }
    }
}
