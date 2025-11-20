using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace LabClinic.Api.Common
{
    public class AddSucursalHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Sucursal-Id",
                In = ParameterLocation.Header,
                Description = "ID de la sucursal activa (1 = Central, 2 = Norte, etc.)",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Default = new OpenApiInteger(1)
                }
            });
        }
    }
}
