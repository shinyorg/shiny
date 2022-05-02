using System.Threading.Tasks;

namespace Shiny.Nfc
{
    public interface INfcTag
    {
        NfcTagType Type { get; }
        bool IsWriteable { get; }
        Task Write(NDefRecord[] records, bool makeReadOnly);
        Task<NDefRecord[]> Read();
    }
}
