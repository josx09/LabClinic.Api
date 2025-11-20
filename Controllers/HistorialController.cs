using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/historial")]
public class HistorialController : ControllerBase
{
    private readonly LabDbContext _db;

    public HistorialController(LabDbContext db)
    {
        _db = db;
    }

  
    [HttpGet("paciente/{id:int}")]
    public async Task<IActionResult> GetHistorialPorPaciente(int id)
    {
        var historial = await _db.Examenes
            .Where(e => e.IdPersona == id)
            .Include(e => e.TipoExamen)
            .Include(e => e.Clinica)
            .GroupBy(e => e.FechaRegistro.Date)
            .Select(g => new
            {
                Fecha = g.Key,
                Examenes = g.Select(e => new
                {
                    e.Id,
                    TipoExamen = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                    Clinica = e.Clinica != null ? e.Clinica.Nombre : "(Sin clínica)",
                    e.PrecioAplicado,
                    e.Resultado,
                    e.Estado
                }).ToList()
            })
            .OrderByDescending(g => g.Fecha)
            .ToListAsync();

        return Ok(historial);
    }

    [HttpGet("paciente/mi-historial")]
    [Authorize(Roles = "Cliente,Paciente,User,Usuario,customer,client")] 
    public async Task<IActionResult> GetHistorialDelCliente()
    {
        //  Obtener ID del usuario autenticado desde el token
        var userIdStr = User.FindFirstValue("uid"); 
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized(new { message = "Usuario no autenticado." });

        int userId = int.Parse(userIdStr);

        //  Buscar persona vinculada al usuario
        var persona = await _db.Persons.FirstOrDefaultAsync(p => p.IdUsuarioCliente == userId);
        if (persona == null)
            return NotFound(new { message = "No se encontró persona vinculada a este usuario." });

        //  Obtener historial de esa persona
        var historial = await _db.Examenes
            .Where(e => e.IdPersona == persona.Id)
            .Include(e => e.TipoExamen)
            .Include(e => e.Clinica)
            .GroupBy(e => e.FechaRegistro.Date)
            .Select(g => new
            {
                Fecha = g.Key,
                Examenes = g.Select(e => new
                {
                    e.Id,
                    TipoExamen = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                    Clinica = e.Clinica != null ? e.Clinica.Nombre : "(Sin clínica)",
                    e.PrecioAplicado,
                    e.Resultado,
                    e.Estado
                }).ToList()
            })
            .OrderByDescending(g => g.Fecha)
            .ToListAsync();

        return Ok(historial);
    }
}
