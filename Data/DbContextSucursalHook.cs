using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using LabClinic.Api.Common;

namespace LabClinic.Api.Data
{
    public static class DbContextSucursalHook
    {
        public static void StampSucursal(this DbContext ctx, ISucursalContext sucCtx)
        {
            foreach (var e in ctx.ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added))
            {
                var prop = e.Properties.FirstOrDefault(p =>
                    string.Equals(p.Metadata.Name, "IdSucursal", StringComparison.OrdinalIgnoreCase));

                if (prop is PropertyEntry pe &&
                    (pe.CurrentValue == null || Convert.ToInt32(pe.CurrentValue) <= 0))
                {
                    pe.CurrentValue = sucCtx.CurrentSucursalId;
                }
            }
        }
    }
}
