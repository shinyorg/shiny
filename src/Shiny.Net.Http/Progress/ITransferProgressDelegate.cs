using System.Collections.Generic;

namespace Shiny.Net.Http;


/// <summary>
/// Overrides the text a progress surface shows. Register with
/// <c>AddTransferProgress&lt;TDelegate&gt;()</c>; it is resolved from DI so it can take dependencies - a
/// localizer, most obviously.
/// </summary>
/// <remarks>
/// Every method may return null to keep the built-in formatting, so overriding one string does not cost you
/// the rest. These run on the transfer progress callback path for every update that survives coalescing, so
/// keep them allocation-light and free of I/O.
/// </remarks>
public interface ITransferProgressDelegate
{
    /// <summary>The headline. Return null for the built-in text.</summary>
    /// <param name="snapshot">The transfers this surface covers.</param>
    string? GetTitle(TransferProgressSnapshot snapshot);

    /// <summary>The supporting detail line. Return null for the built-in text.</summary>
    /// <param name="snapshot">The transfers this surface covers.</param>
    string? GetBody(TransferProgressSnapshot snapshot);

    /// <summary>
    /// The Dynamic Island compact / status bar chip text. Keep it to a handful of characters. Return null
    /// for the built-in value selected by <see cref="TransferProgressOptions.ShortStatus"/>.
    /// </summary>
    /// <param name="snapshot">The transfers this surface covers.</param>
    string? GetShortStatus(TransferProgressSnapshot snapshot);

    /// <summary>
    /// Last look at the content before it is drawn - add or replace entries in the data bag a custom
    /// renderer or iOS widget reads.
    /// </summary>
    /// <param name="snapshot">The transfers this surface covers.</param>
    /// <param name="data">The mutable data bag that becomes <see cref="TransferProgressContent.Data"/>.</param>
    void OnContentBuilding(TransferProgressSnapshot snapshot, IDictionary<string, string> data);
}


/// <summary>
/// A no-op <see cref="ITransferProgressDelegate"/>. Inherit it and override only the pieces you want to
/// change.
/// </summary>
public abstract class TransferProgressDelegate : ITransferProgressDelegate
{
    /// <inheritdoc />
    public virtual string? GetTitle(TransferProgressSnapshot snapshot) => null;

    /// <inheritdoc />
    public virtual string? GetBody(TransferProgressSnapshot snapshot) => null;

    /// <inheritdoc />
    public virtual string? GetShortStatus(TransferProgressSnapshot snapshot) => null;

    /// <inheritdoc />
    public virtual void OnContentBuilding(TransferProgressSnapshot snapshot, IDictionary<string, string> data) { }
}
