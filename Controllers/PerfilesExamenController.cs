using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PerfilesExamenController : ControllerBase
    {
        private readonly LabDbContext _db;

        public PerfilesExamenController(LabDbContext db)
        {
            _db = db;
        }

        // ============================================================
        //  Obtener todos los perfiles (GLOBAL, sin filtro de sucursal)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var perfiles = await _db.PerfilesExamen
                .Include(p => p.PerfilParametros!)
                    .ThenInclude(pp => pp.TipoExamen)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    id_perfil_examen = p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.PrecioTotal,
                    p.PrecioPaquete,
                    Examenes = p.PerfilParametros!.Select(pp => new
                    {
                        IdTipoExamen = pp.TipoExamen!.Id,
                        pp.TipoExamen!.Nombre,
                        pp.TipoExamen!.Precio,
                        Categoria = pp.TipoExamen!.Categoria != null
                            ? pp.TipoExamen!.Categoria.Nombre
                            : "(Sin categoría)"
                    }).ToList()
                })
                .ToListAsync();

            return Ok(perfiles);
        }

        // ============================================================
        //  Obtener lista simple (para dropdowns)
        // ============================================================
        [HttpGet("list")]
        public async Task<IActionResult> GetSimpleList()
        {
            var perfiles = await _db.PerfilesExamen
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    descripcion = p.Descripcion,
                    precioTotal = p.PrecioTotal,
                    precioPaquete = p.PrecioPaquete
                })
                .ToListAsync();

            return Ok(perfiles);
        }

        // ============================================================
        //  Obtener detalle por ID
        // ============================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var perfil = await _db.PerfilesExamen
                .Include(p => p.PerfilParametros!)
                    .ThenInclude(pp => pp.TipoExamen)
                        .ThenInclude(te => te.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfil == null)
                return NotFound(new { message = "❌ Perfil no encontrado" });

            return Ok(new
            {
                id_perfil_examen = perfil.Id,
                perfil.Nombre,
                perfil.Descripcion,
                perfil.PrecioTotal,
                perfil.PrecioPaquete,
                Examenes = perfil.PerfilParametros!.Select(pp => new
                {
                    IdTipoExamen = pp.TipoExamen!.Id,
                    pp.TipoExamen!.Nombre,
                    pp.TipoExamen!.Precio,
                    Categoria = pp.TipoExamen!.Categoria?.Nombre ?? "(Sin categoría)"
                }).ToList()
            });
        }

        // ============================================================
        //  Crear perfil (GLOBAL)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create([FromBody] PerfilExamen model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest(new { message = "El nombre del perfil es obligatorio" });

            if (await _db.PerfilesExamen.AnyAsync(x => x.Nombre == model.Nombre))
                return Conflict(new { message = "Ya existe un perfil con ese nombre." });

            model.PrecioTotal = model.PrecioTotal == 0 ? 0 : model.PrecioTotal;
            model.PrecioPaquete = model.PrecioPaquete == 0 ? model.PrecioTotal : model.PrecioPaquete;

            _db.PerfilesExamen.Add(model);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✅ Perfil creado correctamente.", id_perfil_examen = model.Id, perfil = model });
        }

        // ============================================================
        //  Asignar o actualizar exámenes
        // ============================================================
        [HttpPost("{id:int}/asignar")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> AsignarExamenes(int id, [FromBody] JsonElement body, [FromQuery] bool append = false)
        {
            var perfil = await _db.PerfilesExamen
                .Include(p => p.PerfilParametros!)
                    .ThenInclude(pp => pp.TipoExamen)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfil == null)
                return NotFound(new { message = "❌ Perfil no encontrado." });

            var idsExamenes = body.ValueKind == JsonValueKind.Array
                ? body.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                    .Select(e => e.GetInt32())
                    .ToList()
                : new List<int>();

            if (idsExamenes.Count == 0)
                return BadRequest(new { message = "❌ No se enviaron IDs válidos de exámenes." });

            if (!append)
            {
                var actuales = perfil.PerfilParametros!
                    .Where(p => !idsExamenes.Contains(p.IdTipoExamen))
                    .ToList();
                _db.PerfilParametros.RemoveRange(actuales);
            }

            var existentesIds = perfil.PerfilParametros!.Select(p => p.IdTipoExamen).ToHashSet();
            var nuevos = idsExamenes.Where(x => !existentesIds.Contains(x)).ToList();

            foreach (var idExamen in nuevos)
            {
                _db.PerfilParametros.Add(new PerfilParametro
                {
                    IdPerfilExamen = id,
                    IdTipoExamen = idExamen
                });
            }

            var todosIds = perfil.PerfilParametros!.Select(pp => pp.IdTipoExamen).Union(idsExamenes).ToList();
            var precios = await _db.TiposExamen
                .Where(t => todosIds.Contains(t.Id))
                .Select(t => t.Precio ?? 0m)
                .ToListAsync();

            perfil.PrecioTotal = precios.Sum();
            if (perfil.PrecioPaquete == 0 || perfil.PrecioPaquete > perfil.PrecioTotal)
                perfil.PrecioPaquete = perfil.PrecioTotal;

            await _db.SaveChangesAsync();

            return Ok(new { message = append ? "✅ Exámenes añadidos al perfil." : "✅ Exámenes actualizados correctamente." });
        }

        // ============================================================
        //  Quitar examen de un perfil
        // ============================================================
        [HttpDelete("{idPerfil:int}/examen/{idExamen:int}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> RemoveExamenFromPerfil(int idPerfil, int idExamen)
        {
            var perfil = await _db.PerfilesExamen
                .Include(p => p.PerfilParametros!)
                .FirstOrDefaultAsync(p => p.Id == idPerfil);

            if (perfil == null)
                return NotFound(new { message = "❌ Perfil no encontrado." });

            var relacion = perfil.PerfilParametros!.FirstOrDefault(pp => pp.IdTipoExamen == idExamen);
            if (relacion == null)
                return NotFound(new { message = "⚠️ Este examen no está asociado al perfil." });

            _db.PerfilParametros.Remove(relacion);

            var idsRestantes = perfil.PerfilParametros!
                .Where(pp => pp.IdTipoExamen != idExamen)
                .Select(pp => pp.IdTipoExamen)
                .ToList();

            var precios = await _db.TiposExamen
                .Where(t => idsRestantes.Contains(t.Id))
                .Select(t => t.Precio ?? 0)
                .ToListAsync();

            perfil.PrecioTotal = precios.Sum();
            if (perfil.PrecioPaquete > perfil.PrecioTotal)
                perfil.PrecioPaquete = perfil.PrecioTotal;

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Examen eliminado correctamente.", nuevo_total = perfil.PrecioTotal });
        }

        // ============================================================
        //  Actualizar perfil
        // ============================================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Update(int id, [FromBody] PerfilExamen model)
        {
            var perfil = await _db.PerfilesExamen.FirstOrDefaultAsync(p => p.Id == id);
            if (perfil == null)
                return NotFound(new { message = "❌ Perfil no encontrado." });

            perfil.Nombre = model.Nombre;
            perfil.Descripcion = model.Descripcion;
            perfil.PrecioTotal = model.PrecioTotal != 0 ? model.PrecioTotal : perfil.PrecioTotal;
            perfil.PrecioPaquete = model.PrecioPaquete != 0 ? model.PrecioPaquete : perfil.PrecioPaquete;

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Perfil actualizado correctamente." });
        }

        // ============================================================
        //  Eliminar perfil
        // ============================================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var perfil = await _db.PerfilesExamen
                .Include(p => p.PerfilParametros)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perfil == null)
                return NotFound(new { message = "❌ Perfil no encontrado." });

            _db.PerfilParametros.RemoveRange(perfil.PerfilParametros!);
            _db.PerfilesExamen.Remove(perfil);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Perfil eliminado correctamente." });
        }
    }
}
