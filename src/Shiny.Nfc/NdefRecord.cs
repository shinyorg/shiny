using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shiny.Nfc;


/// <summary>
/// How an NDEF record's <see cref="NdefRecord.Type"/> is to be read - the 3-bit TNF field.
/// </summary>
public enum NdefTypeNameFormat : byte
{
    /// <summary>No type, ID or payload. Used on its own as the empty message.</summary>
    Empty = 0,

    /// <summary>An NFC Forum well-known type - <c>U</c> (URI), <c>T</c> (text), <c>Sp</c> (smart poster)...</summary>
    WellKnown = 1,

    /// <summary>The type is a MIME media type, such as <c>application/json</c>.</summary>
    Media = 2,

    /// <summary>The type is an absolute URI and the payload is usually empty.</summary>
    AbsoluteUri = 3,

    /// <summary>An NFC Forum external type, <c>domain:type</c>, such as <c>android.com:pkg</c>.</summary>
    External = 4,

    /// <summary>The payload type is not known. The type field is empty.</summary>
    Unknown = 5
}


/// <summary>
/// One record of an <see cref="NdefMessage"/>.
/// </summary>
/// <remarks>
/// Immutable. Build the common kinds with the <c>Create*</c> factories and read them back with the
/// <c>TryGet*</c> methods; the constructor is there for anything else. Chunked records are put back
/// together when a message is parsed, so a record is always whole.
/// </remarks>
public sealed class NdefRecord : IEquatable<NdefRecord>
{
    /// <summary>The well-known type of a URI record.</summary>
    public const string UriType = "U";

    /// <summary>The well-known type of a text record.</summary>
    public const string TextType = "T";

    /// <summary>The well-known type of a smart poster record.</summary>
    public const string SmartPosterType = "Sp";

    /// <summary>The external type of an Android Application Record.</summary>
    public const string AndroidApplicationType = "android.com:pkg";


    /// <summary>The empty record - TNF <see cref="NdefTypeNameFormat.Empty"/> with no type, ID or payload.</summary>
    public static NdefRecord Empty { get; } = new(NdefTypeNameFormat.Empty, default, default);


    /// <summary>
    /// Creates a record from its raw parts.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The parts break the NDEF rules for <paramref name="typeNameFormat"/> - an empty record with
    /// content, a typed record without a type, or a type or ID longer than 255 bytes.
    /// </exception>
    public NdefRecord(
        NdefTypeNameFormat typeNameFormat,
        ReadOnlyMemory<byte> type,
        ReadOnlyMemory<byte> payload,
        ReadOnlyMemory<byte> id = default
    )
    {
        if (type.Length > 255)
            throw new ArgumentException("An NDEF record type is at most 255 bytes", nameof(type));

        if (id.Length > 255)
            throw new ArgumentException("An NDEF record ID is at most 255 bytes", nameof(id));

        switch (typeNameFormat)
        {
            case NdefTypeNameFormat.Empty:
                if (!type.IsEmpty || !payload.IsEmpty || !id.IsEmpty)
                    throw new ArgumentException("An empty NDEF record has no type, ID or payload");
                break;

            case NdefTypeNameFormat.Unknown:
                if (!type.IsEmpty)
                    throw new ArgumentException("An NDEF record of unknown type has no type field", nameof(type));
                break;

            case NdefTypeNameFormat.WellKnown:
            case NdefTypeNameFormat.Media:
            case NdefTypeNameFormat.AbsoluteUri:
            case NdefTypeNameFormat.External:
                if (type.IsEmpty)
                    throw new ArgumentException($"An NDEF record with type name format {typeNameFormat} needs a type", nameof(type));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(typeNameFormat), typeNameFormat, "Not a type name format a record can be created with");
        }

