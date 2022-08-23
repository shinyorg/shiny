using System;
using System.Linq;
using System.Threading.Tasks;
using CoreNFC;
using Foundation;

namespace Shiny.Nfc;


public class IosNfcTag : INfcTag
{
    readonly NFCTagReaderSession session;
    readonly INFCTag nativeTag;


    public IosNfcTag(NFCTagReaderSession session, INFCTag nativeTag)
    {
        this.session = session;
        this.nativeTag = nativeTag;
    }


    byte[]? identifier;
    public byte[] Identifier
    {
        get
        {
            this.identifier ??= this.nativeTag.Type switch
            {
#if XAMARIN
                NFCTagType.FeliCa => this.nativeTag.GetNFCFeliCaTag()!.CurrentIdm.ToArray(),
                NFCTagType.Iso15693 => this.nativeTag.GetNFCIso15693Tag()!.Identifier.ToArray(),
                NFCTagType.Iso7816Compatible => this.nativeTag.GetNFCIso7816Tag()!.Identifier.ToArray(),
                NFCTagType.MiFare => this.nativeTag.GetNFCMiFareTag()!.Identifier.ToArray(),
                _ => throw new InvalidProgramException("Invalid tag type")
#else
                NFCTagType.FeliCa => this.nativeTag.AsNFCFeliCaTag!.CurrentIdm.ToArray(),
                NFCTagType.Iso15693 => this.nativeTag.AsNFCIso15693Tag!.Identifier.ToArray(),
                NFCTagType.Iso7816Compatible => this.nativeTag.AsNFCIso7816Tag!.Identifier.ToArray(),
                NFCTagType.MiFare => this.nativeTag.AsNFCMiFareTag!.Identifier.ToArray(),
                _ => throw new InvalidProgramException("Invalid tag type")
#endif
            };
            return this.identifier!;
        }
    }


    public NfcTagType Type => this.nativeTag.Type switch
    {
        NFCTagType.FeliCa => NfcTagType.FeliCa,
        NFCTagType.MiFare => NfcTagType.Mifare,
        NFCTagType.Iso15693 => NfcTagType.Iso15693,
        NFCTagType.Iso7816Compatible => NfcTagType.Iso7816,
        _ => NfcTagType.Unknown
    };


    public bool IsWriteable => this.nativeTag.Available;


    public async Task<NDefRecord[]?> Read()
    {
        await this.session.ConnectToAsync(this.nativeTag);

        var tcs = new TaskCompletionSource<NDefRecord[]>();
        this.OpTag.ReadNdef((msg, e) =>
        {
            if (e != null)
            {
                tcs.SetException(new Exception(e.LocalizedDescription));
            }
            else
            {
                var records = msg
                    .Records
                    .Select(record => new NDefRecord
                    {
                        Identifier = record.Identifier.ToArray(),
                        Payload = record.Payload?.ToArray(),
                        Uri = record.WellKnownTypeUriPayload?.ToString(),
                        PayloadType = record.TypeNameFormat switch
                        {
                            NFCTypeNameFormat.AbsoluteUri => NDefPayloadType.Uri,
                            NFCTypeNameFormat.Empty => NDefPayloadType.Empty,
                            NFCTypeNameFormat.NFCExternal => NDefPayloadType.External,
                            NFCTypeNameFormat.NFCWellKnown => NDefPayloadType.WellKnown,
                            NFCTypeNameFormat.Unchanged => NDefPayloadType.Unchanged,
                            //case NFCTypeNameFormat.Media: // TODO: mime?
                            _ => NDefPayloadType.Unknown
                        }
                    })
                    .ToArray();

                tcs.SetResult(records);
            }
        });
        return await tcs.Task;
    }


    public async Task Write(NDefRecord[] records, bool makeReadOnly)
    {
        await this.session.ConnectToAsync(this.nativeTag);
        //writeTag.QueryNdefStatus((status, code, error) =>
        //{
        //    //status == NFCNdefStatus.ReadWrite
        //});

        var tcs = new TaskCompletionSource<bool>();
        var nativeRecords = records
            .Select(record => new NFCNdefPayload(
                record.PayloadType switch
                {
                    //NDefPayloadType.Mime => NFCTypeNameFormat.Empty
                    _ => NFCTypeNameFormat.Unknown
                },
                NSData.FromArray(new byte[0]), // type
                NSData.FromArray(new byte[0]), // identifier
                NSData.FromArray(new byte[0])  // payload
            ))
            .ToArray();

        var message = new NFCNdefMessage(nativeRecords);
        this.OpTag.WriteNdef(message, e =>
        {
            if (e == null)
                tcs.SetResult(true);
            else
                tcs.SetException(new Exception(e.LocalizedDescription));
        });

        if (makeReadOnly)
        {
            tcs = new TaskCompletionSource<bool>();
            this.OpTag.WriteLock(e =>
            {
                if (e == null)
                    tcs.SetResult(true);
                else
                    tcs.SetException(new Exception(e.LocalizedDescription));
            });
            await tcs.Task;
        }
    }


    INFCNdefTag? opTag;
    INFCNdefTag OpTag
    {
        get
        {
            this.opTag ??= this.nativeTag.Type switch
            {
#if XAMARIN
                NFCTagType.FeliCa => this.nativeTag.GetNFCFeliCaTag()!,
                NFCTagType.Iso15693 => this.nativeTag.GetNFCIso15693Tag()!,
                NFCTagType.Iso7816Compatible => this.nativeTag.GetNFCIso7816Tag()!,
                NFCTagType.MiFare => this.nativeTag.GetNFCMiFareTag()!,
                _ => throw new InvalidProgramException("Invalid tag type")
#else
                NFCTagType.FeliCa => this.nativeTag.AsNFCFeliCaTag!,
                NFCTagType.Iso15693 => this.nativeTag.AsNFCIso15693Tag!,
                NFCTagType.Iso7816Compatible => this.nativeTag.AsNFCIso7816Tag!,
                NFCTagType.MiFare => this.nativeTag.AsNFCMiFareTag!,
                _ => throw new InvalidProgramException("Invalid tag type")
#endif
            };
            return this.opTag;
        }
    }
}
