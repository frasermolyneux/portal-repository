using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class ValidateChatEvaluationJsonAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var mediaType = string.IsNullOrEmpty(request.ContentType) ? default : new MediaType(request.ContentType);
        var encoding = mediaType.Charset.HasValue ? mediaType.Encoding : Encoding.UTF8;
        if (encoding is null || (encoding.CodePage != Encoding.UTF8.CodePage && encoding.CodePage != Encoding.Unicode.CodePage))
        {
            await next().ConfigureAwait(false);
            return;
        }

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
            using var transcoded = encoding.CodePage == Encoding.Unicode.CodePage
                ? Encoding.CreateTranscodingStream(request.Body, new UnicodeEncoding(false, false, true),
                    new UTF8Encoding(false, true), leaveOpen: true)
                : null;
            using var document = await JsonDocument.ParseAsync(transcoded ?? request.Body,
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true },
                context.HttpContext.RequestAborted).ConfigureAwait(false);
            try
            {
                ValidateStrings(document.RootElement);
            }
            catch (InvalidOperationException)
            {
                context.Result = new BadRequestResult();
                return;
            }
        }
        catch (JsonException)
        {
            context.Result = new BadRequestResult();
            return;
        }
        catch (DecoderFallbackException)
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
