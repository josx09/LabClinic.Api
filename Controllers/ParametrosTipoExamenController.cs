using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/parametros-tipo-examen")]
    [Authorize]
    public class ParametrosTipoExamenController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx;

        public ParametrosTipoExamenController(LabDbContext db, ISucursalContext sucCtx)
        {
            _db = db;
            _sucCtx = sucCtx;
        }

        // ==========================================================
        // Obtener parámetros (con soporte de filtro por tipo examen)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? idTipoExamen)
        {
            IQueryable<ParametroTipoExamen> query = _db.ParametrosTipoExamen
                .Include(p => p.TipoExamen)
                .WhereSucursal(_sucCtx); // solo los de la sucursal activa

            if (idTipoExamen.HasValue)
                query = query.Where(p => p.IdTipoExamen == idTipoExamen.Value);

            var list = await query
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .Select(p => new
                {
                    Id = p.Id,
                    IdTipoExamen = p.IdTipoExamen,
                    Nombre = p.Nombre,
                    EsTitulo = p.EsTitulo,
                    Orden = p.Orden,
                    Unidad = p.Unidad,
                    RangoReferencia = p.RangoReferencia,
                    Observaciones = p.Observaciones
                })
                .ToListAsync();

            return Ok(list);
        }

        // ==========================================================
        //  Obtener parámetros por tipo de examen (ruta directa)
        // ==========================================================
        [HttpGet("by-tipo/{idTipo:int}")]
        public async Task<IActionResult> GetByTipo(int idTipo)
        {
            var tipo = await _db.TiposExamen
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(t => t.Id == idTipo);

            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado en esta sucursal." });

            var list = await _db.ParametrosTipoExamen
                .Where(p => p.IdTipoExamen == idTipo)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .Select(p => new
                {
                    Id = p.Id,
                    IdTipoExamen = p.IdTipoExamen,
                    Nombre = p.Nombre,
                    EsTitulo = p.EsTitulo,
                    Orden = p.Orden,
                    Unidad = p.Unidad,
                    RangoReferencia = p.RangoReferencia,
                    Observaciones = p.Observaciones
                })
                .ToListAsync();

            return Ok(list);
        }

        // ==========================================================
        //  Crear nuevo parámetro (asigna sucursal automáticamente)
        // ==========================================================
        [HttpPost]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Create([FromBody] ParametroTipoExamen model)
        {
            if (model == null)
                return BadRequest(new { message = "❌ Datos inválidos." });

            // Validar que el tipo de examen pertenezca a la sucursal
            var tipo = await _db.TiposExamen
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(t => t.Id == model.IdTipoExamen);

            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado en esta sucursal." });

            _db.StampSucursal(_sucCtx); //  asignar sucursal al nuevo registro
            _db.ParametrosTipoExamen.Add(model);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✅ Parámetro creado correctamente.", model });
        }

        // ==========================================================
        //  Actualizar parámetro existente
        // ==========================================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Update(int id, [FromBody] ParametroTipoExamen model)
        {
            if (model == null)
                return BadRequest(new { message = "❌ Datos inválidos." });

            var existente = await _db.ParametrosTipoExamen
                .Include(p => p.TipoExamen)
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existente == null)
                return NotFound(new { message = "❌ Parámetro no encontrado o pertenece a otra sucursal." });

            existente.Nombre = model.Nombre;
            existente.Unidad = model.Unidad;
            existente.RangoReferencia = model.RangoReferencia;
            existente.Observaciones = model.Observaciones;
            existente.EsTitulo = model.EsTitulo;
            existente.Orden = model.Orden;

            await _db.SaveChangesAsync();
            return Ok(new { message = "✏️ Parámetro actualizado correctamente.", existente });
        }

        // ==========================================================
        //  Eliminar parámetro (solo de la sucursal activa)
        // ==========================================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> Delete(int id)
        {
            var existente = await _db.ParametrosTipoExamen
                .Include(p => p.TipoExamen)
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existente == null)
                return NotFound(new { message = "❌ Parámetro no encontrado o pertenece a otra sucursal." });

            _db.ParametrosTipoExamen.Remove(existente);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Parámetro eliminado correctamente." });
        }
    }
}
