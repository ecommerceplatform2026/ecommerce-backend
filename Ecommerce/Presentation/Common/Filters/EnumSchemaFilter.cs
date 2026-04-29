using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Common.Filters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Enum.Clear();

                var enumNames = Enum.GetNames(context.Type);

                foreach (var name in enumNames)
                {
                    schema.Enum.Add(new OpenApiString(name));
                }

                schema.Type = "string";
                schema.Format = null;
            }
        }
    }
}
