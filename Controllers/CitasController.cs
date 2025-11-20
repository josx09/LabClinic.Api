using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/citas")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx;

    public CitasController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx;
    }

    // =============================
    // Query DTO
    // =============================
    public class Query
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? PacienteId { get; set; }
        public int? MedicoId { get; set; }
        public DateOnly? Fecha { get; set; }
    }

    // =============================
    // GET: api/citas
    // =============================
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Query q)
    {
        var set = _db.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
            .WhereSucursal(_sucCtx)
            .AsQueryable();

        if (q.PacienteId is int pid && pid > 0)
            set = set.Where(c => c.IdPaciente == pid);

        if (q.MedicoId is int mid && mid > 0)
            set = set.Where(c => c.IdMedico == mid);

        if (q.Fecha is DateOnly f)
            set = set.Where(c => EF.Functions.DateDiffDay(c.Fecha, f.ToDateTime(TimeOnly.MinValue)) == 0);

        var total = await set.CountAsync();

        var items = await set
            .OrderByDescending(c => c.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(c => new
            {
                id_cita = c.Id,
                id_paciente = c.IdPaciente,
                paciente_nombre = c.Paciente != null ? c.Paciente.Nombre + " " + c.Paciente.Apellido : "(Sin registro)",
                id_medico = c.IdMedico,
                medico_nombre = c.Medico != null ? c.Medico.Firstname + " " + c.Medico.Lastname : "(No asignado)",
                fecha = c.Fecha,
                estado_cita = c.EstadoCita
            })
            .ToListAsync();

        return Ok(new { total, items });
    }

    // =============================
    // GET: api/citas/{id}
    // =============================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cita = await _db.Citas
            .AsNoTracking()
            .WhereSucursal(_sucCtx) 
            .FirstOrDefaultAsync(c => c.Id == id);

        return cita is null ? NotFound() : Ok(cita);
    }

    public class SaveCita
    {
        public int Id { get; set; }
        public int? IdPaciente { get; set; }
        public int? IdMedico { get; set; }
        public DateTime Fecha { get; set; }
        public int EstadoCita { get; set; } = 1;
    }

    // =============================
    // POST: api/citas
    // =============================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCita dto)
    {
        if (dto.Fecha == default)
            return BadRequest(new { message = "La fecha es obligatoria." });

        // Si no se pasó IdPaciente, intentamos resolverlo con el usuario autenticado (cliente)
        if (dto.IdPaciente == null || dto.IdPaciente <= 0)
        {
            int userId = int.TryParse(User.FindFirst("uid")?.Value, out var uid) ? uid : 0;
            if (userId == 0)
                return BadRequest(new { message = "No se pudo identificar al usuario autenticado." });

            // Busca la persona vinculada a ese usuario
            var personId = await _db.Personas
                .WhereSucursal(_sucCtx) 
                .Where(p => p.IdUsuarioCliente == userId || p.IdUsuario == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (personId == 0)
                return BadRequest(new { message = "No se encontró un paciente vinculado al usuario." });

            dto.IdPaciente = personId;
        }

        var entity = new Cita
        {
            IdPaciente = dto.IdPaciente ?? 0,
            IdMedico = dto.IdMedico,
            Fecha = dto.Fecha,
            EstadoCita = dto.EstadoCita
            
        };

      
        

        _db.Citas.Add(entity);
        _db.StampSucursal(_sucCtx);
        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    // =============================
    // PUT: api/citas/{id}
    // =============================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCita dto)
    {
        var entity = await _db.Citas
            .WhereSucursal(_sucCtx) //  proteger contra edición de otra sucursal
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity is null)
            return NotFound();

        if (dto.IdPaciente <= 0 || dto.Fecha == default)
            return BadRequest(new { message = "IdPaciente y Fecha son obligatorios" });

        entity.IdPaciente = dto.IdPaciente ?? 0;
        entity.IdMedico = dto.IdMedico;
        entity.Fecha = dto.Fecha;
        entity.EstadoCita = dto.EstadoCita;

        _db.Entry(entity).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    // =============================
    // DELETE: api/citas/{id}
    // =============================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Citas
            .WhereSucursal(_sucCtx) //  sólo de la sucursal actual
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity is null) return NotFound();

        _db.Citas.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cita eliminada." });
    }

    // =============================
    // GET: api/citas/mias  (citas del cliente autenticado)
    // =============================
    [HttpGet("mias")]
    public async Task<IActionResult> MisCitas()
    {
        int userId = int.TryParse(User.FindFirst("uid")?.Value, out var uid) ? uid : 0;
        if (userId == 0) return Ok(Array.Empty<object>());

        int personId = await _db.Personas
            .WhereSucursal(_sucCtx)
            .Where(p => p.IdUsuarioCliente == userId || p.IdUsuario == userId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        if (personId == 0) return Ok(Array.Empty<object>());

        var citasCliente = await _db.Citas
            .AsNoTracking()
            .WhereSucursal(_sucCtx)
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
            .Where(c => c.IdPaciente == personId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new
            {
                id_cita = c.Id,
                id_paciente = c.IdPaciente,
                paciente_nombre = c.Paciente != null
                    ? c.Paciente.Nombre + " " + c.Paciente.Apellido
                    : "(Sin registro)",

                id_medico = c.IdMedico,
                medico_nombre = c.Medico != null
                    ? c.Medico.Firstname + " " + c.Medico.Lastname
                    : "(No asignado)",

                fecha = c.Fecha,
                estado_cita = c.EstadoCita
            })
            .ToListAsync();

        return Ok(citasCliente);
    }

}
