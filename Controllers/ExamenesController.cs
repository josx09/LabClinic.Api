using LabClinic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LabClinic.Api.Common;
using LabClinic.Api.Services;
using System.Text;
using LabClinic.Api.Models.Dtos;


namespace LabClinic.Api.Controllers;

public class ExamenCreateDto
{
    public int id_persona { get; set; }
   
    public int id_tipo_examen { get; set; }
    public int? id_perfil_examen { get; set; } 
    public int? id_clinica { get; set; }
    public int usar_precio_clinica { get; set; } 
    public string? resultado { get; set; }
    public bool? agrupar { get; set; }
    public string? grupo_examen { get; set; }
    public int? id_referidor { get; set; }


}


[ApiController]
[Route("api/examenes")]
[Authorize]
public class ExamenesController : ControllerBase
{
    private readonly LabDbContext _db;
    private readonly ISucursalContext _sucCtx; 

    public ExamenesController(LabDbContext db, ISucursalContext sucCtx)
    {
        _db = db;
        _sucCtx = sucCtx; 
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var userType = User.FindFirst("type")?.Value;
        int.TryParse(User.FindFirst("uid")?.Value, out int uid);
        int.TryParse(User.FindFirst("pid")?.Value, out int pid);

        if (pid == 0 && uid > 0)
        {
            pid = (await _db.Users
                .Where(u => u.Id == uid)
                .Select(u => u.IdPersona)
                .FirstOrDefaultAsync()) ?? 0;
        }

        var query = _db.Examenes
            .Include(e => e.TipoExamen)
            .Include(e => e.Persona)
            .Where(e => e.GrupoExamen != null)
            .WhereSucursal(_sucCtx); 


        if (desde.HasValue && hasta.HasValue)
        {
            var d1 = desde.Value.Date;
            var d2 = hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.FechaRegistro >= d1 && e.FechaRegistro <= d2);
        }

        if (userType == "3" && pid > 0)
        {
            query = query.Where(e => e.IdReferidor == pid);
        }

        var examenes = await query
            .OrderByDescending(e => e.FechaRegistro)
            .Select(e => new
            {
                e.Id,
                Paciente = e.Persona != null ? e.Persona.Nombre + " " + e.Persona.Apellido : "(Sin paciente)",
                TipoExamen = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                ReferidoPor = e.Referidor != null ? e.Referidor.Firstname + " " + e.Referidor.Lastname : "(Sin referidor)",
                Resultado = e.Resultado ?? "pendiente",
                e.Estado,
                e.FechaRegistro,
                Precio = e.PrecioAplicado
            })
            .ToListAsync();

        return Ok(examenes);
    }


