using CoreNFC;
using Foundation;

namespace Shiny.Nfc;


/// <summary>A tag connected in an <see cref="NFCNdefReaderSession"/>.</summary>
sealed class AppleNfcTag(INFCNdefTag tag, NFCNdefStatus status, int capacity) : INfcTag
{
    NFCNdefStatus status = status;


    // the NDEF session never surfaces the UID
    public byte[]? Identifier => null;
    public bool IsWritable => this.status == NFCNdefStatus.ReadWrite;
    public int? MaxSize => this.status == NFCNdefStatus.NotSupported || capacity <= 0 ? null : capacity;


    public static Task<(NFCNdefStatus Status, int Capacity)> QueryStatus(INFCNdefTag tag)
    {
        var tcs = new TaskCompletionSource<(NFCNdefStatus, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        tag.QueryNdefStatus((status, capacity, error) =>
        {
            if (error != null)
                tcs.TrySetException(Translate(error));
            else
                tcs.TrySetResult((status, (int)capacity));
        });
        return tcs.Task;
    }


    public Task<NdefMessage?> ReadNdef(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (this.status == NFCNdefStatus.NotSupported)
            return Task.FromResult<NdefMessage?>(null);

        var tcs = new TaskCompletionSource<NdefMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        tag.ReadNdef((native, error) =>
        {
            if (error != null)
            {
                // a formatted tag with nothing on it
                if ((NFCReaderError)(long)error.Code == NFCReaderError.NdefReaderSessionErrorZeroLengthMessage)
                    tcs.TrySetResult(null);
                else
                    tcs.TrySetException(Translate(error));
                return;
            }

            try
            {
                var message = FromNative(native);
                tcs.TrySetResult(message.IsEmpty ? null : message);
            }
            catch (ArgumentException ex)
            {
                // a record CoreNFC handed over breaks the NDEF rules - a reserved type name format, say
                tcs.TrySetException(new NdefFormatException($"The tag holds an invalid NDEF message: {ex.Message}"));
            }
        });
        return tcs.Task;
    }


    public Task WriteNdef(NdefMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();

        switch (this.status)
        {
            case NFCNdefStatus.NotSupported:
                throw new NfcNotSupportedException("This tag cannot hold NDEF", NfcCapabilities.WriteNdef);

            case NFCNdefStatus.ReadOnly:
                throw new NfcTagReadOnlyException();
        }

        if (capacity > 0 && message.Length > capacity)
            throw new NdefMessageTooLargeException(message.Length, capacity);

        var native = NFCNdefMessage.Create(NSData.FromArray(message.ToByteArray()))
            ?? throw new NfcException("CoreNFC could not build an NDEF message from these records");

        return Complete(callback => tag.WriteNdef(native, callback));
    }


    public async Task MakeReadOnly(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (this.status == NFCNdefStatus.NotSupported)
            throw new NfcNotSupportedException("Only an NDEF formatted tag can be made read-only - write a message to it first", NfcCapabilities.MakeReadOnly);

        await Complete(tag.WriteLock).ConfigureAwait(false);
        this.status = NFCNdefStatus.ReadOnly;
    }


    static Task Complete(Action<Action<NSError>> call)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        call(error =>
        {
            if (error == null)
                tcs.TrySetResult();
            else
                tcs.TrySetException(Translate(error));
        });
        return tcs.Task;
    }


    static NdefMessage FromNative(NFCNdefMessage native)
    {
        var records = native.Records.Select(r => new NdefRecord(
            (NdefTypeNameFormat)(byte)r.TypeNameFormat,
            r.Type?.ToArray() ?? [],
            r.Payload?.ToArray() ?? [],
            r.Identifier?.ToArray() ?? []
        ));
        return new NdefMessage(records);
    }


    static Exception Translate(NSError error) => (NFCReaderError)(long)error.Code switch
    {
        NFCReaderError.ReaderTransceiveErrorTagConnectionLost or
        NFCReaderError.ReaderTransceiveErrorTagNotConnected or
        NFCReaderError.ReaderTransceiveErrorSessionInvalidated => new NfcTagLostException(new NSErrorException(error)),

        NFCReaderError.NdefReaderSessionErrorTagNotWritable => new NfcTagReadOnlyException(),
        NFCReaderError.NdefReaderSessionErrorTagSizeTooSmall => new NfcException("The NDEF message does not fit on the tag"),
        _ => new NfcException($"Talking to the tag failed: {error.LocalizedDescription}", new NSErrorException(error))
    };
}
