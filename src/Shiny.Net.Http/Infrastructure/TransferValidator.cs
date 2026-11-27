using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Net.Http.Infrastructure;


/// <summary>
/// The validator of the server file a managed download is writing, captured from the response that started the file.
/// A resume sends it as <c>If-Range</c> so a file that changed on the server restarts instead of being appended to.
/// Kept beside <see cref="HttpTransfer"/> in the same repository (keyed by the transfer identifier) rather than on the
/// public record, and removed with the transfer.
/// </summary>
/// <param name="Identifier">The transfer identifier</param>
/// <param name="ETag">A strong entity tag, including its quotes - weak tags can't be used for ranges</param>
/// <param name="LastModified">The server's <c>Last-Modified</c>, used only when there is no strong ETag</param>
record TransferValidator(string Identifier, string? ETag, DateTimeOffset? LastModified) : IRepositoryEntity
{
    /// <summary>
    /// The usable validator of a response, or null when it has neither a strong ETag nor a Last-Modified date.
    /// </summary>
    public static TransferValidator? FromResponse(string identifier, HttpResponseMessage response)
    {
        var etag = response.Headers.ETag;
        if (etag != null && !etag.IsWeak && !String.IsNullOrEmpty(etag.Tag))
            return new TransferValidator(identifier, etag.Tag, null);

        var lastModified = response.Content.Headers.LastModified;
        if (lastModified != null)
            return new TransferValidator(identifier, null, lastModified);

        return null;
    }


    /// <summary>
    /// The <c>If-Range</c> value for this validator, or null if it holds nothing usable.
    /// </summary>
    public RangeConditionHeaderValue? ToIfRange()
    {
        if (this.ETag != null)
        {
            var tag = new EntityTagHeaderValue(this.ETag);
            return new RangeConditionHeaderValue(tag);
        }

        if (this.LastModified != null)
            return new RangeConditionHeaderValue(this.LastModified.Value);

        return null;
    }
}


static class TransferValidatorExtensions
{
    /// <summary>
    /// Removes the stored validator of a transfer. Called wherever the transfer itself is removed.
    /// </summary>
    public static void RemoveTransferValidator(this IRepository repository, string identifier)
        => repository.Remove<TransferValidator>(identifier);


    /// <summary>
    /// Removes every stored validator. Called wherever all transfers are cleared.
    /// </summary>
    public static void ClearTransferValidators(this IRepository repository)
        => repository.Clear<TransferValidator>();
}


// A separate internal context - the public ShinyHttpJsonContext can't expose an internal type.
[Shiny.ShinyJsonContext]
[JsonSerializable(typeof(TransferValidator))]
[JsonSerializable(typeof(System.Collections.Generic.List<TransferValidator>))]
[JsonSerializable(typeof(TransferValidator[]))]
partial class ShinyHttpInternalJsonContext : JsonSerializerContext;
