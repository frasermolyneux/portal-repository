using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace XtremeIdiots.Portal.Repository.Api.V1.OpenApiOperationFilters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Type = "string";
                schema.Enum.Clear();

                Enum.GetNames(context.Type)
                    .ToList()
                    .ForEach(n => schema.Enum.Add(new OpenApiString(n)));
            }
        }
    }
}
