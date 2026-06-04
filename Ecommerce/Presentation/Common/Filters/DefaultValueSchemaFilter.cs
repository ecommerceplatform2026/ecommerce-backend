using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Application.Common.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace Presentation.Common.Filters
{
    public class DefaultValueSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null)
                return;

            foreach (var prop in context.Type.GetProperties())
            {
                if (!schema.Properties.TryGetValue(ToCamelCase(prop.Name), out var propSchema))
                    continue;

                var defaultValueAttr = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttr != null)
                {
                    propSchema.Example = ToOpenApiAny(defaultValueAttr.Value);
                    propSchema.Default = ToOpenApiAny(defaultValueAttr.Value);
                }

                var allowedAttr = prop.GetCustomAttribute<AllowedValuesAttribute>();
                if (allowedAttr != null)
                {
                    propSchema.Enum = allowedAttr.Values
                        .Select(v => new OpenApiString(v))
                        .Cast<IOpenApiAny>()
                        .ToList();
                }
            }
        }

        private static string ToCamelCase(string name)
        {
            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        private static IOpenApiAny? ToOpenApiAny(object? value)
        {
            if (value == null)
                return new OpenApiNull();

            return value switch
            {
                int i => new OpenApiInteger(i),
                long l => new OpenApiLong(l),
                float f => new OpenApiFloat(f),
                double d => new OpenApiDouble(d),
                bool b => new OpenApiBoolean(b),
                string s => new OpenApiString(s),
                _ => new OpenApiString(value.ToString())
            };
        }
    }
}
