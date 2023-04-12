using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApplication1;

public interface ISwaggerIgnoreParameter
{
    
}

public sealed class SwaggerFilters : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameterDescriptions = context.ApiDescription.ParameterDescriptions;

        var parametersToHide = parameterDescriptions
            .Where(ParameterHasIgnoreAttribute);

        foreach (var parameterToHide in parametersToHide)
        {
            var parameter = operation.Parameters
                .FirstOrDefault(parameter => string.Equals(parameter.Name, parameterToHide.Name, System.StringComparison.Ordinal));
            if (parameter is not null)
                operation.Parameters.Remove(parameter);
        }
    }

    private static bool ParameterHasIgnoreAttribute(ApiParameterDescription parameterDescription)
    {
        if (parameterDescription.ModelMetadata is DefaultModelMetadata
            {
                Attributes.ParameterAttributes: {} attributes
            })
        {
            return attributes.OfType<ISwaggerIgnoreParameter>().Any();
        }

        return false;
    }
}