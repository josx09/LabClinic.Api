using System.Linq.Expressions;
using LabClinic.Api.Common;

namespace LabClinic.Api.Data
{
    public static class QueryExtensions
    {
        public static IQueryable<T> WhereSucursal<T>(this IQueryable<T> query, ISucursalContext sucCtx)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (sucCtx == null) throw new ArgumentNullException(nameof(sucCtx));

            var param = Expression.Parameter(typeof(T), "e");
            var prop = typeof(T).GetProperty("IdSucursal");
            if (prop == null)
                return query; 

            Expression left = Expression.Property(param, prop);
            Expression right = Expression.Constant(sucCtx.CurrentSucursalId);

            if (prop.PropertyType == typeof(int?))
            {
               
                right = Expression.Convert(right, typeof(int?));
            }

            var equal = Expression.Equal(left, right);
            var lambda = Expression.Lambda<Func<T, bool>>(equal, param);
            return query.Where(lambda);
        }
    }
}
