using System;
using System.Threading.Tasks;
using CoreNFC;


namespace Shiny.Nfc
{
    public class IosNfcTag : INfcTag
    {
        readonly NFCTagReaderSession session;
        readonly INFCTag nativeTag;


        public IosNfcTag(NFCTagReaderSession session, INFCTag nativeTag)
        {
            this.session = session;
            this.nativeTag = nativeTag;
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


        public async Task<NDefRecord[]> Read()
        {
            await this.session.ConnectToAsync(this.nativeTag);
            if (this.nativeTag is not INFCNdefTag writeTag)
                throw new InvalidOperationException("");

            var tcs = new TaskCompletionSource<NDefRecord[]>();
            writeTag.ReadNdef((msg, e) => 
            {
                if (e != null)
                { 
                    tcs.SetException(new Exception(e.LocalizedDescription));
                }
                else
                {
                    //NFCNdefStatus.NotSupported
                    //NFCNdefStatus.ReadWrite;
                    //NFCNdefStatus.ReadOnly

                    //msg.Records
                    //new NDefRecord
                    //                {
                    //                    Identifier = record.Identifier.ToArray(),
                    //                    Payload = record.Payload?.ToArray(),
                    //                    Uri = record.WellKnownTypeUriPayload?.ToString(),
                    //                    PayloadType = record.TypeNameFormat switch
                    //                    {
                    //                        NFCTypeNameFormat.AbsoluteUri => NfcPayloadType.Uri,
                    //                        NFCTypeNameFormat.Empty => NfcPayloadType.Empty,
                    //                        NFCTypeNameFormat.NFCExternal => NfcPayloadType.External,
                    //                        NFCTypeNameFormat.NFCWellKnown => NfcPayloadType.WellKnown,
                    //                        NFCTypeNameFormat.Unchanged => NfcPayloadType.Unchanged,
                    //                        //case NFCTypeNameFormat.Media: // TODO: mime?
                    //                        _ => NfcPayloadType.Unknown
                    //                    }
                    //                }
                }
            });
            return await tcs.Task;
        }


        public async Task Write(NDefRecord[] records, bool makeReadOnly)
        {
            await this.session.ConnectToAsync(this.nativeTag);
            if (this.nativeTag is not INFCNdefTag writeTag)
                throw new InvalidOperationException("");

            var tcs = new TaskCompletionSource<bool>();
            // TODO: build message
            writeTag.WriteNdef(null, e =>
            {
                if (e == null)
                    tcs.SetResult(true);
                else
                    tcs.SetException(new Exception(e.LocalizedDescription));
            });

            if (makeReadOnly)
            {
                tcs = new TaskCompletionSource<bool>();
                writeTag.WriteLock(e =>
                {
                    if (e == null)
                        tcs.SetResult(true);
                    else
                        tcs.SetException(new Exception(e.LocalizedDescription));
                });
                await tcs.Task;
            }
        }
    }
}
