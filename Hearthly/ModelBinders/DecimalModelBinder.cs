using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Threading.Tasks;

public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var valueAsString = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(valueAsString))
            return Task.CompletedTask;

        // Normalize both comma and dot to the correct format
        valueAsString = valueAsString.Replace(',', '.');

        if (decimal.TryParse(valueAsString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Enter a valid decimal value.");
        }

        return Task.CompletedTask;
    }
}
