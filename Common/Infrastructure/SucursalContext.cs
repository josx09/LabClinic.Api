namespace LabClinic.Api.Common
{
    public class SucursalContext : ISucursalContext
    {
        private int _current = 1; // ✅ sucursal por defecto

        public int CurrentSucursalId => _current;

        // ✅ Implementación de la propiedad que faltaba
        public int? SucursalId => _current;

        public void Set(int sucursalId)
        {
            _current = sucursalId > 0 ? sucursalId : 1;
        }
    }
}