        this.TypeNameFormat = typeNameFormat;
        this.Type = type.ToArray();
        this.Payload = payload.ToArray();
        this.Id = id.ToArray();
    }


    /// <summary>How <see cref="Type"/> is to be read.</summary>
    public NdefTypeNameFormat TypeNameFormat { get; }

    /// <summary>The record type, in the form <see cref="TypeNameFormat"/> says.</summary>
    public ReadOnlyMemory<byte> Type { get; }

    /// <summary>The record ID - usually empty.</summary>
    public ReadOnlyMemory<byte> Id { get; }

    /// <summary>The record payload.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary><see cref="Type"/> as a string. Record types are ASCII by specification.</summary>
    public string TypeString => Encoding.Latin1.GetString(this.Type.Span);

    /// <summary>The MIME type of a <see cref="NdefTypeNameFormat.Media"/> record, otherwise null.</summary>
    public string? MimeType => this.TypeNameFormat == NdefTypeNameFormat.Media ? this.TypeString : null;


    /// <summary>True when this is a well-known record of the given type, such as <see cref="UriType"/>.</summary>
    public bool IsWellKnown(string type)
        => this.TypeNameFormat == NdefTypeNameFormat.WellKnown && this.TypeString == type;


    #region URI

    /// <summary>
    /// Creates a well-known URI record, abbreviating the URI with the NFC Forum prefix table
    /// (<c>https://www.</c>, <c>tel:</c>, <c>mailto:</c>...) to save tag space.
    /// </summary>
    public static NdefRecord CreateUri(string uri)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);

        var (code, prefix) = NdefUriPrefixes.Match(uri);
        var rest = uri.AsSpan(prefix.Length);
        var payload = new byte[1 + Encoding.UTF8.GetByteCount(rest)];
        payload[0] = code;
        Encoding.UTF8.GetBytes(rest, payload.AsSpan(1));

        return new(NdefTypeNameFormat.WellKnown, "U"u8.ToArray(), payload);
    }


    /// <summary>
    /// Creates a well-known URI record from an absolute <see cref="System.Uri"/>.
    /// </summary>
    public static NdefRecord CreateUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            throw new ArgumentException("An NDEF URI record needs an absolute URI", nameof(uri));

        return CreateUri(uri.AbsoluteUri);
    }


    /// <summary>
    /// Creates an absolute-URI record - TNF <see cref="NdefTypeNameFormat.AbsoluteUri"/>, where the
    /// URI is the record type and the payload is empty. Prefer <see cref="CreateUri(string)"/>,
    /// which every platform opens; this form exists mostly for reading older tags.
    /// </summary>
    public static NdefRecord CreateAbsoluteUri(string uri)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);
        return new(NdefTypeNameFormat.AbsoluteUri, Ascii(uri, nameof(uri)), default);
    }


    /// <summary>
    /// Reads the URI from a well-known URI record or an absolute-URI record.
    /// </summary>
    /// <returns>False for any other record, or a URI record with a prefix code outside the NFC Forum table.</returns>
    public bool TryGetUri([NotNullWhen(true)] out string? uri)
    {
        uri = null;
        if (this.TypeNameFormat == NdefTypeNameFormat.AbsoluteUri)
        {
            uri = this.TypeString;
            return true;
        }
        if (!this.IsWellKnown(UriType) || this.Payload.IsEmpty)
            return false;

        var span = this.Payload.Span;
        var prefix = NdefUriPrefixes.Get(span[0]);
        if (prefix == null)
            return false;

        uri = prefix + Encoding.UTF8.GetString(span[1..]);
        return true;
    }

    #endregion

    #region Text

    /// <summary>
    /// Creates a well-known text record.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="languageCode">An IANA language code such as <c>en</c> or <c>fr-CA</c>, at most 63 characters.</param>
    /// <param name="utf16">Encode as UTF-16 (big-endian, with a byte order mark) instead of UTF-8.</param>
    public static NdefRecord CreateText(string text, string languageCode = "en", bool utf16 = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(languageCode);

        var lang = Ascii(languageCode, nameof(languageCode));
        if (lang.Length > 63)
            throw new ArgumentException("An NDEF text record's language code is at most 63 characters", nameof(languageCode));

        var encoded = utf16
            ? [0xFE, 0xFF, .. Encoding.BigEndianUnicode.GetBytes(text)]
            : Encoding.UTF8.GetBytes(text);

        var payload = new byte[1 + lang.Length + encoded.Length];
        payload[0] = (byte)((utf16 ? 0x80 : 0x00) | lang.Length);
        lang.CopyTo(payload, 1);
        encoded.CopyTo(payload, 1 + lang.Length);

        return new(NdefTypeNameFormat.WellKnown, "T"u8.ToArray(), payload);
    }


    /// <summary>
    /// Reads the text and language code from a well-known text record.
    /// </summary>
    /// <returns>False for any other record, or a text record too short for the language code it declares.</returns>
    public bool TryGetText([NotNullWhen(true)] out NdefText? text)
    {
        text = null;
        if (!this.IsWellKnown(TextType) || this.Payload.IsEmpty)
            return false;

        var span = this.Payload.Span;
        var status = span[0];
        var langLength = status & 0x3F;
        if (1 + langLength > span.Length)
            return false;

        var lang = Encoding.ASCII.GetString(span.Slice(1, langLength));
        var body = span[(1 + langLength)..];

        string value;
        if ((status & 0x80) == 0)
        {
            value = Encoding.UTF8.GetString(body);
        }
        else if (body.Length >= 2 && body[0] == 0xFF && body[1] == 0xFE)
        {
            value = Encoding.Unicode.GetString(body[2..]);
        }
        else
        {
            // big-endian, with or without a byte order mark - the specification's default
            if (body.Length >= 2 && body[0] == 0xFE && body[1] == 0xFF)
                body = body[2..];

            value = Encoding.BigEndianUnicode.GetString(body);
        }

        text = new(value, lang);
        return true;
    }

    #endregion

    #region MIME, external, application

    /// <summary>
    /// Creates a MIME media record. The type is lowercased, as Android does, since MIME types are
    /// case-insensitive and readers compare them byte for byte.
    /// </summary>
    public static NdefRecord CreateMime(string mimeType, ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        var normalized = mimeType.Trim().ToLowerInvariant();
        if (normalized.IndexOf('/') < 1)
            throw new ArgumentException($"'{mimeType}' is not a MIME type", nameof(mimeType));

        return new(NdefTypeNameFormat.Media, Ascii(normalized, nameof(mimeType)), data);
    }


    /// <summary>
    /// Creates an NFC Forum external type record, <c>domain:type</c>. Both parts are lowercased -
    /// external types are case-insensitive and Android matches intent filters against the
    /// lowercase form.
    /// </summary>
    public static NdefRecord CreateExternal(string domain, string type, ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        var full = $"{domain.Trim().ToLowerInvariant()}:{type.Trim().ToLowerInvariant()}";
        return new(NdefTypeNameFormat.External, Ascii(full, nameof(domain)), data);
    }


    /// <summary>
    /// Creates an Android Application Record (AAR). Put it last in a message and Android opens
    /// that app - or its Play Store page - for the tag, ahead of any intent filter. Other platforms
    /// ignore it.
    /// </summary>
    public static NdefRecord CreateAndroidApplication(string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        return CreateExternal("android.com", "pkg", Encoding.UTF8.GetBytes(packageName));
    }

    #endregion

    #region Smart poster

    /// <summary>
    /// Creates a smart poster record - a URI with an optional title.
    /// </summary>
    public static NdefRecord CreateSmartPoster(string uri, string? title = null, string languageCode = "en")
    {
        var records = new List<NdefRecord> { CreateUri(uri) };
        if (title != null)
            records.Add(CreateText(title, languageCode));

        var payload = new NdefMessage(records).ToByteArray();
        return new(NdefTypeNameFormat.WellKnown, "Sp"u8.ToArray(), payload);
    }


    /// <summary>
    /// Reads the URI and titles from a smart poster record. Action, size, type and icon
    /// sub-records are skipped.
    /// </summary>
    /// <returns>False for any other record, or a smart poster whose payload is not a valid message with a URI record.</returns>
    public bool TryGetSmartPoster([NotNullWhen(true)] out NdefSmartPoster? poster)
    {
        poster = null;
        if (!this.IsWellKnown(SmartPosterType))
            return false;

        if (!NdefMessage.TryParse(this.Payload.Span, out var inner))
            return false;

        string? uri = null;
        var titles = new List<NdefText>();
        foreach (var record in inner.Records)
        {
            if (uri == null && record.IsWellKnown(UriType) && record.TryGetUri(out var u))
                uri = u;
            else if (record.TryGetText(out var t))
                titles.Add(t);
        }
        if (uri == null)
            return false;

        poster = new(uri, titles);
        return true;
    }

    #endregion


    /// <inheritdoc />
    public bool Equals(NdefRecord? other)
        => other is not null
           && this.TypeNameFormat == other.TypeNameFormat
           && this.Type.Span.SequenceEqual(other.Type.Span)
           && this.Id.Span.SequenceEqual(other.Id.Span)
           && this.Payload.Span.SequenceEqual(other.Payload.Span);

    /// <inheritdoc />
    public override bool Equals(object? obj) => this.Equals(obj as NdefRecord);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(this.TypeNameFormat);
        hash.AddBytes(this.Type.Span);
        hash.AddBytes(this.Payload.Span);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => $"{this.TypeNameFormat}:{this.TypeString} ({this.Payload.Length} bytes)";


    static byte[] Ascii(string value, string paramName)
    {
        foreach (var c in value)
        {
            if (c < 0x20 || c > 0x7E)
                throw new ArgumentException($"'{value}' must be printable ASCII", paramName);
        }
        return Encoding.ASCII.GetBytes(value);
    }
}


/// <summary>The content of a text record.</summary>
/// <param name="Text">The text.</param>
/// <param name="LanguageCode">The IANA language code, such as <c>en</c>.</param>
public record NdefText(string Text, string LanguageCode);


/// <summary>The content of a smart poster record.</summary>
/// <param name="Uri">The poster's URI.</param>
/// <param name="Titles">Its titles, one per language - often empty or one.</param>
public record NdefSmartPoster(string Uri, IReadOnlyList<NdefText> Titles);
