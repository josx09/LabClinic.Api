using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/categorias-tipoexamen")]
    [Route("api/categoriastipoexamen")]
    [Authorize]
    public class CategoriasTipoExamenController : ControllerBase
    {
        private readonly LabDbContext _db;

        public CategoriasTipoExamenController(LabDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        //  OBTENER TODAS LAS CATEGORÍAS (GLOBAL)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categorias = await _db.CategoriasTipoExamen
                .Include(c => c.TiposExamen)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Descripcion,
                    TotalExamenes = c.TiposExamen != null ? c.TiposExamen.Count : 0
                })
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return Ok(categorias);
        }

        // ==========================================================
        //  OBTENER CATEGORÍA POR ID
        // ==========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var categoria = await _db.CategoriasTipoExamen
                .Include(c => c.TiposExamen)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return NotFound(new { message = "❌ Categoría no encontrada." });

            return Ok(categoria);
        }

        // ==========================================================
        //  CREAR NUEVA CATEGORÍA
        // ==========================================================
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] CategoriaTipoExamen model)
        {
            if (model == null)
                return BadRequest(new { message = "❌ Datos inválidos." });

            if (string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest(new { message = "El nombre de la categoría es obligatorio." });

            bool existe = await _db.CategoriasTipoExamen
                .AnyAsync(c => c.Nombre.ToLower() == model.Nombre.ToLower());

            if (existe)
                return Conflict(new { message = "⚠️ Ya existe una categoría con ese nombre." });

            _db.CategoriasTipoExamen.Add(model);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✅ Categoría creada correctamente.", model });
        }

        // ==========================================================
        //  ACTUALIZAR CATEGORÍA EXISTENTE
        // ==========================================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaTipoExamen model)
        {
            if (model == null)
                return BadRequest(new { message = "❌ Datos inválidos." });

            var categoria = await _db.CategoriasTipoExamen.FindAsync(id);
            if (categoria == null)
                return NotFound(new { message = "❌ Categoría no encontrada." });

            if (string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest(new { message = "El nombre de la categoría es obligatorio." });

            categoria.Nombre = model.Nombre;
            categoria.Descripcion = model.Descripcion;

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Categoría actualizada correctamente.", categoria });
        }

        // ==========================================================
        //  ELIMINAR CATEGORÍA
        // ==========================================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _db.CategoriasTipoExamen
                .Include(c => c.TiposExamen)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return NotFound(new { message = "❌ Categoría no encontrada." });

            if (categoria.TiposExamen != null && categoria.TiposExamen.Any())
                return Conflict(new { message = "⚠️ No se puede eliminar la categoría porque tiene tipos de examen asociados." });

            _db.CategoriasTipoExamen.Remove(categoria);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Categoría eliminada correctamente." });
        }
    }
}
