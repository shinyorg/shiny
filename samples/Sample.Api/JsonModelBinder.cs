// NOTES: Taken from https://stackoverflow.com/questions/41367602/upload-files-and-json-in-asp-net-core-web-api
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sample.Api;


public class JsonModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        // Check the value sent in
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult != ValueProviderResult.None)
        {
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            // Attempt to convert the input value
            var valueAsString = valueProviderResult.FirstValue;
            var result = JsonSerializer.Deserialize(valueAsString, bindingContext.ModelType);
            if (result != null)
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}