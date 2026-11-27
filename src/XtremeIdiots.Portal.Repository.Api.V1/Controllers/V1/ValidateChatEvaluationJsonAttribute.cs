using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class ValidateChatEvaluationJsonAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var limit = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;
        if (limit is { } maximum && request.ContentLength > maximum)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }

        if (limit is { } bufferLimit)
        {
            request.EnableBuffering(bufferLimit);
        }
        else
        {
            request.EnableBuffering();
        }
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body,
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true },
                context.HttpContext.RequestAborted).ConfigureAwait(false);
            ValidateStrings(document.RootElement);
        }
        catch (JsonException)
        {
            context.Result = new BadRequestResult();
            return;
        }
        catch (InvalidOperationException)
        {
            context.Result = new BadRequestResult();
            return;
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }
        // FileBufferingReadStream reports its buffer limit with this specific IOException.
        catch (IOException exception) when (exception.Message == "Buffer limit exceeded.")
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }
        finally
        {
            request.Body.Position = 0;
        }
        await next().ConfigureAwait(false);
    }

    private static void ValidateStrings(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    _ = property.Name;
                    ValidateStrings(property.Value);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ValidateStrings(item);
                }
                break;
            case JsonValueKind.String:
                _ = element.GetString();
                break;
        }
    }
}
