using Microsoft.AspNetCore.Mvc.ModelBinding;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

/// <summary>
/// Model binder for the analytics query enums (<see cref="AnalyticsCompareMode"/>,
/// <see cref="AnalyticsAlignMode"/>, <see cref="AnalyticsBucket"/>). The typed API client serialises
/// these as snake_case wire values (for example <c>previous_period</c>) rather than the enum member
/// name, which the default enum binder rejects. This binder accepts snake_case, the enum member name
/// (case-insensitive) and numeric values.
/// </summary>
public sealed class AnalyticsEnumModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var rawValue = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var enumType = bindingContext.ModelMetadata.UnderlyingOrModelType;
        var normalized = rawValue.Replace("_", string.Empty, StringComparison.Ordinal);

        if (Enum.TryParse(enumType, normalized, ignoreCase: true, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(modelName, $"The value '{rawValue}' is not valid.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Supplies <see cref="AnalyticsEnumModelBinder"/> for the analytics query enum types.
/// </summary>
public sealed class AnalyticsEnumModelBinderProvider : IModelBinderProvider
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(AnalyticsCompareMode),
        typeof(AnalyticsAlignMode),
        typeof(AnalyticsBucket)
    ];

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SupportedTypes.Contains(context.Metadata.UnderlyingOrModelType)
            ? new AnalyticsEnumModelBinder()
            : null;
    }
}
