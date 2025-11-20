using Microsoft.AspNetCore.Http;

namespace LabClinic.Api.Common
{
    public class SucursalMiddleware
    {
        private readonly RequestDelegate _next;

        public SucursalMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ISucursalContext sucCtx)
        {
          
            if (context.Request.Headers.TryGetValue("X-Sucursal-Id", out var raw)
                && int.TryParse(raw.ToString(), out var idHeader) && idHeader > 0)
            {
                sucCtx.Set(idHeader);
            }
            
            else if (context.Request.Query.TryGetValue("id_sucursal", out var qv)
                     && int.TryParse(qv.ToString(), out var idQuery) && idQuery > 0)
            {
                sucCtx.Set(idQuery);
            }

            await _next(context);
        }
    }
}
