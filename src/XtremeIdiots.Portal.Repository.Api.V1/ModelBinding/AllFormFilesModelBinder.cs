using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace XtremeIdiots.Portal.Repository.Api.V1.ModelBinding;

/// <summary>
/// Binds all multipart files, regardless of the multipart part name.
/// </summary>
public sealed class AllFormFilesModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var request = bindingContext.HttpContext.Request;
        if (!request.HasFormContentType)
        {
            bindingContext.Result = ModelBindingResult.Success(new FormFileCollection());
            return;
        }

        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted).ConfigureAwait(false);
        bindingContext.Result = ModelBindingResult.Success(form.Files);
    }
}
