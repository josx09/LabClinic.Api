using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly LabDbContext _db;
    public UsersController(LabDbContext db) { _db = db; }

    // DTO de salida (lo que se envía al frontend, sin password)
    public record UserDto(
        int Id,
        string Username,
        string Firstname,
        string Lastname,
        int Status,
        int Type,
        string? Rol
    );

    // DTO de entrada (lo que envía Angular al crear/editar)
    public record SaveUserDto(
        int? Id,
        string Username,
        string Firstname,
        string Lastname,
        int IdRol,
        int Status,
        string? Password
    );

    // ==========================================================
    // GET: lista de usuarios
    // ==========================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get()
    {
        var users = await _db.Users
            .Include(u => u.Rol)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Firstname,
                u.Lastname,
                u.Status,
                u.Type,
                u.Rol != null ? u.Rol.Nombre : null
            ))
            .ToListAsync();

        return Ok(users);
    }

    // ==========================================================
    // GET: usuario por Id
    // ==========================================================
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var u = await _db.Users
            .Include(x => x.Rol)
            .Where(x => x.Id == id)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Firstname,
                u.Lastname,
                u.Status,
                u.Type,
                u.Rol != null ? u.Rol.Nombre : null
            ))
            .FirstOrDefaultAsync();

        return u is null ? NotFound() : Ok(u);
    }

    // ==========================================================
    // POST: crear usuario
    // ==========================================================
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(SaveUserDto dto)
    {
        var u = new User
        {
            Username = dto.Username,
            Firstname = dto.Firstname,
            Lastname = dto.Lastname,
            IdRol = dto.IdRol,
            Status = dto.Status,
            Type = 1 
        };

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        }
        else
        {
            u.PasswordHash = "";
        }

        _db.Users.Add(u);
        await _db.SaveChangesAsync();

        var roleName = (await _db.Roles.FindAsync(u.IdRol))?.Nombre;
        var userDto = new UserDto(u.Id, u.Username, u.Firstname, u.Lastname, u.Status, u.Type, roleName);

        return CreatedAtAction(nameof(GetById), new { id = u.Id }, userDto);
    }

    // ==========================================================
    // PUT: actualizar usuario
    // ==========================================================
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveUserDto dto)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        // solo si no vienen nulos o vacíos
        if (!string.IsNullOrWhiteSpace(dto.Username))
            u.Username = dto.Username;

        if (!string.IsNullOrWhiteSpace(dto.Firstname))
            u.Firstname = dto.Firstname;

        if (!string.IsNullOrWhiteSpace(dto.Lastname))
            u.Lastname = dto.Lastname;

        if (dto.IdRol > 0)
            u.IdRol = dto.IdRol;

        // este siempre, porque el status siempre viene en el request
        u.Status = dto.Status;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        await _db.SaveChangesAsync();
        return NoContent();
    }


    // ==========================================================
    // DELETE: eliminar usuario
    // ==========================================================
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        _db.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET: api/users/medicos
    [HttpGet("medicos")]
    [AllowAnonymous] // Esto permite acceder sin token
    public async Task<IActionResult> GetMedicos()
    {
        var medicos = await _db.Users
            .Include(u => u.Rol)
            .Where(u => u.Rol.Nombre == "Médico") // nombre exacto del rol
            .Select(u => new
            {
                id = u.Id,
                nombre = u.Firstname,
                apellido = u.Lastname,
                username = u.Username
            })
            .ToListAsync();

        return Ok(medicos);
    }

    // ==========================================================
    // GET: api/users/buscar?search=texto
    //  Devuelve solo usuarios con rol "Cliente"
    // ==========================================================
    [HttpGet("buscar")]
    [AllowAnonymous] // si quieres que Angular pueda acceder sin token
    public async Task<IActionResult> BuscarUsuarios([FromQuery] string? search)
    {
        var query = _db.Users
            .Include(u => u.Rol)
            .Where(u => u.Rol.Nombre == "Cliente"); //  solo rol Cliente

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.Firstname.Contains(search) ||
                u.Lastname.Contains(search));
        }

        var usuarios = await query
            .OrderBy(u => u.Username)
            .Take(15) // limitar resultados
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                firstname = u.Firstname,
                lastname = u.Lastname,
                rol = u.Rol.Nombre
            })
            .ToListAsync();

        return Ok(usuarios);
    }


}
