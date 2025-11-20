using LabClinic.Api.Common;
using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PreciosClinicaController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly ISucursalContext _sucCtx;

        public PreciosClinicaController(LabDbContext db, ISucursalContext sucCtx)
        {
            _db = db;
            _sucCtx = sucCtx;
        }

        //  Listar todos los precios especiales (filtrados por sucursal)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var precios = await _db.PreciosClinica
                .Include(p => p.Clinica)
                .Include(p => p.TipoExamen)
                .WhereSucursal(_sucCtx)
                .OrderBy(p => p.Clinica!.Nombre)
                .Select(p => new
                {
                    id = p.Id,
                    clinica = p.Clinica!.Nombre,
                    tipoExamen = p.TipoExamen!.Nombre,
                    precioEspecial = p.PrecioEspecial,
                    vigenteDesde = p.VigenteDesde,
                    vigenteHasta = p.VigenteHasta
                })
                .ToListAsync();

            return Ok(precios);
        }

        //  Obtener precios de un tipo de examen (por sucursal)
        [HttpGet("porClinica/{idTipoExamen}")]
        public async Task<IActionResult> GetByTipoExamen(int idTipoExamen)
        {
            var precios = await _db.PreciosClinica
                .Include(p => p.Clinica)
                .Where(p => p.IdTipoExamen == idTipoExamen)
                .WhereSucursal(_sucCtx)
                .Select(p => new
                {
                    id = p.Id,
                    clinica = p.Clinica != null ? p.Clinica.Nombre : "(Sin clínica)",
                    precioEspecial = p.PrecioEspecial,
                    vigenteDesde = p.VigenteDesde,
                    vigenteHasta = p.VigenteHasta
                })
                .ToListAsync();

            return Ok(precios);
        }

        //  Crear o actualizar un precio especial
        [HttpPost]
        public async Task<IActionResult> CrearOActualizar([FromBody] PrecioClinica model)
        {
            if (model.IdClinica == 0 || model.IdTipoExamen == 0)
                return BadRequest(new { message = "Debe especificar clínica y tipo de examen." });

            //  Filtrar dentro de la misma sucursal
            var existente = await _db.PreciosClinica
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(p => p.IdClinica == model.IdClinica && p.IdTipoExamen == model.IdTipoExamen);

            if (existente != null)
            {
                existente.PrecioEspecial = model.PrecioEspecial;
                existente.VigenteDesde = model.VigenteDesde;
                existente.VigenteHasta = model.VigenteHasta;
            }
            else
            {
                _db.PreciosClinica.Add(model);
                _db.StampSucursal(_sucCtx); // asignar sucursal automáticamente
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "💰 Precio guardado correctamente." });
        }

        // Eliminar un registro de precio (filtrado por sucursal)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var precio = await _db.PreciosClinica
                .WhereSucursal(_sucCtx)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (precio == null)
                return NotFound(new { message = "Registro no encontrado o no pertenece a esta sucursal." });

            _db.PreciosClinica.Remove(precio);
            await _db.SaveChangesAsync();

            return Ok(new { message = "🗑️ Registro eliminado correctamente." });
        }
    }
}
