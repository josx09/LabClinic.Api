namespace LabClinic.Api.Common
{
    public interface ISucursalContext
    {
        int CurrentSucursalId { get; }
        void Set(int sucursalId);

        int? SucursalId { get; }
    }
}