// GET: api/examenes/paciente/{idPersona}
    [HttpGet("paciente/{idPersona}")]
    public async Task<IActionResult> GetByPersona(int idPersona)
    {
        // ==========================================================
        //  Leer info del usuario actual (desde el JWT)
        // ==========================================================
        var userIdStr = User.FindFirst("uid")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role))
            return Unauthorized(new { message = "Token inválido o sin rol." });

        int.TryParse(userIdStr, out int userId);

        // ==========================================================
        //  CLIENTE: solo puede ver sus propios exámenes
        // ==========================================================
        if (role == "Cliente")
        {
            // Busca si este usuario está vinculado a esta persona (paciente)
            var personaVinculada = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IdPersona)
                .FirstOrDefaultAsync();

            if (personaVinculada == null)
                return Forbid("No tienes un registro de paciente vinculado.");

            if (personaVinculada != idPersona)
                return Forbid("Solo puedes ver tus propios resultados.");
        }

        // ==========================================================
        //  MÉDICO: solo puede ver los exámenes donde él es referidor
        // ==========================================================
        if (role == "Médico")
        {
            bool tieneExamenesReferidos = await _db.Examenes
                .AnyAsync(e => e.IdPersona == idPersona && e.IdReferidor == userId);

            if (!tieneExamenesReferidos)
                return Forbid("Solo puedes ver los exámenes de pacientes referidos por ti.");
        }


        // ==========================================================
        // Si pasa las validaciones, devolver exámenes
        // ==========================================================
        var examenes = await _db.Examenes
            .Include(e => e.TipoExamen)
            .Where(e => e.IdPersona == idPersona)
            .WhereSucursal(_sucCtx) 
            .Select(e => new
            {
                id = e.Id,
                id_pago = e.IdPago,
                resultado = e.Resultado,
                estado = e.Estado,
                tipoExamen = e.TipoExamen != null
                    ? new { id = e.TipoExamen.Id, nombre = e.TipoExamen.Nombre }
                    : null,
                parametrosCount = _db.Set<ParametroExamen>().Count(px => px.IdExamen == e.Id),
                precio = e.PrecioAplicado,
                grupo_examen = e.GrupoExamen,
                fechaRegistro = e.FechaRegistro
            })
            .ToListAsync();


        var shaped = examenes.Select(e => new
        {
            e.id,
            e.id_pago,
            e.resultado,
            e.estado,
            e.tipoExamen,
            e.precio,
            e.fechaRegistro,
            e.grupo_examen,
            hasParametros = e.parametrosCount > 0
        });

        return Ok(shaped);
    }




    // =====================================================
    // CREAR / ACTUALIZAR / ELIMINAR EXAMEN
    // =====================================================

    // POST: api/examenes
    [HttpPost]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> Create([FromBody] ExamenCreateDto body)
    {
        if (body == null)
            return BadRequest("Datos inválidos.");

        if (!_db.Persons.Any(p => p.Id == body.id_persona))
            return BadRequest("El paciente no existe.");

        //  Si viene un perfil, creamos TODOS los exámenes del perfil para el paciente
        if (body.id_perfil_examen.HasValue && body.id_perfil_examen.Value > 0)
        {
            return await CrearDesdePerfil(body);
        }

        //  Caso normal: examen individual
        if (body.id_tipo_examen <= 0)
            return BadRequest("Debe indicar un tipo de examen (> 0) o un perfil de examen.");

        // =========================================
        //  Lógica mejorada para control de grupo
        // ==========================================
        string? grupo = null;

       
        if (!string.IsNullOrEmpty(body.grupo_examen))
        {
            grupo = body.grupo_examen;
        }
        
        else if (body.agrupar == true)
        {
            
            var grupoReciente = await _db.Examenes
                .Where(e => e.IdPersona == body.id_persona)
                .OrderByDescending(e => e.FechaRegistro)
                .Select(e => new { e.GrupoExamen, e.FechaRegistro })
                .FirstOrDefaultAsync();

            if (grupoReciente != null &&
                !string.IsNullOrEmpty(grupoReciente.GrupoExamen) &&
                (DateTime.Now - grupoReciente.FechaRegistro).TotalMinutes < 15)
            {
               
                grupo = grupoReciente.GrupoExamen;
            }
            else
            {
                
                grupo = Guid.NewGuid().ToString();
            }
        }
       

        // ====== LÓGICA ORIGINAL TUYA (individual) ======
        var req = new Examen
        {
            IdPersona = body.id_persona,
            IdTipoExamen = body.id_tipo_examen,
            IdClinica = body.id_clinica,
            UsarPrecioClinica = body.usar_precio_clinica == 1,
            Resultado = body.resultado,
            GrupoExamen = grupo,
            IdReferidor = body.id_referidor
        };

        if (!_db.TiposExamen.Any(t => t.Id == req.IdTipoExamen))
            return BadRequest("El tipo de examen no existe.");

        decimal precioAplicado = 0;

        if (req.UsarPrecioClinica && req.IdClinica.HasValue)
        {
            var precioClinica = await _db.PreciosClinica
                .Where(p => p.IdClinica == req.IdClinica && p.IdTipoExamen == req.IdTipoExamen)
                .OrderByDescending(p => p.VigenteDesde)
                .Select(p => p.PrecioEspecial)
                .FirstOrDefaultAsync();

            if (precioClinica > 0)
                precioAplicado = precioClinica;
        }

        if (precioAplicado == 0)
        {
            var precioTipo = await _db.TiposExamen
                .Where(t => t.Id == req.IdTipoExamen)
                .Select(t => t.Precio)
                .FirstOrDefaultAsync();

            precioAplicado = precioTipo ?? 0;
        }

        req.PrecioAplicado = precioAplicado;
        req.FechaRegistro = DateTime.Now;
        req.Estado = req.Estado == 0 ? 1 : req.Estado;

        //  Asignar automáticamente la sucursal actual
        _db.StampSucursal(_sucCtx);

        _db.Examenes.Add(req);
        await _db.SaveChangesAsync();



        // ==========================================================
        // 🔹 Descontar insumos automáticamente y registrar historial
        // ==========================================================
        try
        {
            var insumosAsociados = await _db.TipoExamenInsumos
                .Where(ti => ti.IdTipoExamen == req.IdTipoExamen)
                .Include(ti => ti.Insumo)
                .ToListAsync();

            foreach (var ti in insumosAsociados)
            {
                if (ti.Insumo != null)
                {
                    // Descontar del stock
                    ti.Insumo.Stock -= ti.CantidadUsada;
                    if (ti.Insumo.Stock < 0)
                        ti.Insumo.Stock = 0;

                    _db.Insumos.Update(ti.Insumo);

                    // ✅ Registrar el uso en la tabla de historial
                    var registro = new RegistroInsumoUso
                    {
                        IdInsumo = ti.IdInsumo,
                        IdExamen = req.Id,
                        CantidadUsada = ti.CantidadUsada,
                        Fecha = DateTime.Now,
                        IdSucursal = _sucCtx.SucursalId ?? _sucCtx.CurrentSucursalId, // usa el ID actual si es null
                        Justificacion = "Uso automático por examen"
                    };

                    _db.RegistroInsumoUsos.Add(registro);

                }
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error al actualizar stock/registrar uso: {ex.Message}");
        }



        //  Crear automáticamente los parámetros desde la plantilla base
        var plantilla = await _db.ParametrosTipoExamen
            .Where(p => p.IdTipoExamen == req.IdTipoExamen)
            .AsNoTracking()
            .ToListAsync();

        if (plantilla.Any())
        {
            var nuevos = plantilla.Select(p => new ParametroExamen
            {
                IdExamen = req.Id,
                Nombre = p.Nombre,
                Unidad = p.Unidad,
                RangoReferencia = p.RangoReferencia,
                Resultado = string.Empty,
                Observaciones = p.Observaciones ?? string.Empty
            }).ToList();

            _db.ParametrosExamen.AddRange(nuevos);
            await _db.SaveChangesAsync();
        }

        var tipoExamen = await _db.TiposExamen
            .Where(t => t.Id == req.IdTipoExamen)
            .Select(t => new { id = t.Id, nombre = t.Nombre })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            id = req.Id,
            id_persona = req.IdPersona,
            id_tipo_examen = req.IdTipoExamen,
            tipoExamen,
            resultado = req.Resultado,
            estado = req.Estado,
            precio = req.PrecioAplicado,
            fechaRegistro = req.FechaRegistro,
            hasParametros = plantilla.Any(),
            message = plantilla.Any()
                ? "✅ Examen registrado correctamente con parámetros generados."
                : "✅ Examen registrado (sin plantilla asociada)."
        });
    }

    // =====================================================
    // 🔹 Crear TODOS los exámenes de un perfil para un paciente
    // =====================================================
    private async Task<IActionResult> CrearDesdePerfil(ExamenCreateDto body)
    {
        var perfil = await _db.PerfilesExamen
            .Include(p => p.PerfilParametros!)
            .ThenInclude(pp => pp.TipoExamen)
            .FirstOrDefaultAsync(p => p.Id == body.id_perfil_examen);

        if (perfil == null)
            return NotFound(new { message = "❌ Perfil no encontrado." });

        var grupo = body.grupo_examen ?? Guid.NewGuid().ToString();
        var creados = new List<object>();

        //  Asignar la sucursal activa a todas las inserciones que se hagan en este contexto
        _db.StampSucursal(_sucCtx);

        foreach (var pp in perfil.PerfilParametros!)
        {
            var tipo = pp.TipoExamen!;
            decimal precioAplicado = 0;

            //  Buscar precio especial por clínica (si aplica)
            if (body.usar_precio_clinica == 1 && body.id_clinica.HasValue)
            {
                var precioClinica = await _db.PreciosClinica
                    .Where(p => p.IdClinica == body.id_clinica && p.IdTipoExamen == tipo.Id)
                    .OrderByDescending(p => p.VigenteDesde)
                    .Select(p => p.PrecioEspecial)
                    .FirstOrDefaultAsync();

                if (precioClinica > 0)
                    precioAplicado = precioClinica;
            }

            //  Si no hay precio especial, usar el del tipo de examen
            if (precioAplicado == 0)
            {
                var precioTipo = await _db.TiposExamen
                    .Where(t => t.Id == tipo.Id)
                    .Select(t => t.Precio)
                    .FirstOrDefaultAsync();

                precioAplicado = precioTipo ?? 0;
            }

            //  Crear examen individual dentro del perfil
            var examen = new Examen
            {
                IdPersona = body.id_persona,
                IdTipoExamen = tipo.Id,
                IdClinica = body.id_clinica,
                UsarPrecioClinica = body.usar_precio_clinica == 1,
                Resultado = body.resultado,
                PrecioAplicado = precioAplicado,
                FechaRegistro = DateTime.Now,
                Estado = 1,
                GrupoExamen = grupo
            };

            _db.Examenes.Add(examen);
            await _db.SaveChangesAsync();

            //  Copiar parámetros desde la plantilla base
            await CrearParametrosDesdePlantilla(examen.Id, tipo.Id);

            creados.Add(new
            {
                id_examen = examen.Id,
                tipo_examen = tipo.Nombre,
                precio = examen.PrecioAplicado
            });
        }

        //  Responder con resumen + precios del perfil
        return Ok(new
        {
            tipo = "perfil",
            message = $"✅ Perfil '{perfil.Nombre}' asignado correctamente a la persona.",
            perfil = new { perfil.Id, perfil.Nombre, perfil.PrecioTotal, perfil.PrecioPaquete },
            examenes_creados = creados
        });
    }



    // =====================================================
    //  Clonar parámetros desde la plantilla base
    // =====================================================
    private async Task CrearParametrosDesdePlantilla(int idExamen, int idTipoExamen)
    {
        var plantilla = await _db.ParametrosTipoExamen
            .Where(p => p.IdTipoExamen == idTipoExamen)
            .AsNoTracking()
            .ToListAsync();

        if (!plantilla.Any()) return;

        var nuevos = plantilla.Select(p => new ParametroExamen
        {
            IdExamen = idExamen,
            Nombre = p.Nombre,
            Unidad = p.Unidad,
            RangoReferencia = p.RangoReferencia,
            Resultado = string.Empty,
            Observaciones = p.Observaciones ?? string.Empty
        }).ToList();

        _db.ParametrosExamen.AddRange(nuevos);
        await _db.SaveChangesAsync();
    }


    // PUT: api/examenes/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> Update(int id, [FromBody] Examen req)
    {
        var examen = await _db.Examenes.FindAsync(id);
        if (examen == null) return NotFound("Examen no encontrado.");

        examen.Resultado = req.Resultado ?? examen.Resultado;
        examen.Estado = req.Estado != 0 ? req.Estado : examen.Estado;
        examen.IdTipoExamen = req.IdTipoExamen != 0 ? req.IdTipoExamen : examen.IdTipoExamen;

        _db.Examenes.Update(examen);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✏️ Examen actualizado correctamente." });
    }

    // DELETE: api/examenes/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> Delete(int id)
    {
        var examen = await _db.Examenes.FindAsync(id);
        if (examen == null) return NotFound("Examen no encontrado.");

        _db.Examenes.Remove(examen);
        await _db.SaveChangesAsync();

        return Ok(new { message = "🗑️ Examen eliminado correctamente." });
    }

    // DELETE: api/examenes/paciente/{idPersona}
    [HttpDelete("paciente/{idPersona}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> DeleteByPaciente(int idPersona)
    {
        var examenes = await _db.Examenes.Where(e => e.IdPersona == idPersona).ToListAsync();
        if (!examenes.Any()) return NotFound("Este paciente no tiene exámenes registrados.");

        _db.Examenes.RemoveRange(examenes);
        await _db.SaveChangesAsync();

        return Ok(new { message = "🧹 Todos los exámenes del paciente fueron eliminados." });
    }

    // =====================================================
    // PARÁMETROS DE EXAMEN
    // =====================================================

    // Crear parámetros desde la plantilla base (manual)
    [HttpPost("from-template/{idExamen}")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> CreateFromTemplate(int idExamen)
    {
        var examen = await _db.Examenes
            .Include(e => e.TipoExamen)
            .FirstOrDefaultAsync(e => e.Id == idExamen);

        if (examen == null)
            return NotFound(new { message = "❌ Examen no encontrado." });

        var plantilla = await _db.ParametrosTipoExamen
            .Where(p => p.IdTipoExamen == examen.IdTipoExamen)
            .AsNoTracking()
            .ToListAsync();

        if (!plantilla.Any())
            return BadRequest(new { message = "⚠️ El tipo de examen no tiene parámetros definidos." });

        bool existen = await _db.ParametrosExamen.AnyAsync(p => p.IdExamen == idExamen);
        if (existen)
            return BadRequest(new { message = "⚠️ Este examen ya tiene parámetros cargados." });

        var nuevos = plantilla.Select(p => new ParametroExamen
        {
            IdExamen = idExamen,
            Nombre = p.Nombre,
            Unidad = p.Unidad,
            RangoReferencia = p.RangoReferencia,
            Resultado = string.Empty,
            Observaciones = p.Observaciones ?? string.Empty
        }).ToList();

        _db.ParametrosExamen.AddRange(nuevos);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "✅ Parámetros del examen creados correctamente desde la plantilla base.",
            count = nuevos.Count
        });
    }

    // Actualizar todos los parámetros del examen
    [HttpPut("{id}/parametros")]
    public async Task<IActionResult> UpdateAllParametros(int id, [FromBody] List<ParametroExamen> parametros)
    {
        var existentes = await _db.ParametrosExamen.Where(p => p.IdExamen == id).ToListAsync();

        foreach (var p in parametros)
        {
            var param = existentes.FirstOrDefault(x => x.Id == p.Id);
            if (param != null)
            {
                param.Resultado = p.Resultado;
                param.Observaciones = p.Observaciones;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "✅ Todos los parámetros actualizados correctamente." });
    }

    // ==========================================================
    // 📅 Obtener insumos usados (hoy o por rango de fechas)
    // ==========================================================
    [HttpGet("insumos/hoy")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> GetInsumosUsadosHoy([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        // Si no se envían fechas, usar "hoy" por defecto
        var inicio = desde?.Date ?? DateTime.Today;
        var fin = (hasta?.Date ?? DateTime.Today).AddDays(1);

        var registros = await _db.RegistroInsumoUsos
            .Include(r => r.Insumo)
            .Include(r => r.Examen)
            .Where(r => r.Fecha >= inicio && r.Fecha < fin)
            .WhereSucursal(_sucCtx)
            .Select(r => new
            {
                r.Id,
                Fecha = r.Fecha,
                Insumo = r.Insumo != null ? r.Insumo.Nombre : "(Eliminado)",
                Cantidad = r.CantidadUsada,
                ExamenId = r.IdExamen,
                TipoExamen = r.Examen != null
                    ? (r.Examen.TipoExamen != null
                        ? r.Examen.TipoExamen.Nombre
                        : "(Sin tipo)")
                    : "(Uso manual)",
                Justificacion = string.IsNullOrWhiteSpace(r.Justificacion)
                    ? "—"
                    : r.Justificacion
            })
            .OrderByDescending(r => r.Fecha)
            .ToListAsync();

        // ✅ Si no hay registros, devolver mensaje amigable
        if (!registros.Any())
            return Ok(new { message = "No hay registros de uso en este rango de fechas.", items = new object[0] });

        return Ok(registros);
    }


    // POST: api/examenes/insumos/manual
    [HttpPost("insumos/manual")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> RegistrarUsoManual([FromBody] RegistroInsumoUso body)
    {
        if (body.IdInsumo <= 0 || body.CantidadUsada <= 0)
            return BadRequest("Datos incompletos.");

        var insumo = await _db.Insumos.FindAsync(body.IdInsumo);
        if (insumo == null)
            return NotFound("Insumo no encontrado.");

        // Descontar stock
        insumo.Stock -= body.CantidadUsada;
        if (insumo.Stock < 0) insumo.Stock = 0;

        // Registrar uso manual
        var registro = new RegistroInsumoUso
        {
            IdInsumo = body.IdInsumo,
            CantidadUsada = body.CantidadUsada,
            Justificacion = body.Justificacion ?? "Uso extra registrado manualmente",
            IdSucursal = _sucCtx.CurrentSucursalId
,
            Fecha = DateTime.Now
        };

        _db.Add(registro);
        await _db.SaveChangesAsync();

        return Ok(new { message = "✅ Uso manual registrado correctamente." });
    }



    // =====================================================
    // REPORTES (PDF)
    // =====================================================


    [HttpGet("reporte-demo")]
    [AllowAnonymous]
    public IActionResult DemoReportePdf()
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Header().Text("Laboratorio Clínico - Demo").SemiBold().FontSize(16);
                page.Content().Text("Si ves este PDF, QuestPDF quedó OK ✅").FontSize(12);
                page.Footer().AlignCenter().Text("Fin del demo");
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", "demo-reporte.pdf");
    }

    // =====================================================
    // Reporte de un examen
    // =====================================================
    [HttpGet("{id}/reporte")]
    [AllowAnonymous]
    public IActionResult GenerarReporteExamen(int id)
    {
        QuestPDF.Settings.EnableDebugging = false;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        QuestPDF.Settings.EnableCaching = true;

        var datos = _db.VistaExamenReporteClinico
            .Where(v => v.IdExamen == id)
            .OrderBy(v => v.IdParametroPlantilla)
            .ToList();

        if (!datos.Any())
            return NotFound(new { message = "No se encontraron datos para este examen." });

        var info = datos.First();

        //  Agrupar por secciones (títulos)
        var grupos = new List<(string Titulo, List<VistaExamenReporteClinico> Items)>();
        string? tituloActual = null;
        foreach (var fila in datos)
        {
            var param = _db.ParametrosTipoExamen
                .FirstOrDefault(x => x.Nombre == (fila.ParametroPlantilla ?? fila.ParametroBase));

            bool esTitulo = param != null && param.EsTitulo == 1;

            if (esTitulo)
            {
                // Nuevo grupo
                tituloActual = (fila.ParametroPlantilla ?? fila.ParametroBase)?.ToUpper();
                grupos.Add((tituloActual!, new List<VistaExamenReporteClinico>()));
            }
            else
            {
                if (grupos.Count == 0)
                {
                    grupos.Add((null, new List<VistaExamenReporteClinico>()));
                }

                grupos.Last().Items.Add(fila);
            }


        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                // Fondo membretado
                var assets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
                var candidatos = new[]
                {
                "hoja_membretada.png", "hoja membretada.png",
                "hoja_membretada.jpg", "hoja membretada.jpg"
            };
                var fondo = candidatos
                    .Select(n => Path.Combine(assets, n))
                    .FirstOrDefault(System.IO.File.Exists);

                if (!string.IsNullOrEmpty(fondo))
                    page.Background().Image(fondo, ImageScaling.FitWidth);

                page.Content().PaddingTop(130).Column(content =>
                {
                    //  Datos del paciente
                    content.Item().Column(col =>
                    {
                        col.Item().Text($"Paciente: {info.Paciente}")
                            .FontSize(13).Bold();

                        col.Item().Text($"Sexo: {info.SexoPaciente ?? "N/A"}  |  Teléfono: {info.TelefonoPaciente ?? "—"}")
                            .FontSize(11);

                        if (!string.IsNullOrWhiteSpace(info.ReferidoPor) && info.ReferidoPor != "Particular")
                        {
                            col.Item().Text($"Referido por: {info.ReferidoPor}")
                                .FontSize(11)
                                .FontColor(Colors.Blue.Darken2)
                                .Italic();
                        }

                        col.Item().PaddingTop(5)
                            .Text($"Examen: {info.TipoExamen}")
                            .FontSize(12)
                            .Bold();

                        col.Item().Text($"Descripción: {info.DescripcionExamen ?? "Sin descripción."}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    //  Secciones agrupadas por título
                    foreach (var grupo in grupos)
                    {
                        content.Item().PaddingTop(12).Text(grupo.Titulo)
                            .Bold()
                            .FontSize(12)
                            .FontColor(Colors.Black);

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Parámetro").Bold();
                                h.Cell().Text("Unidad").Bold();
                                h.Cell().Text("Rango Ref.").Bold();
                                h.Cell().Text("Resultado").Bold();
                                h.Cell().Text("Observaciones").Bold();
                            });

                            foreach (var p in grupo.Items)
                            {
                                table.Cell().Text(p.ParametroPlantilla ?? p.ParametroBase ?? "-").FontSize(10);
                                table.Cell().Text(p.UnidadPlantilla ?? p.UnidadBase ?? "-").FontSize(10);
                                table.Cell().Text(p.RangoReferenciaPlantilla ?? p.ValorReferenciaBase ?? "-").FontSize(10);
                                table.Cell().Text(p.ResultadoParametro ?? p.ResultadoGeneral ?? "-").FontSize(10);
                                table.Cell().Text(p.ObservacionesParametro ?? "-").FontSize(10);
                            }
                        });
                    }
                });

                // Pie de página
                page.Footer().AlignCenter().Column(f =>
                {
                    f.Item().Text("Fin del Reporte Clínico")
                        .Italic().FontSize(9).FontColor(Colors.Grey.Darken1);

                    f.Item().Text($"Emitido por: Laboratorio Clínico CDC Poptún — Fecha: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1)
                        .Italic();
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"examen_{id}.pdf");
    }



    // =====================================================
    // Reporte múltiple de exámenes
    // =====================================================
    [HttpPost("reporte-multiple")]
    [AllowAnonymous]
    public IActionResult GenerarReporteMultiple([FromBody] List<int> idsExamenes)
    {
        if (idsExamenes == null || !idsExamenes.Any())
            return BadRequest(new { message = "No se recibieron exámenes para imprimir." });

        var examenes = _db.VistaExamenReporteClinico
            .Where(v => idsExamenes.Contains(v.IdExamen))
            .OrderBy(v => v.IdExamen)
            .ThenBy(v => v.IdParametroPlantilla)
            .ToList();

        if (!examenes.Any())
            return NotFound(new { message = "No se encontraron datos para los exámenes seleccionados." });

        var assets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        var candidatos = new[]
        {
        "hoja_membretada.png", "hoja membretada.png",
        "hoja_membretada.pdf", "hoja membretada.pdf"
    };
        var fondo = candidatos
            .Select(n => Path.Combine(assets, n))
            .FirstOrDefault(System.IO.File.Exists);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                //  Fondo institucional
                if (!string.IsNullOrEmpty(fondo))
                {
                    page.Background().Element(e => e.Image(fondo).FitArea());
                }

                // ============================
                // CONTENIDO
                // ============================
                page.Content().PaddingTop(100).Column(col =>
                {
                    var gruposPacientes = examenes.GroupBy(e => e.Paciente).ToList();

                    for (int i = 0; i < gruposPacientes.Count; i++)
                    {
                        var grupo = gruposPacientes[i];
                        var primerExamen = grupo.First();

                        //  Datos del paciente
                        col.Item().Column(header =>
                        {
                            header.Item().Text($"Paciente: {primerExamen.Paciente}")
                                .FontSize(13).Bold();

                            header.Item().Text(
                                $"Sexo: {primerExamen.SexoPaciente ?? "N/A"}  |  Teléfono: {primerExamen.TelefonoPaciente ?? "—"}  |  Edad: {(primerExamen.Edad?.ToString() ?? "—")} años"
                            ).FontSize(11);

                            if (!string.IsNullOrWhiteSpace(primerExamen.ReferidoPor) && primerExamen.ReferidoPor != "Particular")
                            {
                                header.Item()
                                    .Text($"Referido por: {primerExamen.ReferidoPor}")
                                    .FontSize(11)
                                    .FontColor(Colors.Blue.Darken2)
                                    .Italic();
                            }
                        });

                        //  Por cada examen del paciente
                        foreach (var examen in grupo.GroupBy(e => e.IdExamen))
                        {
                            var e = examen.First();

                            col.Item().PaddingTop(15).Column(header =>
                            {
                                header.Item().Text($"Examen: {e.TipoExamen}")
                                    .FontSize(12).Bold();
                                header.Item().Text($"Descripción: {e.DescripcionExamen ?? "(Sin descripción)"}")
                                    .FontSize(11).FontColor(Colors.Grey.Darken2);
                            });

                            //  Agrupar parámetros por títulos
                            var filas = examen.ToList();
                            var grupos = new List<(string Titulo, List<VistaExamenReporteClinico> Items)>();
                            string? tituloActual = null;

                            foreach (var fila in filas)
                            {
                                var param = _db.ParametrosTipoExamen
                                    .FirstOrDefault(x => x.Nombre == (fila.ParametroPlantilla ?? fila.ParametroBase));

                                bool esTitulo = param != null && param.EsTitulo == 1;

                                if (esTitulo)
                                {
                                    tituloActual = (fila.ParametroPlantilla ?? fila.ParametroBase)?.ToUpper();
                                    grupos.Add((tituloActual!, new List<VistaExamenReporteClinico>()));
                                }
                              
                              
                                   else
                                    {
                                        if (grupos.Count == 0)
                                        {
                                            grupos.Add((null, new List<VistaExamenReporteClinico>()));
                                        }

                                        grupos.Last().Items.Add(fila);
                                    }
                               
                            }

                            //  Mostrar cada grupo en orden
                            foreach (var grupoTitulo in grupos)
                            {
                                col.Item().PaddingTop(8).Text(grupoTitulo.Titulo)
                                    .Bold()
                                    .FontSize(12)
                                    .FontColor(Colors.Black);

                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(1);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Text("Parámetro").Bold();
                                        h.Cell().Text("Unidad").Bold();
                                        h.Cell().Text("Rango Ref.").Bold();
                                        h.Cell().Text("Resultado").Bold();
                                        h.Cell().Text("Observaciones").Bold();
                                    });

                                    foreach (var f in grupoTitulo.Items)
                                    {
                                        table.Cell().Text(f.ParametroPlantilla ?? f.ParametroBase ?? "-").FontSize(10);
                                        table.Cell().Text(f.UnidadPlantilla ?? f.UnidadBase ?? "-").FontSize(10);
                                        table.Cell().Text(f.RangoReferenciaPlantilla ?? f.ValorReferenciaBase ?? "-").FontSize(10);
                                        table.Cell().Text(f.ResultadoParametro ?? "-").FontSize(10);
                                        table.Cell().Text(f.ObservacionesParametro ?? "-").FontSize(10);
                                    }
                                });
                            }

                            col.Item().PaddingVertical(6)
                                .LineHorizontal(0.5f)
                                .LineColor(Colors.Grey.Lighten1);
                        }

                        //  Salto de página entre pacientes
                        if (i < gruposPacientes.Count - 1)
                            col.Item().PageBreak();
                    }
                });

                // ============================
                // PIE GLOBAL
                // ============================
                page.Footer().Column(footer =>
                {
                    footer.Item().Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(300).Column(f =>
                        {
                            f.Item().AlignCenter()
                                .Text("Firma del Laboratorista")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);

                            f.Item().LineHorizontal(1)
                                .LineColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem();
                    });

                    footer.Item().AlignCenter()
                        .Text("Fin del Reporte Clínico")
                        .Italic().FontSize(9).FontColor(Colors.Grey.Darken1);

                    footer.Item().AlignCenter().PaddingTop(6)
                        .Text($"Emitido por: Laboratorio Clínico CDC Poptún — Fecha: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1)
                        .Italic();
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", "reporte-multiple.pdf");
    }

    // =====================================================
    // AGRUPACIÓN DE EXÁMENES
    // =====================================================
    [Authorize]
    [HttpGet("grupos")]
    public async Task<IActionResult> GetGrupos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        // ==========================================================
        //  Obtener información del usuario logueado
        // ==========================================================
        var userIdStr = User.FindFirst("uid")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        int.TryParse(userIdStr, out int userId);

        // ==========================================================
        //  Query base con relaciones necesarias
        // ==========================================================
        var query = _db.Examenes
            .Include(e => e.TipoExamen)
            .Include(e => e.Persona)
            .Where(e => e.GrupoExamen != null)
            .WhereSucursal(_sucCtx)
            .AsQueryable();

        // ==========================================================
        //  Filtros por fecha (opcional)
        // ==========================================================
        if (desde.HasValue)
            query = query.Where(e => e.FechaRegistro >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(e => e.FechaRegistro <= hasta.Value.Date.AddDays(1));

        // ==========================================================
        //  Si el usuario es médico, filtra solo sus grupos referidos
        // ==========================================================
        if (role == "Médico")
        {
            query = query.Where(e => e.IdReferidor == userId);
        }

        // ==========================================================
        //  Agrupar por visita (grupo)
        // ==========================================================
        var grupos = await query
            .AsNoTracking()
            .GroupBy(e => new { e.GrupoExamen, Fecha = e.FechaRegistro.Date })
            .OrderByDescending(g => g.Key.Fecha)
            .Select(g => new
            {
                grupo = g.Key.GrupoExamen,
                fecha = g.Key.Fecha,
                cantidad = g.Count(),
                paciente = g.First().Persona != null
                    ? g.First().Persona.Nombre + " " + g.First().Persona.Apellido
                    : "(Sin paciente)",
                total = g.Sum(x => x.PrecioAplicado),
                
                referidor = g.First().Referidor != null
                    ? g.First().Referidor.Firstname + " " + g.First().Referidor.Lastname
                    : null
            })
            .ToListAsync();

        return Ok(grupos);
    }


    [Authorize]
    [HttpGet("grupos/{grupo}")]
    public async Task<IActionResult> GetGrupoById(string grupo)
    {
        if (string.IsNullOrWhiteSpace(grupo))
            return BadRequest("Debe especificar un grupo válido.");

        // ==========================================================
        //  Datos del usuario logueado
        // ==========================================================
        var userIdStr = User.FindFirst("uid")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        int.TryParse(userIdStr, out int userId);

        // ==========================================================
        //  Query base
        // ==========================================================
        var query = _db.Examenes
            .Include(e => e.TipoExamen)
            .Include(e => e.Persona)
            .Include(e => e.Referidor)
            .Where(e => e.GrupoExamen != null)
            .WhereSucursal(_sucCtx); // ✅


        // ==========================================================
        //  Filtrar si el usuario es médico
        // ==========================================================
        if (role == "Médico")
        {
            query = query.Where(e => e.IdReferidor == userId);
        }

        // ==========================================================
        //  Filtrar por grupo
        // ==========================================================
        var filtrados = await query
            .Where(e => e.GrupoExamen == grupo)
            .OrderBy(e => e.FechaRegistro)
            .Select(e => new
            {
                e.Id,
                TipoExamen = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)",
                Paciente = e.Persona != null ? e.Persona.Nombre + " " + e.Persona.Apellido : "(Sin paciente)",
                e.PrecioAplicado,
                e.Resultado,
                e.Estado,
                e.FechaRegistro,
                ReferidoPor = e.Referidor != null ? e.Referidor.Firstname + " " + e.Referidor.Lastname : "(Sin referidor)"
            })
            .ToListAsync();

        // ==========================================================
        //  Validación de acceso y respuesta
        // ==========================================================
        if (!filtrados.Any())
            return NotFound(new { message = "No se encontraron exámenes en este grupo." });

        var total = filtrados.Sum(x => x.PrecioAplicado);
        return Ok(new { grupo, total, examenes = filtrados });
    }


    // =====================================================
    //  GESTIÓN DE PACIENTES CON SUS EXÁMENES
    // =====================================================
    [HttpGet("por-paciente")]
    [Authorize]
    public async Task<IActionResult> GetPacientesConExamenes()
    {
        //  Datos del usuario autenticado
        int.TryParse(User.FindFirst("uid")?.Value, out int userId);
        string? userType = User.FindFirst("type")?.Value;
        string? userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

        //  Query base: examenes válidos con paciente
        var query = _db.Examenes
            .Include(e => e.Persona)
            .Where(e => e.Persona != null)
            .WhereSucursal(_sucCtx);

        //  Filtrar si es médico
        if (userType == "3" || (userRole != null && userRole.Equals("Médico", StringComparison.OrdinalIgnoreCase)))
        {
            query = query.Where(e => e.IdReferidor == userId);
        }

        var pacientes = await query
            .GroupBy(e => new
            {
                e.Persona!.Id,
                e.Persona.Nombre,
                e.Persona.Apellido,
                e.Persona.Telefono,
                e.Persona.Correo,
                e.Persona.Estado
            })
            .Select(g => new
            {
                idPersona = g.Key.Id,
                paciente = g.Key.Nombre + " " + g.Key.Apellido,
                correo = g.Key.Correo,
                telefono = g.Key.Telefono,
                estado = g.Key.Estado,
                fechaRegistro = g.Max(e => e.FechaRegistro)
            })
            .OrderByDescending(x => x.fechaRegistro)
            .ToListAsync();

        return Ok(pacientes);
    }



    // =======================================
    // GET: api/examenes/pacientes-referidos/{idMedico}
    // =======================================
    [HttpGet("pacientes-referidos/{idMedico}")]
    public async Task<IActionResult> GetPacientesReferidos(int idMedico)
    {
        if (idMedico <= 0)
            return BadRequest(new { message = "Id del médico inválido" });

        var pacientes = await _db.Examenes
            .Include(e => e.Persona)
            .Where(e => e.IdReferidor == idMedico)
            .WhereSucursal(_sucCtx) 
            .Select(e => new
            {
                id = e.Persona.Id,
                nombre = e.Persona.Nombre,
                apellido = e.Persona.Apellido,
                correo = e.Persona.Correo,
                telefono = e.Persona.Telefono,
                estado = e.Persona.Estado,
                fechaRegistro = e.Persona.FechaRegistro
            })
            .Distinct()
            .OrderBy(p => p.apellido)
            .ToListAsync();

        return Ok(pacientes);
    }



    [HttpPost("reportes/enviar")]
    [Authorize(Roles = "Administrador,Usuario")]
    public async Task<IActionResult> EnviarReporte(
    [FromServices] IEmailService emailService,
    [FromBody] EmailReporteDto body,
    CancellationToken ct)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.correo))
            return BadRequest(new { message = "Faltan datos de destinatario o cuerpo del reporte." });

        // ✅ Plantilla HTML con estilo profesional (sin logo)
        var tabla = $@"
