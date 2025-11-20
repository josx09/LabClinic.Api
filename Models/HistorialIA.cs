using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabClinic.Api.Models
{
    [Table("historial_ia")]
    public class HistorialIA
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("prompt")]
        [Required]
        [MaxLength(1000)]
        public string Prompt { get; set; } = string.Empty;

        [Column("respuesta")]
        public string Respuesta { get; set; } = string.Empty;

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("sucursal_id")]
        public int SucursalId { get; set; }
    }
}
