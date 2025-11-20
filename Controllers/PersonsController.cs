using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabClinic.Api.Common; 


namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/personas")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx; 

    public PersonsController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx;
    }


    // ==========================================================
    //  Obtener todas las personas (activas e inactivas)
    [HttpGet]
    public IActionResult GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _db.Persons
            .Include(p => p.Direccion)
                .ThenInclude(d => d.Municipio)
                .ThenInclude(m => m.Departamento)
            .WhereSucursal(_sucCtx) 
            .AsQueryable();


        // Filtro opcional por rango de fechas
        if (desde.HasValue && hasta.HasValue)
        {
            var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(p => p.FechaRegistro >= desde.Value.Date && p.FechaRegistro <= hastaFinal);
        }

        //   Obtener las fechas de última visita de cada persona
        var ultimosExamenes = _db.Examenes
            .GroupBy(e => e.IdPersona)
            .Select(g => new
            {
                IdPersona = g.Key,
                UltimaVisita = g.Max(e => e.FechaRegistro)
            })
            .ToList();

        //   Obtener todas las personas y unir en memoria con su última visita
        var persons = query
            .AsNoTracking()
            .ToList() 
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Apellido,
                p.Sexo,
                p.Telefono,
                p.Correo,
                p.Dpi,
                p.FechaNacimiento,
                p.FechaRegistro,
                p.Observaciones,
                p.Estado,
                p.TipoCliente,
                UltimaVisita = ultimosExamenes.FirstOrDefault(u => u.IdPersona == p.Id)?.UltimaVisita,
                Direccion = p.Direccion != null ? new
                {
                    p.Direccion.Id,
                    p.Direccion.IdMunicipio,
                    p.Direccion.Calle,
                    p.Direccion.Numero,
                    p.Direccion.Zona,
                    p.Direccion.Referencia
                } : null
            })
            //  Ordenar por última visita o por fecha de registro (descendente)
            .OrderByDescending(p => p.UltimaVisita ?? p.FechaRegistro)
            .ToList();

        return Ok(persons);
    }





    // ==========================================================
    //  Obtener persona por ID
    // ==========================================================
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var person = _db.Persons
            .Include(p => p.Direccion)
                .ThenInclude(d => d.Municipio)
                .ThenInclude(m => m.Departamento)
            .AsEnumerable()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Apellido,
                p.Sexo,
                p.Telefono,
                p.Correo,
                p.Dpi,
                p.FechaNacimiento,
                p.Observaciones,
                p.Estado,
                Direccion = p.Direccion != null
                    ? $"{(p.Direccion.Referencia ?? "(sin descripción)")}, " +
                      $"{(p.Direccion.Municipio != null ? p.Direccion.Municipio.Nombre : "")}, " +
                      $"{(p.Direccion.Municipio?.Departamento?.Nombre ?? "")}"
                    : null
            })
            .FirstOrDefault();

        if (person == null)
            return NotFound(new { message = "❌ Persona no encontrada." });

        return Ok(person);
    }

    // ==========================================================
    //  Crear persona
    // ==========================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Person person)
    {
        if (person == null)
            return BadRequest(new { message = "❌ Datos inválidos." });

        person.Estado = 1;

        if (person.FechaNacimiento == default(DateTime))
            person.FechaNacimiento = null;

        if (person.TipoCliente != 1)
            person.TipoCliente = 0;

        if (person.IdUsuarioCliente.HasValue)
        {
            var usuario = await _db.Users.FindAsync(person.IdUsuarioCliente.Value);
            if (usuario == null)
                return BadRequest(new { message = "El usuario cliente especificado no existe." });
        }

        person.FechaRegistro = DateTime.Now;

        //  Asignar sucursal antes de guardar
        _db.StampSucursal(_sucCtx);
        _db.Persons.Add(person);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Persona registrada correctamente.", person });
    }




    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Person updated)
    {
        if (updated == null)
            return BadRequest(new { message = "❌ Datos inválidos." });

        var existing = await _db.Persons
            .Include(p => p.Direccion)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing == null)
            return NotFound(new { message = "❌ Persona no encontrada." });

        //  Actualiza los datos básicos
        existing.Nombre = updated.Nombre;
        existing.Apellido = updated.Apellido;
        existing.Sexo = updated.Sexo;
        existing.Telefono = updated.Telefono;
        existing.Correo = updated.Correo;
        existing.Dpi = updated.Dpi;
        existing.FechaNacimiento = updated.FechaNacimiento;
        existing.Observaciones = updated.Observaciones;
        existing.Estado = updated.Estado;
        existing.TipoCliente = updated.TipoCliente;

        existing.IdUsuarioCliente = updated.IdUsuarioCliente;



        // Si no existe dirección, la crea
        if (existing.Direccion == null && updated.Direccion != null)
        {
            existing.Direccion = new Direccion
            {
                IdMunicipio = updated.Direccion.IdMunicipio,
                Calle = updated.Direccion.Calle,
                Numero = updated.Direccion.Numero,
                Zona = updated.Direccion.Zona,
                Referencia = updated.Direccion.Referencia
            };
        }
        else if (existing.Direccion != null && updated.Direccion != null)
        {
            // Si ya existe dirección, la actualiza
            existing.Direccion.IdMunicipio = updated.Direccion.IdMunicipio;
            existing.Direccion.Calle = updated.Direccion.Calle;
            existing.Direccion.Numero = updated.Direccion.Numero;
            existing.Direccion.Zona = updated.Direccion.Zona;
            existing.Direccion.Referencia = updated.Direccion.Referencia;
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "✏️ Persona y dirección actualizadas correctamente.", persona = existing });
    }



    // ==========================================================
    //  Eliminar persona (lógica o física)
    // ==========================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hard = false)
    {
        var person = await _db.Persons
            .Include(p => p.Direccion)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null)
            return NotFound(new { message = "❌ Persona no encontrada." });

        if (hard)
        {
            var examenes = await _db.Examenes.Where(e => e.IdPersona == id).ToListAsync();
            if (examenes.Any())
            {
                var idsExamenes = examenes.Select(e => e.Id).ToList();
                var parametros = await _db.ParametrosExamen
                    .Where(px => idsExamenes.Contains(px.IdExamen))
                    .ToListAsync();

                if (parametros.Any())
                    _db.ParametrosExamen.RemoveRange(parametros);

                _db.Examenes.RemoveRange(examenes);
            }

            if (person.Direccion != null)
                _db.Direcciones.Remove(person.Direccion);

            _db.Persons.Remove(person);
            await _db.SaveChangesAsync();

            return Ok(new { message = "💣 Persona y todos sus exámenes fueron eliminados definitivamente." });
        }

        person.Estado = 0;
        _db.Persons.Update(person);

        var exs = await _db.Examenes
            .Where(e => e.IdPersona == id)
            .WhereSucursal(_sucCtx) 
            .ToListAsync();

        foreach (var ex in exs)
            ex.Estado = 0;

        if (exs.Any())
            _db.Examenes.UpdateRange(exs);

        await _db.SaveChangesAsync();

        return Ok(new { message = "🚫 Persona y sus exámenes fueron desactivados (eliminación lógica)." });
    }

    // ==========================================================
    //  Reactivar persona
    // ==========================================================
    [HttpPut("{id}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var person = await _db.Persons.FindAsync(id);
        if (person == null)
            return NotFound(new { message = "❌ Persona no encontrada." });

        person.Estado = 1;
        _db.Persons.Update(person);

        var exs = await _db.Examenes
            .Where(e => e.IdPersona == id)
            .WhereSucursal(_sucCtx)
            .ToListAsync();

        foreach (var ex in exs)
            ex.Estado = 1;

        if (exs.Any())
            _db.Examenes.UpdateRange(exs);

        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Persona y exámenes reactivados correctamente." });
    }
}