<style>
    body {{
        font-family: 'Segoe UI', Arial, sans-serif;
        background-color: #f4f6f9;
        margin: 0;
        padding: 0;
    }}
    .card {{
        background: #fff;
        max-width: 700px;
        margin: 20px auto;
        border-radius: 10px;
        box-shadow: 0 2px 6px rgba(0,0,0,0.15);
        overflow: hidden;
        border: 1px solid #e5e7eb;
    }}
    .header {{
        background: #0d6efd;
        color: white;
        padding: 18px 20px;
        text-align: center;
    }}
    .header h1 {{
        margin: 0;
        font-size: 20px;
    }}
    .content {{
        padding: 25px;
    }}
    h2 {{
        color: #0d6efd;
        margin-bottom: 10px;
    }}
    table {{
        border-collapse: collapse;
        width: 100%;
        font-size: 13px;
        margin-top: 10px;
    }}
    th, td {{
        border: 1px solid #dee2e6;
        padding: 8px;
        text-align: left;
    }}
    th {{
        background: #0d6efd;
        color: white;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        font-size: 12px;
    }}
    tr:nth-child(even) {{
        background: #f8f9fa;
    }}
    tr:hover {{
        background: #eef3ff;
    }}
    .footer {{
        font-size: 12px;
        text-align: center;
        color: #6c757d;
        border-top: 1px solid #e5e7eb;
        padding: 15px;
        background: #f9fafc;
    }}
