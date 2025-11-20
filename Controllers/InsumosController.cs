using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabClinic.Api.Data;
using LabClinic.Api.Common;
using Microsoft.AspNetCore.Authorization;

namespace LabClinic.Api.Controllers;

[ApiController]
[Route("api/insumolaboratorio")]
[Authorize]
public class InsumolaboratorioController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx;

    public InsumolaboratorioController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx;
    }

    //  LISTAR INSUMOS filtrados por sucursal
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var insumos = await _db.Insumos
            .WhereSucursal(_sucCtx)
            .OrderBy(i => i.Nombre)
            .Select(i => new
            {
                i.Id,
                i.Nombre,
                i.Stock,
                i.StockMinimo,
                i.UnidadMedida,
                i.Estado,
                i.Almacenado,
                i.Precio,
                i.Descripcion,
                IdCategoria = i.IdCategoria,
                Categoria = _db.CategoryInsumos
                    .Where(c => c.Id == i.IdCategoria)
                    .Select(c => c.Nombre)
                    .FirstOrDefault(),
                IdProveedor = i.IdProveedor,
                Proveedor = _db.Suppliers
                    .Where(p => p.Id == i.IdProveedor)
                    .Select(p => p.Nombre)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(insumos);
    }

    //  OBTENER UNO
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var insumo = await _db.Insumos
            .WhereSucursal(_sucCtx)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (insumo == null)
            return NotFound(new { message = "Insumo no encontrado en esta sucursal." });

        return Ok(insumo);
    }

    //  CREAR
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Insumo model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _db.StampSucursal(_sucCtx);
        _db.Insumos.Add(model);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Insumo registrado correctamente en la sucursal actual." });
    }

    //  ACTUALIZAR
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Insumo model)
    {
        var insumo = await _db.Insumos
            .WhereSucursal(_sucCtx)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (insumo == null)
            return NotFound(new { message = "❌ No se puede editar: el insumo no pertenece a esta sucursal." });

        insumo.Nombre = model.Nombre;
        insumo.Stock = model.Stock;
        insumo.StockMinimo = model.StockMinimo;
        insumo.UnidadMedida = model.UnidadMedida;
        insumo.Estado = model.Estado;
        insumo.Almacenado = model.Almacenado;
        insumo.Precio = model.Precio;
        insumo.Descripcion = model.Descripcion;
        insumo.IdCategoria = model.IdCategoria;
        insumo.IdProveedor = model.IdProveedor;

        insumo.IdSucursal = _sucCtx.CurrentSucursalId;

        await _db.SaveChangesAsync();
        return Ok(new { message = "✏️ Insumo actualizado correctamente." });
    }

    //  ELIMINAR
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var insumo = await _db.Insumos
            .WhereSucursal(_sucCtx)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (insumo == null)
            return NotFound(new { message = "❌ Insumo no encontrado en esta sucursal." });

        _db.Insumos.Remove(insumo);
        await _db.SaveChangesAsync();

        return Ok(new { message = "🗑️ Insumo eliminado correctamente." });
    }
    // ============================================================
    // 🔹 ENDPOINT ESPECIAL PARA CATÁLOGO DE INSUMOS DISPONIBLES
    // ============================================================
    [HttpGet("disponibles")]
    public async Task<IActionResult> GetDisponibles()
    {
        var insumos = await _db.Insumos
            .WhereSucursal(_sucCtx)
            .OrderBy(i => i.Nombre)
            .Select(i => new
            {
                id_insumo = i.Id,
                nombre_insumo = i.Nombre,
                stock = i.Stock,
                unidad_medida = i.UnidadMedida
            })
            .ToListAsync();

        return Ok(insumos);
    }

}
