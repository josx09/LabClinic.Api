using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/empleados")]
[Authorize]
public class EmpleadosController : ControllerBase
{
    private readonly LabDbContext _db;
    public EmpleadosController(LabDbContext db) => _db = db;

    // ======================================================
    // DTOs
    // ======================================================

    public record EmpleadoDto(
        int Id,
        string Nombre,
        string Apellido,
        string Sexo,
        string? Telefono,
        string? Correo,
        DateTime? FechaNacimiento,
        string Dpi,
        string FormacionAcademica,
        int? IdMunicipio,
        int? IdDepartamento,
        int Estado
    );

    public record SaveEmpleadoDto(
        int? Id,
        string Nombre,
        string Apellido,
        string Sexo,
        string? Telefono,
        string? Correo,
        DateTime? FechaNacimiento,
        string Dpi,
        string FormacionAcademica,
        int? IdMunicipio,
        int? IdDepartamento,
        int Estado
    );

    // ======================================================
    // GET: Todos los empleados
    // ======================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpleadoDto>>> GetAll()
    {
        var empleados = await _db.Empleados
            .Select(e => new EmpleadoDto(
                e.Id,
                e.Nombre,
                e.Apellido,
                e.Sexo,
                e.Telefono,
                e.Correo,
                e.FechaNacimiento,
                e.Dpi,
                e.FormacionAcademica,
                e.IdMunicipio,
                e.IdDepartamento,
                e.Estado
            ))
            .ToListAsync();

        return Ok(empleados);
    }

    // ======================================================
    // GET: Empleado por ID
    // ======================================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> GetById(int id)
    {
        var e = await _db.Empleados
            .Where(x => x.Id == id)
            .Select(e => new EmpleadoDto(
                e.Id,
                e.Nombre,
                e.Apellido,
                e.Sexo,
                e.Telefono,
                e.Correo,
                e.FechaNacimiento,
                e.Dpi,
                e.FormacionAcademica,
                e.IdMunicipio,
                e.IdDepartamento,
                e.Estado
            ))
            .FirstOrDefaultAsync();

        return e is null ? NotFound() : Ok(e);
    }

    // ======================================================
    // POST: Crear empleado
    // ======================================================
    [HttpPost]
    public async Task<ActionResult<EmpleadoDto>> Create([FromBody] SaveEmpleadoDto dto)
    {
        var e = new Empleado
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Sexo = dto.Sexo,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            FechaNacimiento = dto.FechaNacimiento,
            Dpi = dto.Dpi,
            FormacionAcademica = dto.FormacionAcademica,
            IdMunicipio = dto.IdMunicipio,
            IdDepartamento = dto.IdDepartamento,
            Estado = dto.Estado
        };

        _db.Empleados.Add(e);
        await _db.SaveChangesAsync();

        var result = new EmpleadoDto(
            e.Id,
            e.Nombre,
            e.Apellido,
            e.Sexo,
            e.Telefono,
            e.Correo,
            e.FechaNacimiento,
            e.Dpi,
            e.FormacionAcademica,
            e.IdMunicipio,
            e.IdDepartamento,
            e.Estado
        );

        return CreatedAtAction(nameof(GetById), new { id = e.Id }, result);
    }

    // ======================================================
    // PUT: Actualizar empleado
    // ======================================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveEmpleadoDto dto)
    {
        var e = await _db.Empleados.FindAsync(id);
        if (e is null) return NotFound();

        e.Nombre = dto.Nombre.Trim();
        e.Apellido = dto.Apellido.Trim();
        e.Sexo = dto.Sexo;
        e.Telefono = dto.Telefono;
        e.Correo = dto.Correo;
        e.FechaNacimiento = dto.FechaNacimiento;
        e.Dpi = dto.Dpi;
        e.FormacionAcademica = dto.FormacionAcademica;
        e.IdMunicipio = dto.IdMunicipio;
        e.IdDepartamento = dto.IdDepartamento;
        e.Estado = dto.Estado;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ======================================================
    // DELETE: Eliminar empleado
    // ======================================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Empleados.FindAsync(id);
        if (e is null) return NotFound();

        _db.Empleados.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