</style>

<div class='card'>
    <div class='header'>
        <h1>Laboratorio Clínico CDC Poptún</h1>
    </div>
    <div class='content'>
        <h2>{body.titulo}</h2>
        <table>
            <thead>
                <tr>
                    <th>Fecha</th>
                    <th>Insumo</th>
                    <th>Cantidad</th>
                    <th>Examen</th>
                    <th>Justificación</th>
                </tr>
            </thead>
            <tbody>
                {string.Join("", body.items.Select(i => $@"
                    <tr>
                        <td>{i.fecha}</td>
                        <td>{i.insumo}</td>
                        <td>{i.cantidad}</td>
                        <td>{i.tipoExamen}</td>
                        <td>{i.justificacion}</td>
                    </tr>
                "))}
            </tbody>
        </table>
    </div>
    <div class='footer'>
        Reporte generado automáticamente por <b>LabClinic</b> • {DateTime.Now:dd/MM/yyyy HH:mm}
    </div>
</div>";

        // ✅ Envío del correo (sin logo ni recursos embebidos)
        using (var msg = new System.Net.Mail.MailMessage())
        {
            msg.From = new System.Net.Mail.MailAddress("josx009@gmail.com", "Laboratorio Clínico CDC Poptún", Encoding.UTF8);
            msg.Subject = body.asunto;
            msg.IsBodyHtml = true;
            msg.Body = tabla;

            foreach (var addr in body.correo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                msg.To.Add(addr.Trim());

            using var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new System.Net.NetworkCredential("josx009@gmail.com", "hircfosbyhhfeakl"),
                EnableSsl = true
            };

            await smtp.SendMailAsync(msg, ct);
        }

        return Ok(new { message = "📨 Reporte enviado correctamente con formato limpio y profesional (sin logo)." });
    }

}
