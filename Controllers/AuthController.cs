using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LabClinic.Api.Data;
using LabClinic.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly ISucursalContext _sucCtx; 

    public AuthController(LabDbContext db, IConfiguration cfg, ISucursalContext sucCtx)
    {
        _db = db;
        _cfg = cfg;
        _sucCtx = sucCtx;
    }

    public record LoginRequest(string username, string password);

    // ===========================================================
    //  LOGIN
    // ============================================================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.username) || string.IsNullOrWhiteSpace(req.password))
            return BadRequest(new { message = "Usuario y contraseña son requeridos" });

        //  Buscar usuario activo con su rol
        var user = await _db.Users
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Username == req.username && u.Status == 1);

        if (user == null)
            return Unauthorized(new { message = "Usuario o contraseña inválidos" });

        // ============================================================
        //  Verificar contraseña (BCrypt / MD5 / texto plano)
        // ============================================================
        bool ok = false;
        var stored = user.PasswordHash ?? "";
        try
        {
            if (!string.IsNullOrEmpty(stored) && stored.StartsWith("$2"))
                ok = BCrypt.Net.BCrypt.Verify(req.password, stored);
            else if (!string.IsNullOrEmpty(stored) && stored.Length == 32 &&
                     stored.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                using var md5 = MD5.Create();
                var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(req.password))).ToLower();
                ok = string.Equals(hash, stored, StringComparison.OrdinalIgnoreCase);
            }
            else
                ok = stored == req.password;
        }
        catch { ok = false; }

        if (!ok)
            return Unauthorized(new { message = "Usuario o contraseña inválidos" });

        // ============================================================
        //  Determinar rol y persona asociada (si aplica)
        // ============================================================
        string roleName = user.Rol?.Nombre ?? user.Type switch
        {
            1 => "Administrador",
            2 => "Usuario",
            3 => "Médico",
            4 => "Cliente",
            _ => "Usuario"
        };

        int? idPersona = null;
        if (roleName == "Cliente")
        {
            idPersona = await _db.Persons
                .Where(p => p.Correo == user.Username || p.Telefono == user.Username)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        //  Determinar sucursal activa desde el contexto
        // ============================================================
        int sucursalId = _sucCtx.CurrentSucursalId;
        string sucursalNombre = "(Sucursal desconocida)";
        if (sucursalId > 0)
        {
            var s = await _db.Sucursales.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sucursalId);
            if (s != null) sucursalNombre = s.Nombre;
        }

        // ============================================================
        //  Generar token JWT
        // ============================================================
        var keyStr = _cfg["Jwt:Key"] ?? "dev-key-123456789012345678901234567890";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("uid", user.Id.ToString()),
            new Claim("type", user.Type.ToString()),
            new Claim("roleId", user.IdRol.ToString()),
            new Claim(ClaimTypes.Role, roleName),
            //  Nueva claim: sucursal activa
            new Claim("id_sucursal", sucursalId.ToString())
        };

        if (idPersona.HasValue)
            claims.Add(new Claim("idPersona", idPersona.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        // ============================================================
        //  Respuesta al frontend
        // ============================================================
        return Ok(new
        {
            token = jwt,
            type = user.Type,
            name = $"{user.Firstname} {user.Lastname}".Trim(),
            rol = roleName,
            idPersona = idPersona,
            idSucursal = sucursalId,
            sucursal = sucursalNombre
        });
    }

    // ============================================================
    //  Registro de usuarios (clientes)
    // ============================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] User req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) ||
            string.IsNullOrWhiteSpace(req.PasswordHash) ||
            string.IsNullOrWhiteSpace(req.Firstname))
            return BadRequest(new { message = "Todos los campos son obligatorios." });

        var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
        if (exists)
            return BadRequest(new { message = "El nombre de usuario ya está en uso." });

        // Solo permitir registro de tipo cliente (rol 4)
        req.Type = 4;
        req.IdRol = 4;
        req.Status = 1;
        req.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.PasswordHash);

        _db.Users.Add(req);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Cuenta creada correctamente. Ya puedes iniciar sesión." });
    }
}
