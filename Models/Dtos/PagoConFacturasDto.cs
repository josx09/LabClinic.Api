namespace LabClinic.Api.Models.Dtos
{
    public class PagoConFacturasDto
    {
        public int IdPago { get; set; }
        public string? Concepto { get; set; }
        public decimal MontoPagado { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? Metodo { get; set; }
        public string? Nota { get; set; }

    
        public int? IdPersona { get; set; }
        public string? Paciente { get; set; }

   
        public List<FacturaDto> Facturas { get; set; } = new();
    }

    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public decimal MontoTotal { get; set; }
        public string? Nit { get; set; }
        public DateTime? FechaFactura { get; set; }
        public string? Detalle { get; set; }
    }
}
