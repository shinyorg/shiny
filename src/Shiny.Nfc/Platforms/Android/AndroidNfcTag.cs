using Android.Nfc;
using Android.Nfc.Tech;
using NativeMessage = Android.Nfc.NdefMessage;

namespace Shiny.Nfc;


/// <summary>
/// A tag from reader mode, spoken to through <see cref="Ndef"/> - or <see cref="NdefFormatable"/>
/// for a blank tag, which is formatted by its first write.
/// </summary>
/// <remarks>
/// Each call connects, does its I/O and closes, on the thread pool: Android tag I/O blocks, and
/// only one technology may be connected at a time.
/// </remarks>
sealed class AndroidNfcTag : INfcTag
{
    readonly Tag tag;
    readonly SemaphoreSlim gate = new(1, 1);


    public AndroidNfcTag(Tag tag)
    {
        this.tag = tag;
        this.Identifier = tag.GetId() is { Length: > 0 } id ? id : null;

        using var ndef = Ndef.Get(tag);
        if (ndef != null)
        {
            this.IsWritable = ndef.IsWritable;
            this.MaxSize = ndef.MaxSize;
        }
        else
        {
            // blank but formattable: writable once formatted, size unknown until then
            using var formatable = NdefFormatable.Get(tag);
            this.IsWritable = formatable != null;
        }
    }


    public byte[]? Identifier { get; }
    public bool IsWritable { get; private set; }
    public int? MaxSize { get; private set; }


    public Task<NdefMessage?> ReadNdef(CancellationToken ct = default) => this.Run(() =>
    {
        using var ndef = Ndef.Get(this.tag);
        if (ndef == null)
            return null; // not NDEF formatted

        ndef.Connect();
        try
        {
            var native = ndef.NdefMessage;
            if (native == null)
                return null; // formatted, but holds no message

            var message = NdefMessage.Parse(native.ToByteArray());
            return message.IsEmpty ? null : message;
        }
        finally
        {
            ndef.Close();
        }
    }, ct);


    public Task WriteNdef(NdefMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return this.Run<object?>(() =>
        {
            var native = new NativeMessage(message.ToByteArray());

            using var ndef = Ndef.Get(this.tag);
            if (ndef != null)
            {
                if (!ndef.IsWritable)
                    throw new NfcTagReadOnlyException();

                if (message.Length > ndef.MaxSize)
                    throw new NdefMessageTooLargeException(message.Length, ndef.MaxSize);

                ndef.Connect();
                try
                {
                    ndef.WriteNdefMessage(native);
                }
                finally
                {
                    ndef.Close();
                }
                return null;
            }

            using var formatable = NdefFormatable.Get(this.tag)
                ?? throw new NfcNotSupportedException("This tag cannot hold NDEF", NfcCapabilities.WriteNdef);

            formatable.Connect();
            try
            {
                formatable.Format(native);
            }
            finally
            {
                formatable.Close();
            }

            // the tag is NDEF now - pick up its real size and lock state
            using var formatted = Ndef.Get(this.tag);
            if (formatted != null)
            {
                this.IsWritable = formatted.IsWritable;
                this.MaxSize = formatted.MaxSize;
            }
            return null;
        }, ct);
    }


    public Task MakeReadOnly(CancellationToken ct = default) => this.Run<object?>(() =>
    {
        using var ndef = Ndef.Get(this.tag)
            ?? throw new NfcNotSupportedException("Only an NDEF formatted tag can be made read-only - write a message to it first", NfcCapabilities.MakeReadOnly);

        if (!ndef.CanMakeReadOnly())
            throw new NfcNotSupportedException("This tag cannot be made read-only", NfcCapabilities.MakeReadOnly);

        ndef.Connect();
        try
        {
            if (!ndef.MakeReadOnly())
                throw new NfcException("The tag refused to be made read-only");
        }
        finally
        {
            ndef.Close();
        }
        this.IsWritable = false;
        return null;
    }, ct);


    async Task<T> Run<T>(Func<T> io, CancellationToken ct)
    {
        await this.gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // tag I/O can't be interrupted; cancellation only stops new work from starting
            ct.ThrowIfCancellationRequested();
            return await Task.Run(() => Translate(io), CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            this.gate.Release();
        }
    }


    static T Translate<T>(Func<T> io)
    {
        try
        {
            return io();
        }
        catch (TagLostException ex)
        {
            throw new NfcTagLostException(ex);
        }
        catch (Android.Nfc.FormatException ex)
        {
            throw new NdefFormatException($"The tag holds an invalid NDEF message: {ex.Message}");
        }
        catch (Java.Lang.SecurityException ex)
        {
            // thrown when the tag object is stale - it left the field and a new one was discovered
            throw new NfcTagLostException(ex);
        }
        catch (Java.IO.IOException ex)
        {
            throw new NfcException($"Talking to the tag failed: {ex.Message}", ex);
        }
    }
}
