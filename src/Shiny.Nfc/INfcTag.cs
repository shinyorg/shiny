using System.Threading.Tasks;

namespace Shiny.Nfc;

public interface INfcTag
{
    byte[] Identifier { get; }

    NfcTagType Type { get; }
    bool IsWriteable { get; }
    Task Write(NDefRecord[] records, bool makeReadOnly = false);
    Task<NDefRecord[]?> Read();
}
