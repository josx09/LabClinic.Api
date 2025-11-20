using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/tipos-examen")]
    [Route("api/tiposexamen")]
    [Authorize]
    public class TiposExamenController : ControllerBase
    {
        private readonly LabDbContext _db;

        public TiposExamenController(LabDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        //  LISTAR TODOS LOS TIPOS DE EXAMEN (GLOBAL)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PagedQuery q, [FromQuery] string? search = null)
        {
            if (q.Page <= 0) q.Page = 1;
            if (q.PageSize <= 0) q.PageSize = 500;

            var query = _db.TiposExamen
                .Include(t => t.Categoria)
                .Include(t => t.Perfil)
                .AsQueryable();

            //  Filtro de búsqueda
            string term = search ?? q.Search ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                query = query.Where(x =>
                    x.Nombre.ToLower().Contains(term) ||
                    (x.Descripcion != null && x.Descripcion.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Nombre)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(x => new
                {
                    id = x.Id,
                    nombre = x.Nombre,
                    descripcion = x.Descripcion,
                    precio = x.Precio,
                    categoria = x.Categoria != null ? x.Categoria.Nombre : "(Sin categoría)",
                    perfil = x.Perfil != null ? x.Perfil.Nombre : "(Sin perfil)"
                })
                .ToListAsync();

            return Ok(new { total, items });
        }

        // ==========================================================
        //  OBTENER TIPO DE EXAMEN POR ID
        // ==========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var tipo = await _db.TiposExamen
                .Include(t => t.Categoria)
                .Include(t => t.Perfil)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado." });

            return Ok(new
            {
                id = tipo.Id,
                nombre = tipo.Nombre,
                descripcion = tipo.Descripcion,
                precio = tipo.Precio,
                id_categoria_tipo_examen = tipo.IdCategoriaTipoExamen,
                id_perfil_examen = tipo.IdPerfilExamen,
                categoria = tipo.Categoria != null ? tipo.Categoria.Nombre : "(Sin categoría)",
                perfil = tipo.Perfil != null ? tipo.Perfil.Nombre : "(Sin perfil)"
            });
        }

        // ==========================================================
        //  CREAR NUEVO TIPO DE EXAMEN (GLOBAL)
        // ==========================================================
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([FromBody] TipoExamen model)
        {
            if (model == null)
                return BadRequest("Datos inválidos.");

            if (string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest("El nombre es obligatorio.");

            if (model.IdCategoriaTipoExamen <= 0)
                model.IdCategoriaTipoExamen = null;
            if (model.IdPerfilExamen <= 0)
                model.IdPerfilExamen = null;

            _db.TiposExamen.Add(model);
            await _db.SaveChangesAsync();


            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // ==========================================================
        //  ACTUALIZAR TIPO DE EXAMEN
        // ==========================================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(int id, [FromBody] JsonElement body)
        {
            try
            {
                var model = JsonSerializer.Deserialize<TipoExamen>(
                    body.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (model == null)
                    return BadRequest(new { message = "Modelo nulo." });

                if (id != model.Id)
                    return BadRequest(new { message = "El ID del cuerpo no coincide con la URL." });

                var existing = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == id);
                if (existing == null)
                    return NotFound(new { message = "❌ No encontrado." });

                existing.Nombre = model.Nombre;
                existing.Descripcion = model.Descripcion;
                existing.Precio = model.Precio;
                existing.IdCategoriaTipoExamen = model.IdCategoriaTipoExamen > 0 ? model.IdCategoriaTipoExamen : null;
                existing.IdPerfilExamen = model.IdPerfilExamen > 0 ? model.IdPerfilExamen : null;

                await _db.SaveChangesAsync();
                return Ok(new { message = "✅ Registro actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Error interno al actualizar.", detalle = ex.Message });
            }
        }

        // ==========================================================
        //  CREAR / EDITAR PARÁMETROS DE UN TIPO DE EXAMEN
        // ==========================================================
        [HttpPost("{idTipo}/parametros")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> SaveParametro(int idTipo, [FromBody] ParametroTipoExamen model)
        {
            if (model == null)
                return BadRequest(new { message = "❌ Datos inválidos." });

            var tipo = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == idTipo);
            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado." });

            if (model.Id == 0)
            {
                model.IdTipoExamen = idTipo;
                _db.ParametrosTipoExamen.Add(model);
            }
            else
            {
                var existente = await _db.ParametrosTipoExamen.FindAsync(model.Id);
                if (existente == null)
                    return NotFound(new { message = "❌ Parámetro no encontrado." });

                existente.Nombre = model.Nombre;
                existente.Unidad = model.Unidad;
                existente.RangoReferencia = model.RangoReferencia;
                existente.Observaciones = model.Observaciones;
                existente.EsTitulo = model.EsTitulo;
                existente.Orden = model.Orden;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Parámetro guardado correctamente." });
        }

        // ==========================================================
        //  LISTAR PARÁMETROS DE UN TIPO DE EXAMEN
        // ==========================================================
        [HttpGet("{idTipo}/parametros")]
        public async Task<IActionResult> GetParametrosByTipo(int idTipo)
        {
            var tipo = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == idTipo);
            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado." });

            var parametros = await _db.ParametrosTipoExamen
                .Where(p => p.IdTipoExamen == idTipo)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return Ok(parametros);
        }

        // ==========================================================
        //  ELIMINAR TIPO DE EXAMEN
        // ==========================================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var tipo = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == id);
            if (tipo == null)
                return NotFound(new { message = "❌ No encontrado." });

            bool tieneRelacion = await _db.Examenes.AnyAsync(e => e.IdTipoExamen == id);
            if (tieneRelacion)
                return Conflict(new { message = "No se puede eliminar el tipo de examen porque tiene exámenes asociados." });

            _db.TiposExamen.Remove(tipo);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // ==========================================================
        //  OBTENER PRECIOS POR CLÍNICA (solo informativo)
        // ==========================================================
        [HttpGet("{id}/precios-clinica")]
        public async Task<IActionResult> GetPreciosPorClinica(int id)
        {
            var precios = await _db.PreciosClinica
                .Include(p => p.Clinica)
                .Where(p => p.IdTipoExamen == id)
                .Select(p => new
                {
                    id = p.Id,
                    idClinica = p.IdClinica,
                    nombreClinica = p.Clinica != null ? p.Clinica.Nombre : "(Sin clínica)",
                    precio = p.PrecioEspecial,
                    desde = p.VigenteDesde,
                    hasta = p.VigenteHasta
                })
                .OrderBy(p => p.nombreClinica)
                .ToListAsync();

            return Ok(precios);
        }

        // ==========================================================
        // ACTUALIZAR / ELIMINAR PARÁMETRO
        // ==========================================================
        [HttpPut("{idTipo}/parametros/{id}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> UpdateParametro(int idTipo, int id, [FromBody] ParametroTipoExamen body)
        {
            var tipo = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == idTipo);
            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado." });

            var p = await _db.ParametrosTipoExamen
                .FirstOrDefaultAsync(x => x.Id == id && x.IdTipoExamen == idTipo);

            if (p == null)
                return NotFound(new { message = "❌ Parámetro no encontrado." });

            p.Nombre = body.Nombre;
            p.Unidad = body.Unidad;
            p.RangoReferencia = body.RangoReferencia;
            p.Observaciones = body.Observaciones;
            p.EsTitulo = body.EsTitulo;
            p.Orden = body.Orden;

            await _db.SaveChangesAsync();
            return Ok(new { message = "✅ Parámetro actualizado." });
        }

        [HttpDelete("{idTipo}/parametros/{id}")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> DeleteParametro(int idTipo, int id)
        {
            var tipo = await _db.TiposExamen.FirstOrDefaultAsync(t => t.Id == idTipo);
            if (tipo == null)
                return NotFound(new { message = "❌ Tipo de examen no encontrado." });

            var p = await _db.ParametrosTipoExamen
                .FirstOrDefaultAsync(x => x.Id == id && x.IdTipoExamen == idTipo);

            if (p == null)
                return NotFound(new { message = "❌ Parámetro no encontrado." });

            _db.ParametrosTipoExamen.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{idTipoExamen}/insumos")]
        public async Task<IActionResult> AsignarInsumos(int idTipoExamen, [FromBody] List<TipoExamenInsumo> insumos)
        {
            _db.TipoExamenInsumos.RemoveRange(_db.TipoExamenInsumos.Where(t => t.IdTipoExamen == idTipoExamen));
            await _db.SaveChangesAsync();

            foreach (var i in insumos)
            {
                i.IdTipoExamen = idTipoExamen;
            }

            _db.TipoExamenInsumos.AddRange(insumos);
            await _db.SaveChangesAsync();

            return Ok(new { message = "✅ Insumos asignados correctamente al tipo de examen." });
        }

        // ==========================================================
        // 🔹 Listar insumos asociados a un tipo de examen
        // ==========================================================
        [HttpGet("{idTipoExamen}/insumos")]
        public async Task<IActionResult> GetInsumosPorTipo(int idTipoExamen)
        {
            var insumos = await _db.TipoExamenInsumos
                .Include(ti => ti.Insumo)
                    .ThenInclude(i => i.Categoria)
                .Where(ti => ti.IdTipoExamen == idTipoExamen)
                .Select(ti => new
                {
                    id = ti.Id,
                    id_insumo = ti.IdInsumo,
                    nombre = ti.Insumo != null ? ti.Insumo.Nombre : "(Desconocido)",
                    cantidad_usada = ti.CantidadUsada,
                    stock = ti.Insumo != null ? ti.Insumo.Stock : 0,
                    unidad_medida = ti.Insumo != null
                        ? (!string.IsNullOrEmpty(ti.Insumo.UnidadMedida)
                            ? ti.Insumo.UnidadMedida
                            : (ti.Insumo.Categoria != null ? ti.Insumo.Categoria.Unidad : ""))
                        : ""
                })
                .OrderBy(ti => ti.nombre)
                .ToListAsync();

            return Ok(insumos);
        }


        // ==========================================================
        // 🔹 Eliminar un insumo específico del tipo de examen
        // ==========================================================
        [HttpDelete("{idTipoExamen}/insumos/{idInsumo}")]
        public async Task<IActionResult> DeleteInsumo(int idTipoExamen, int idInsumo)
        {
            var rel = await _db.TipoExamenInsumos
                .FirstOrDefaultAsync(x => x.IdTipoExamen == idTipoExamen && x.IdInsumo == idInsumo);

            if (rel == null)
                return NotFound(new { message = "❌ Relación no encontrada." });

            _db.TipoExamenInsumos.Remove(rel);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Insumo eliminado correctamente." });
        }
        // ============================================================
        // 🔹 AGREGAR INSUMO A UN TIPO DE EXAMEN
        // ============================================================
        [HttpPost("{idTipoExamen:int}/insumos/agregar")]
        public async Task<IActionResult> AgregarInsumo(int idTipoExamen, [FromBody] JsonElement body)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Undefined || body.ValueKind == JsonValueKind.Null)
                    return BadRequest(new { message = "Datos no enviados." });

                // 🔹 Extraer valores seguros
                int idInsumo = 0;
                decimal cantidad = 0;

                if (body.TryGetProperty("id_insumo", out var insumoProp) && insumoProp.ValueKind == JsonValueKind.Number)
                    idInsumo = insumoProp.GetInt32();

                if (body.TryGetProperty("cantidad_usada", out var cantProp) &&
                    (cantProp.ValueKind == JsonValueKind.Number || cantProp.ValueKind == JsonValueKind.String))
                {
                    decimal.TryParse(cantProp.ToString(), out cantidad);
                }

                if (idInsumo <= 0 || cantidad <= 0)
                    return BadRequest(new { message = "Datos inválidos para agregar insumo." });

                // 🔹 Validar tipo de examen
                var tipoExamen = await _db.TiposExamen.FindAsync(idTipoExamen);
                if (tipoExamen == null)
                    return NotFound(new { message = "Tipo de examen no encontrado." });

                // 🔹 Validar insumo
                var insumo = await _db.Insumos.FindAsync(idInsumo);
                if (insumo == null)
                    return NotFound(new { message = "Insumo no encontrado." });

                // 🔹 Verificar duplicado
                var existe = await _db.TipoExamenInsumos
                    .AnyAsync(ti => ti.IdTipoExamen == idTipoExamen && ti.IdInsumo == idInsumo);
                if (existe)
                    return BadRequest(new { message = "Este insumo ya está asociado a este examen." });

                // 🔹 Crear relación
                var relacion = new TipoExamenInsumo
                {
                    IdTipoExamen = idTipoExamen,
                    IdInsumo = idInsumo,
                    CantidadUsada = cantidad
                };

                _db.TipoExamenInsumos.Add(relacion);
                await _db.SaveChangesAsync();

                return Ok(new { message = "✅ Insumo asociado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al asociar insumo.", detail = ex.Message });
            }
        }


    }
}
