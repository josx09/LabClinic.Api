using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LabClinic.Api.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using System.Data;

public class ReporteGeneralRequest
{
    public List<string> Secciones { get; set; } = new();
    public string? Desde { get; set; }
    public string? Hasta { get; set; }
}


namespace LabClinic.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportesController : ControllerBase
    {
        private readonly LabDbContext _db;

        public ReportesController(LabDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        //  Reporte de Tipos de Examen
        // ==========================================================
        [HttpGet("tipoexamen")]
        public async Task<IActionResult> ReporteTipoExamen()
        {
            var examenes = await _db.TiposExamen
                .Select(t => new { t.Id, t.Nombre, t.Descripcion, t.Precio })
                .OrderBy(t => t.Nombre)
                .ToListAsync();

            if (!examenes.Any())
                return NotFound(new { message = "No hay tipos de examen registrados." });

            var totalGeneral = examenes.Sum(e => e.Precio ?? 0);
            var pdfStream = new MemoryStream();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    
                    page.Header().Column(col =>
                    {
                        string logoPath = Path.Combine("Assets", "logo_lab.png");
                        if (System.IO.File.Exists(logoPath))
                            col.Item().AlignCenter().Height(70).Image(logoPath);

                        col.Item().AlignCenter().Text("REPORTE DE TIPOS DE EXAMEN")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text("LABORATORIO CLÍNICO CENTRO DE DIAGNÓSTICO COMPLETO")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                    });

                   
                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("ID").Bold();
                                h.Cell().Text("Nombre").Bold();
                                h.Cell().Text("Descripción").Bold();
                                h.Cell().Text("Precio (Q)").Bold().AlignRight();
                            });

                            foreach (var e in examenes)
                            {
                                table.Cell().Text(e.Id.ToString());
                                table.Cell().Text(e.Nombre);
                                table.Cell().Text(e.Descripcion ?? "-");
                                table.Cell().AlignRight().Text($"Q {e.Precio:0.00}");
                            }

                            table.Cell().ColumnSpan(3).AlignRight().Text("TOTAL GENERAL:").Bold();
                            table.Cell().AlignRight().Text($"Q {totalGeneral:0.00}").Bold();
                        });
                    });

                    
                    page.Footer().AlignRight().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor("#777");
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "reporte_tipo_examen.pdf");
        }

        // ==========================================================
        // Reporte de Pacientes
        // ==========================================================
        [HttpGet("pacientes")]
        public async Task<IActionResult> ReportePacientes([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.Persons.AsQueryable();

            // Si se proporcionan fechas, filtramos por rango
            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.FechaRegistro >= desde.Value.Date && p.FechaRegistro <= hastaFinal);
            }

            var pacientes = await query.OrderBy(p => p.Nombre).ToListAsync();

            if (!pacientes.Any())
                return NotFound(new { message = "No hay pacientes registrados en el rango seleccionado." });

            var pdfStream = new MemoryStream();

            var doc = Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(40);
                    p.Size(PageSizes.A4);

                    //Encabezado
                    p.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPORTE DE PACIENTES REGISTRADOS")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (desde.HasValue && hasta.HasValue)
                        {
                            col.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                                .FontSize(10).Italic().FontColor("#666");
                        }
                    });

                    // Contenido
                    p.Content().Column(col =>
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("ID").Bold();
                                h.Cell().Text("Nombre completo").Bold();
                                h.Cell().Text("Correo").Bold();
                                h.Cell().Text("Teléfono").Bold();
                                h.Cell().Text("Estado").Bold();
                            });

                            foreach (var p in pacientes)
                            {
                                t.Cell().Text(p.Id.ToString());
                                t.Cell().Text($"{p.Nombre} {p.Apellido}");
                                t.Cell().Text(p.Correo ?? "-");
                                t.Cell().Text(p.Telefono ?? "-");
                                t.Cell().Text(p.Estado == 1 ? "Activo" : "Inactivo");
                            }
                        });
                    });

                    // Pie de página
                    p.Footer().AlignRight()
                        .Text($"Total pacientes: {pacientes.Count}")
                        .FontSize(10);
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "reporte_pacientes.pdf");
        }


        // ==========================================================
        //  Reporte de Referidos
        // ==========================================================
        [HttpGet("referidos")]
        public async Task<IActionResult> ReporteReferidos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.Persons
                .Where(p => p.TipoCliente == 1) 
                .AsQueryable();

            // Si se proporcionan fechas, filtramos por rango
            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.FechaRegistro >= desde.Value.Date && p.FechaRegistro <= hastaFinal);
            }

            var referidos = await query.OrderBy(p => p.Nombre).ToListAsync();

            if (!referidos.Any())
                return NotFound(new { message = "No hay pacientes referidos registrados en el rango seleccionado." });

            var pdfStream = new MemoryStream();
            var doc = Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(40);
                    p.Size(PageSizes.A4);

                    //  Encabezado
                    p.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPORTE DE PACIENTES REFERIDOS")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (desde.HasValue && hasta.HasValue)
                        {
                            col.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                                .FontSize(10).Italic().FontColor("#666");
                        }
                    });

                    //  Contenido
                    p.Content().Column(col =>
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("ID").Bold();
                                h.Cell().Text("Nombre completo").Bold();
                                h.Cell().Text("Teléfono").Bold();
                            });

                            foreach (var r in referidos)
                            {
                                t.Cell().Text(r.Id.ToString());
                                t.Cell().Text($"{r.Nombre} {r.Apellido}");
                                t.Cell().Text(r.Telefono ?? "-");
                            }
                        });
                    });

                    //  Pie
                    p.Footer().AlignRight()
                        .Text($"Total referidos: {referidos.Count}")
                        .FontSize(10);
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "reporte_referidos.pdf");
        }


        // ==========================================================
        //  Reporte de Exámenes
        // ==========================================================
        [HttpGet("examenes")]
        public async Task<IActionResult> ReporteExamenes([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.Examenes
                .Include(e => e.Persona)
                .Include(e => e.TipoExamen)
                .AsQueryable();

            // Si se proporcionan fechas, filtramos por rango en la fecha de registro
            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(e => e.FechaRegistro >= desde.Value.Date && e.FechaRegistro <= hastaFinal);
            }

            var examenes = await query.OrderBy(e => e.FechaRegistro).ToListAsync();

            if (!examenes.Any())
                return NotFound(new { message = "No hay exámenes registrados en el rango seleccionado." });

            var pdfStream = new MemoryStream();

            var doc = Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(40);
                    p.Size(PageSizes.A4);

                    //  Encabezado
                    p.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPORTE DE EXÁMENES REALIZADOS")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (desde.HasValue && hasta.HasValue)
                        {
                            col.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                                .FontSize(10).Italic().FontColor("#666");
                        }
                    });

                    //  Contenido
                    p.Content().Column(col =>
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("ID").Bold();
                                h.Cell().Text("Tipo de examen").Bold();
                                h.Cell().Text("Paciente").Bold();
                                h.Cell().Text("Fecha").Bold();
                            });

                            foreach (var e in examenes)
                            {
                                string paciente = e.Persona != null ? $"{e.Persona.Nombre} {e.Persona.Apellido}" : "(Sin paciente)";
                                string tipo = e.TipoExamen != null ? e.TipoExamen.Nombre : "(Sin tipo)";
                                string fecha = e.FechaRegistro.ToString("dd/MM/yyyy");

                                t.Cell().Text(e.Id.ToString());
                                t.Cell().Text(tipo);
                                t.Cell().Text(paciente);
                                t.Cell().Text(fecha);
                            }
                        });
                    });

                    //  Pie de página
                    p.Footer().AlignRight().Text($"Total exámenes: {examenes.Count}")
                        .FontSize(10);
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            return File(pdfStream, "application/pdf", "reporte_examenes.pdf");
        }


        // ==========================================================
        // Reporte de Pacientes Activos
        // ==========================================================
        [HttpGet("activos")]
        public async Task<IActionResult> ReporteActivos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.Persons
                .Where(p => p.Estado == 1)
                .AsQueryable();

            //  Filtro por rango de fechas (usa FechaRegistro)
            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.FechaRegistro >= desde.Value.Date && p.FechaRegistro <= hastaFinal);
            }

            var activos = await query.OrderBy(p => p.Nombre).ToListAsync();

            if (!activos.Any())
                return NotFound(new { message = "No hay pacientes activos registrados en el rango seleccionado." });

            var pdfStream = new MemoryStream();

            var doc = Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(40);
                    p.Size(PageSizes.A4);

                    //  Encabezado
                    p.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPORTE DE PACIENTES ACTIVOS")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        if (desde.HasValue && hasta.HasValue)
                        {
                            col.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                                .FontSize(10).Italic().FontColor("#666");
                        }
                    });

                    //  Contenido
                    p.Content().Column(col =>
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("ID").Bold();
                                h.Cell().Text("Nombre completo").Bold();
                                h.Cell().Text("Correo").Bold();
                                h.Cell().Text("Teléfono").Bold();
                            });

                            foreach (var a in activos)
                            {
                                t.Cell().Text(a.Id.ToString());
                                t.Cell().Text($"{a.Nombre} {a.Apellido}");
                                t.Cell().Text(a.Correo ?? "-");
                                t.Cell().Text(a.Telefono ?? "-");
                            }
                        });
                    });

                    //  Pie de página
                    p.Footer().AlignRight().Text($"Total activos: {activos.Count}")
                        .FontSize(10);
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "reporte_activos.pdf");
        }

        // ==========================================================
        //  Reporte General (PDF combinado con selección dinámica)
        // ==========================================================
        [HttpPost("general")]
        [Consumes("application/json")]
        public async Task<IActionResult> ReporteGeneral([FromBody] ReporteGeneralRequest data)
        {
            if (data == null || data.Secciones == null || !data.Secciones.Any())
                return BadRequest(new { message = "Debe seleccionar al menos un reporte." });

            DateTime? desde = null;
            DateTime? hasta = null;

            if (!string.IsNullOrWhiteSpace(data.Desde) &&
                DateTime.TryParse(data.Desde, out var d1))
                desde = d1;

            if (!string.IsNullOrWhiteSpace(data.Hasta) &&
                DateTime.TryParse(data.Hasta, out var d2))
                hasta = d2;

            // Normalizar nombres
            static string Normalize(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                var formD = s.Normalize(System.Text.NormalizationForm.FormD);
                var filtered = new string(formD.Where(c =>
                    System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
                return filtered.ToLowerInvariant().Trim();
            }

            var set = new HashSet<string>(data.Secciones.Select(Normalize));

            // Consultas
            var queryPersonas = _db.Persons.AsQueryable();
            var queryExamenes = _db.Examenes
                .Include(e => e.TipoExamen)
                .Include(e => e.Persona)
                .AsQueryable();

            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);
                queryPersonas = queryPersonas.Where(p => p.FechaRegistro >= desde.Value && p.FechaRegistro <= hastaFinal);
                queryExamenes = queryExamenes.Where(e => e.FechaRegistro >= desde.Value && e.FechaRegistro <= hastaFinal);
            }

            List<Person> pacientes;
            List<Examen> examenes;

            if (desde.HasValue && hasta.HasValue)
            {
                var hastaFinal = hasta.Value.Date.AddDays(1).AddTicks(-1);

                pacientes = await _db.Persons
                    .Where(p => p.FechaRegistro >= desde.Value && p.FechaRegistro <= hastaFinal)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                examenes = await _db.Examenes
                    .Include(e => e.TipoExamen)
                    .Include(e => e.Persona)
                    .Where(e => e.FechaRegistro >= desde.Value && e.FechaRegistro <= hastaFinal)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
            }
            else
            {
                
                pacientes = await _db.Persons
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                examenes = await _db.Examenes
                    .Include(e => e.TipoExamen)
                    .Include(e => e.Persona)
                    .OrderBy(e => e.Id)
                    .ToListAsync();
            }


            var pdfStream = new MemoryStream();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(col =>
                    {
                        string logoPath = Path.Combine("Assets", "logo_lab.png");
                        if (System.IO.File.Exists(logoPath))
                            col.Item().AlignCenter().Height(70).Image(logoPath);

                        col.Item().AlignCenter().Text("LABORATORIO CLÍNICO - REPORTE GENERAL")
                            .Bold().FontSize(14).FontColor(Colors.Blue.Medium);

                        if (desde.HasValue && hasta.HasValue)
                        {
                            col.Item().AlignCenter().Text($"Rango: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}")
                                .FontSize(10).FontColor("#666").Italic();
                        }

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                    });

                    // CONTENIDO DEL PDF PERMANECE IGUAL
                    page.Content().Column(col =>
                    {
                        // ============================
                        // PACIENTES
                        // ============================
                        if (set.Contains("pacientes"))
                        {
                            col.Item().PaddingBottom(5).Text("📘 Pacientes")
                                .Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                            if (pacientes.Any())
                            {
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(40);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().Text("ID").Bold();
                                        h.Cell().Text("Nombre").Bold();
                                        h.Cell().Text("Teléfono").Bold();
                                        h.Cell().Text("Estado").Bold();
                                    });

                                    foreach (var p in pacientes)
                                    {
                                        t.Cell().Text(p.Id.ToString());
                                        t.Cell().Text($"{p.Nombre} {p.Apellido}");
                                        t.Cell().Text(p.Telefono ?? "-");
                                        t.Cell().Text(p.Estado == 1 ? "Activo" : "Inactivo");
                                    }
                                });
                            }

                            col.Item().PaddingVertical(10);
                        }

                        // ============================
                        // REFERIDOS
                        // ============================
                        if (set.Contains("referidos"))
                        {
                            var referidos = pacientes.Where(p => p.TipoCliente == 1).ToList();

                            col.Item().PaddingBottom(5).Text("📗 Referidos")
                                .Bold().FontSize(13).FontColor(Colors.Green.Darken2);

                            if (referidos.Any())
                            {
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(40);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().Text("ID").Bold();
                                        h.Cell().Text("Nombre").Bold();
                                        h.Cell().Text("Teléfono").Bold();
                                    });

                                    foreach (var r in referidos)
                                    {
                                        t.Cell().Text(r.Id.ToString());
                                        t.Cell().Text($"{r.Nombre} {r.Apellido}");
                                        t.Cell().Text(r.Telefono ?? "-");
                                    }
                                });
                            }

                            col.Item().PaddingVertical(10);
                        }

                        // ============================
                        // EXÁMENES
                        // ============================
                        if (set.Contains("examenes"))
                        {
                            col.Item().PaddingBottom(5).Text("🧪 Exámenes")
                                .Bold().FontSize(13).FontColor(Colors.Red.Darken2);

                            if (examenes.Any())
                            {
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(40);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().Text("ID").Bold();
                                        h.Cell().Text("Tipo").Bold();
                                        h.Cell().Text("Paciente").Bold();
                                        h.Cell().Text("Fecha").Bold();
                                    });

                                    foreach (var e in examenes)
                                    {
                                        string paciente = e.Persona != null ? $"{e.Persona.Nombre} {e.Persona.Apellido}" : "(sin paciente)";
                                        string tipo = e.TipoExamen?.Nombre ?? "(sin tipo)";

                                        t.Cell().Text(e.Id.ToString());
                                        t.Cell().Text(tipo);
                                        t.Cell().Text(paciente);
                                        t.Cell().Text(e.FechaRegistro.ToString("dd/MM/yyyy"));
                                    }
                                });
                            }

                            col.Item().PaddingVertical(10);
                        }

                        // ============================
                        // ACTIVOS
                        // ============================
                        if (set.Contains("activos"))
                        {
                            var activos = pacientes.Where(p => p.Estado == 1).ToList();

                            col.Item().PaddingBottom(5).Text("💠 Pacientes Activos")
                                .Bold().FontSize(13).FontColor(Colors.Blue.Medium);

                            if (activos.Any())
                            {
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(40);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().Text("ID").Bold();
                                        h.Cell().Text("Nombre").Bold();
                                        h.Cell().Text("Correo").Bold();
                                        h.Cell().Text("Teléfono").Bold();
                                    });

                                    foreach (var a in activos)
                                    {
                                        t.Cell().Text(a.Id.ToString());
                                        t.Cell().Text($"{a.Nombre} {a.Apellido}");
                                        t.Cell().Text(a.Correo ?? "-");
                                        t.Cell().Text(a.Telefono ?? "-");
                                    }
                                });
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Generado el ").FontSize(9).FontColor("#777");
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold().FontSize(9);
                    });
                });
            });

            doc.GeneratePdf(pdfStream);
            pdfStream.Position = 0;
            return File(pdfStream, "application/pdf", "reporte_general.pdf");
        }



        // ==========================================================
        //  Reportes individuales en Excel (Pacientes, Referidos, Exámenes, Activos)
        // ==========================================================
        [HttpGet("excel/{tipo}")]
        public async Task<IActionResult> ExportarExcel(string tipo)
        {
            var workbook = new XLWorkbook();

            // =======================================================
            // Hoja 1: Resumen General
            // =======================================================
            var resumenWs = workbook.Worksheets.Add("Resumen General");

            int totalPacientes = await _db.Persons.CountAsync();
            int totalReferidos = await _db.Persons.CountAsync(p => p.TipoCliente == 1);
            int totalExamenes = await _db.Examenes.CountAsync();
            int totalActivos = await _db.Persons.CountAsync(p => p.Estado == 1);

            resumenWs.Cell(1, 1).Value = "LABORATORIO CLÍNICO - RESUMEN GENERAL";
            resumenWs.Range("A1:D1").Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(14)
                .Font.FontColor = XLColor.White;
            resumenWs.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.FromArgb(0, 51, 102);

            resumenWs.Cell(3, 1).Value = "Categoría";
            resumenWs.Cell(3, 2).Value = "Total";
            resumenWs.Range("A3:B3").Style.Font.SetBold();

            resumenWs.Cell(4, 1).Value = "Pacientes";
            resumenWs.Cell(4, 2).Value = totalPacientes;

            resumenWs.Cell(5, 1).Value = "Referidos";
            resumenWs.Cell(5, 2).Value = totalReferidos;

            resumenWs.Cell(6, 1).Value = "Exámenes";
            resumenWs.Cell(6, 2).Value = totalExamenes;

            resumenWs.Cell(7, 1).Value = "Activos";
            resumenWs.Cell(7, 2).Value = totalActivos;

            resumenWs.Columns().AdjustToContents();

            // =======================================================
            // Hoja 2: Reporte específico
            // =======================================================
            var ws = workbook.Worksheets.Add($"Reporte {tipo.ToUpper()}");
            int fila = 3;

            ws.Cell(1, 1).Value = $"LABORATORIO CLÍNICO - {tipo.ToUpper()}";
            ws.Range("A1:D1").Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(14)
                .Font.FontColor = XLColor.White;
            ws.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.FromArgb(0, 51, 102);

            switch (tipo.ToLower())
            {
                case "pacientes":
                    var pacientes = await _db.Persons.ToListAsync();
                    ws.Cell(fila, 1).Value = "ID";
                    ws.Cell(fila, 2).Value = "Nombre";
                    ws.Cell(fila, 3).Value = "Teléfono";
                    ws.Cell(fila, 4).Value = "Estado";
                    ws.Range(fila, 1, fila, 4).Style.Font.SetBold();
                    fila++;
                    foreach (var p in pacientes)
                    {
                        ws.Cell(fila, 1).Value = p.Id;
                        ws.Cell(fila, 2).Value = $"{p.Nombre} {p.Apellido}";
                        ws.Cell(fila, 3).Value = p.Telefono ?? "-";
                        ws.Cell(fila, 4).Value = p.Estado == 1 ? "Activo" : "Inactivo";
                        fila++;
                    }
                    break;

                case "referidos":
                    var referidos = await _db.Persons.Where(p => p.TipoCliente == 1).ToListAsync();
                    ws.Cell(fila, 1).Value = "ID";
                    ws.Cell(fila, 2).Value = "Nombre";
                    ws.Cell(fila, 3).Value = "Teléfono";
                    ws.Range(fila, 1, fila, 3).Style.Font.SetBold();
                    fila++;
                    foreach (var r in referidos)
                    {
                        ws.Cell(fila, 1).Value = r.Id;
                        ws.Cell(fila, 2).Value = $"{r.Nombre} {r.Apellido}";
                        ws.Cell(fila, 3).Value = r.Telefono ?? "-";
                        fila++;
                    }
                    break;

                case "examenes":
                    var examenes = await _db.Examenes
                        .Include(e => e.TipoExamen)
                        .Include(e => e.Persona)
                        .ToListAsync();
                    ws.Cell(fila, 1).Value = "ID";
                    ws.Cell(fila, 2).Value = "Tipo de Examen";
                    ws.Cell(fila, 3).Value = "Paciente";
                    ws.Range(fila, 1, fila, 3).Style.Font.SetBold();
                    fila++;
                    foreach (var e in examenes)
                    {
                        ws.Cell(fila, 1).Value = e.Id;
                        ws.Cell(fila, 2).Value = e.TipoExamen?.Nombre ?? "(Sin tipo)";
                        ws.Cell(fila, 3).Value = $"{e.Persona?.Nombre} {e.Persona?.Apellido}";
                        fila++;
                    }
                    break;

                case "activos":
                    var activos = await _db.Persons.Where(p => p.Estado == 1).ToListAsync();
                    ws.Cell(fila, 1).Value = "ID";
                    ws.Cell(fila, 2).Value = "Nombre";
                    ws.Cell(fila, 3).Value = "Teléfono";
                    ws.Range(fila, 1, fila, 3).Style.Font.SetBold();
                    fila++;
                    foreach (var a in activos)
                    {
                        ws.Cell(fila, 1).Value = a.Id;
                        ws.Cell(fila, 2).Value = $"{a.Nombre} {a.Apellido}";
                        ws.Cell(fila, 3).Value = a.Telefono ?? "-";
                        fila++;
                    }
                    break;

                default:
                    return BadRequest(new { message = "Tipo de reporte inválido" });
            }

            ws.Columns().AdjustToContents();

            // =======================================================
            // Exportación final
            // =======================================================
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"reporte_{tipo}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }



        // ==========================================================
        // Reporte General en Excel (todas las secciones seleccionadas)
        // ==========================================================
        [HttpPost("generalexcel")]
        [Consumes("application/json")]
        public async Task<IActionResult> ExportarExcelGeneral([FromBody] List<string> secciones)
        {
            if (secciones == null || secciones.Count == 0)
                return BadRequest(new { message = "Debe seleccionar al menos una sección." });

            static string Normalize(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                var formD = s.Normalize(System.Text.NormalizationForm.FormD);
                var filtered = new string(formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
                return filtered.ToLowerInvariant().Trim();
            }

            var set = new HashSet<string>(secciones.Select(Normalize));
            var workbook = new XLWorkbook();

            var headerColor = XLColor.FromArgb(0, 51, 102);
            var headerFont = XLColor.White;

            // ==========================================================
            //  Hoja de Resumen General
            // ==========================================================
            var resumenWs = workbook.Worksheets.Add("Resumen General");

            resumenWs.Cell(1, 1).Value = "LABORATORIO CLÍNICO - RESUMEN GENERAL";
            resumenWs.Range("A1:C1").Merge().Style
                .Font.SetBold().Font.SetFontSize(14)
                .Font.FontColor = headerFont;
            resumenWs.Range("A1:C1").Style.Fill.BackgroundColor = headerColor;

            resumenWs.Cell(3, 1).Value = "Categoría";
            resumenWs.Cell(3, 2).Value = "Total";
            resumenWs.Range("A3:B3").Style.Font.SetBold();

            var totalPacientes = await _db.Persons.CountAsync();
            var totalReferidos = await _db.Persons.CountAsync(p => p.TipoCliente == 1);
            var totalExamenes = await _db.Examenes.CountAsync();
            var totalActivos = await _db.Persons.CountAsync(p => p.Estado == 1);

            resumenWs.Cell(4, 1).Value = "Pacientes";
            resumenWs.Cell(4, 2).Value = totalPacientes;
            resumenWs.Cell(5, 1).Value = "Referidos";
            resumenWs.Cell(5, 2).Value = totalReferidos;
            resumenWs.Cell(6, 1).Value = "Exámenes";
            resumenWs.Cell(6, 2).Value = totalExamenes;
            resumenWs.Cell(7, 1).Value = "Activos";
            resumenWs.Cell(7, 2).Value = totalActivos;

            resumenWs.Cell(9, 1).Value = "📈 Nota:";
            resumenWs.Cell(9, 2).Value = "Inserta un gráfico de columnas o pastel en Excel usando estos datos para visualizar los totales.";

            resumenWs.Columns().AdjustToContents();

            // ==========================================================
            // Hoja de Pacientes
            // ==========================================================
            if (set.Contains("pacientes"))
            {
                var ws = workbook.Worksheets.Add("Pacientes");
                var pacientes = await _db.Persons.OrderBy(p => p.Nombre).ToListAsync();

                ws.Cell(1, 1).Value = "LABORATORIO CLÍNICO - PACIENTES";
                ws.Range("A1:D1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(14)
                    .Font.FontColor = headerFont;
                ws.Range("A1:D1").Style.Fill.BackgroundColor = headerColor;

                ws.Cell(3, 1).Value = "ID";
                ws.Cell(3, 2).Value = "Nombre";
                ws.Cell(3, 3).Value = "Teléfono";
                ws.Cell(3, 4).Value = "Estado";
                ws.Range("A3:D3").Style.Font.SetBold();

                int fila = 4;
                foreach (var p in pacientes)
                {
                    ws.Cell(fila, 1).Value = p.Id;
                    ws.Cell(fila, 2).Value = $"{p.Nombre} {p.Apellido}";
                    ws.Cell(fila, 3).Value = p.Telefono ?? "-";
                    ws.Cell(fila, 4).Value = p.Estado == 1 ? "Activo" : "Inactivo";
                    fila++;
                }

                ws.Columns().AdjustToContents();
            }

            // ==========================================================
            // Hoja de Referidos
            // ==========================================================
            if (set.Contains("referidos"))
            {
                var ws = workbook.Worksheets.Add("Referidos");
                var referidos = await _db.Persons.Where(p => p.TipoCliente == 1).ToListAsync();

                ws.Cell(1, 1).Value = "LABORATORIO CLÍNICO - REFERIDOS";
                ws.Range("A1:C1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(14)
                    .Font.FontColor = headerFont;
                ws.Range("A1:C1").Style.Fill.BackgroundColor = headerColor;

                ws.Cell(3, 1).Value = "ID";
                ws.Cell(3, 2).Value = "Nombre";
                ws.Cell(3, 3).Value = "Teléfono";
                ws.Range("A3:C3").Style.Font.SetBold();

                int fila = 4;
                foreach (var r in referidos)
                {
                    ws.Cell(fila, 1).Value = r.Id;
                    ws.Cell(fila, 2).Value = $"{r.Nombre} {r.Apellido}";
                    ws.Cell(fila, 3).Value = r.Telefono ?? "-";
                    fila++;
                }

                ws.Columns().AdjustToContents();
            }

            // ==========================================================
            //  Hoja de Exámenes
            // ==========================================================
            if (set.Contains("examenes"))
            {
                var ws = workbook.Worksheets.Add("Exámenes");
                var examenes = await _db.Examenes
                    .Include(e => e.TipoExamen)
                    .Include(e => e.Persona)
                    .ToListAsync();

                ws.Cell(1, 1).Value = "LABORATORIO CLÍNICO - EXÁMENES";
                ws.Range("A1:D1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(14)
                    .Font.FontColor = headerFont;
                ws.Range("A1:D1").Style.Fill.BackgroundColor = headerColor;

                ws.Cell(3, 1).Value = "ID";
                ws.Cell(3, 2).Value = "Tipo de Examen";
                ws.Cell(3, 3).Value = "Paciente";
                ws.Cell(3, 4).Value = "Estado";
                ws.Range("A3:D3").Style.Font.SetBold();

                int fila = 4;
                foreach (var e in examenes)
                {
                    ws.Cell(fila, 1).Value = e.Id;
                    ws.Cell(fila, 2).Value = e.TipoExamen?.Nombre ?? "(Sin tipo)";
                    ws.Cell(fila, 3).Value = $"{e.Persona?.Nombre} {e.Persona?.Apellido}";
                    ws.Cell(fila, 4).Value = e.Estado == 1 ? "Activo" : "Inactivo";
                    fila++;
                }

                ws.Columns().AdjustToContents();
            }

            // ==========================================================
            //  Hoja de Activos
            // ==========================================================
            if (set.Contains("activos"))
            {
                var ws = workbook.Worksheets.Add("Activos");
                var activos = await _db.Persons.Where(p => p.Estado == 1).ToListAsync();

                ws.Cell(1, 1).Value = "LABORATORIO CLÍNICO - PACIENTES ACTIVOS";
                ws.Range("A1:C1").Merge().Style
                    .Font.SetBold().Font.SetFontSize(14)
                    .Font.FontColor = headerFont;
                ws.Range("A1:C1").Style.Fill.BackgroundColor = headerColor;

                ws.Cell(3, 1).Value = "ID";
                ws.Cell(3, 2).Value = "Nombre";
                ws.Cell(3, 3).Value = "Teléfono";
                ws.Range("A3:C3").Style.Font.SetBold();

                int fila = 4;
                foreach (var a in activos)
                {
                    ws.Cell(fila, 1).Value = a.Id;
                    ws.Cell(fila, 2).Value = $"{a.Nombre} {a.Apellido}";
                    ws.Cell(fila, 3).Value = a.Telefono ?? "-";
                    fila++;
                }

                ws.Columns().AdjustToContents();
            }

            // ==========================================================
            //  Exportar archivo final
            // ==========================================================
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"reporte_general_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // Clase auxiliar para evitar dinámicos
        public class ParametroRow
        {
            public string Parametro { get; set; } = "-";
            public string Unidad { get; set; } = "-";
            public string Rango { get; set; } = "-";
            public string Valor { get; set; } = "-";
        }


        // ==========================================================
        // Reporte de Resultados Clínicos por Paciente
        // ==========================================================
        [HttpGet("resultados/{idPaciente}")]
        public async Task<IActionResult> ReporteResultadosPaciente(
            int idPaciente,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            // Validar paciente
            var paciente = await _db.Persons.FindAsync(idPaciente);
            if (paciente == null)
                return NotFound(new { message = "Paciente no encontrado." });

            //  Consultar vista clínica (une examen + parámetros + plantilla)
            var query = _db.VistaExamenReporteClinico
                .Where(v => v.IdPaciente == idPaciente);

            if (desde.HasValue) query = query.Where(v => v.FechaRegistro >= desde.Value);
            if (hasta.HasValue) query = query.Where(v => v.FechaRegistro <= hasta.Value);

            var filas = await query
                .OrderBy(v => v.IdExamen)
                .ThenBy(v => v.IdParametroPlantilla)
                .ThenBy(v => v.ParametroPlantilla)
                .ToListAsync();

            if (filas.Count == 0)
                return BadRequest(new { message = "El paciente no tiene exámenes en el rango solicitado." });

            //  Agrupar por examen
            var grupos = filas.GroupBy(f => new
            {
                f.IdExamen,
                f.TipoExamen,
                f.DescripcionExamen,
                f.ResultadoGeneral,
                f.FechaRegistro
            })
            .OrderBy(g => g.Key.FechaRegistro)
            .ToList();

            //  Cargar títulos divisores (parámetros con es_titulo = 1)
            var idsPlantilla = filas
                .Where(f => f.IdParametroPlantilla != null)
                .Select(f => f.IdParametroPlantilla!.Value)
                .Distinct()
                .ToList();

            var titulos = await _db.ParametrosTipoExamen
                .Where(p => idsPlantilla.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.EsTitulo == 1);

            //  Generar PDF
            var stream = new MemoryStream();
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    //  Encabezado
                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("REPORTE DE RESULTADOS CLÍNICOS")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().AlignCenter().Text($"{paciente.Nombre} {paciente.Apellido}")
                            .FontSize(11).FontColor(Colors.Grey.Darken2);
                        var rango = (desde.HasValue || hasta.HasValue)
                            ? $"(Rango: {(desde?.ToString("dd/MM/yyyy") ?? "…")} - {(hasta?.ToString("dd/MM/yyyy") ?? "…")})"
                            : string.Empty;
                        if (!string.IsNullOrEmpty(rango))
                            col.Item().AlignCenter().Text(rango).FontSize(9).FontColor("#777");
                        col.Item().PaddingVertical(8).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten2);
                    });

                    //  Contenido
                    page.Content().Column(col =>
                    {
                        foreach (var g in grupos)
                        {
                            col.Item().Text($"🧪 {g.Key.TipoExamen ?? "(sin tipo)"}")
                                .Bold().FontSize(13).FontColor(Colors.Blue.Darken2);

                            // Verificar si tiene parámetros
                            var tieneParams = g.Any(f => f.IdParametroPlantilla != null || f.IdParametroBase != null);
                            if (!tieneParams)
                            {
                                col.Item().PaddingBottom(10)
                                    .Text($"Resultado: {g.Key.ResultadoGeneral ?? "-"}")
                                    .FontSize(11);
                                continue;
                            }

                            //  Tabla de parámetros
                            //  Agrupar por secciones (cada título crea una nueva tabla)
                            string? tituloActual = null;
                            var buffer = new List<ParametroRow>();


                            foreach (var f in g)
                            {
                                bool esTitulo = f.IdParametroPlantilla != null
                                    && titulos.TryGetValue(f.IdParametroPlantilla.Value, out var flag)
                                    && flag;

                                if (esTitulo)
                                {
                                    // Si había datos previos, renderizamos tabla antes de cambiar título
                                    if (buffer.Any())
                                    {
                                        col.Item().Table(t =>
                                        {
                                            t.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(3);
                                                c.RelativeColumn(2);
                                                c.RelativeColumn(3);
                                                c.RelativeColumn(2);
                                            });

                                            t.Header(h =>
                                            {
                                                h.Cell().Text("Parámetro").Bold();
                                                h.Cell().Text("Unidad").Bold();
                                                h.Cell().Text("Rango Ref.").Bold();
                                                h.Cell().Text("Resultado").Bold();
                                            });

                                            foreach (var x in buffer)
                                            {
                                                t.Cell().Text(x.Parametro);
                                                t.Cell().Text(x.Unidad);
                                                t.Cell().Text(x.Rango);
                                                t.Cell().Text(x.Valor);
                                            }
                                        });
                                        buffer.Clear();
                                    }

                                    // Mostrar título como subtítulo destacado
                                    tituloActual = f.ParametroPlantilla ?? f.ParametroBase ?? "";
                                    col.Item()
                                        .PaddingTop(10)
                                        .PaddingBottom(4)
                                        .Text(tituloActual.ToUpper())
                                        .Bold()
                                        .FontSize(12)
                                        .FontColor(Colors.Blue.Medium);
                                }
                                else
                                {
                                    buffer.Add(new ParametroRow
                                    {
                                        Parametro = f.ParametroPlantilla ?? f.ParametroBase ?? "-",
                                        Unidad = f.UnidadPlantilla ?? f.UnidadBase ?? "-",
                                        Rango = f.RangoReferenciaPlantilla ?? f.ValorReferenciaBase ?? "-",
                                        Valor = f.ResultadoParametro ?? f.ResultadoGeneral ?? "-"
                                    });

                                }
                            }

                            // Renderizar última tabla si hay datos pendientes
                            if (buffer.Any())
                            {
                                col.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                    });

                                    t.Header(h =>
                                    {
                                        h.Cell().Text("Parámetro").Bold();
                                        h.Cell().Text("Unidad").Bold();
                                        h.Cell().Text("Rango Ref.").Bold();
                                        h.Cell().Text("Resultado").Bold();
                                    });

                                    foreach (var x in buffer)
                                    {
                                        t.Cell().Text(x.Parametro);
                                        t.Cell().Text(x.Unidad);
                                        t.Cell().Text(x.Rango);
                                        t.Cell().Text(x.Valor);
                                    }
                                });
                            }


                            col.Item().PaddingBottom(14);
                        }
                    });

                    //  Pie de página
                    page.Footer().AlignRight().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor("#777");
                });
            })
            .GeneratePdf(stream);

            stream.Position = 0;
            string nombreArchivo = $"reporte_resultados_paciente_{idPaciente}_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(stream, "application/pdf", nombreArchivo);
        }

        // ==========================================================
        //  Reporte de Exámenes en Excel (por rango de fechas)
        // ==========================================================
        [HttpGet("excel/examenes")]
        public async Task<IActionResult> ExcelExamenes([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var query = _db.VistaExamenReporteClinico.AsQueryable();

            if (desde.HasValue)
                query = query.Where(v => v.FechaRegistro >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(v => v.FechaRegistro <= hasta.Value);

            var data = await query.OrderBy(v => v.IdExamen).ToListAsync();

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Exámenes");

            ws.Cell(1, 1).Value = "ID Examen";
            ws.Cell(1, 2).Value = "Paciente";
            ws.Cell(1, 3).Value = "Tipo Examen";
            ws.Cell(1, 4).Value = "Parámetro";
            ws.Cell(1, 5).Value = "Resultado";
            ws.Cell(1, 6).Value = "Unidad";
            ws.Cell(1, 7).Value = "Rango";
            ws.Cell(1, 8).Value = "Fecha";

            ws.Range("A1:H1").Style.Font.SetBold().Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromArgb(0, 51, 102)).Font.FontColor = ClosedXML.Excel.XLColor.White;

            int row = 2;
            foreach (var r in data)
            {
                ws.Cell(row, 1).Value = r.IdExamen;
                ws.Cell(row, 2).Value = r.Paciente;
                ws.Cell(row, 3).Value = r.TipoExamen;
                ws.Cell(row, 4).Value = r.ParametroPlantilla ?? r.ParametroBase;
                ws.Cell(row, 5).Value = r.ResultadoParametro ?? r.ResultadoGeneral;
                ws.Cell(row, 6).Value = r.UnidadPlantilla ?? r.UnidadBase;
                ws.Cell(row, 7).Value = r.RangoReferenciaPlantilla ?? r.ValorReferenciaBase;
                ws.Cell(row, 8).Value = r.FechaRegistro?.ToString("yyyy-MM-dd HH:mm") ?? "";

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"reporte_examenes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}