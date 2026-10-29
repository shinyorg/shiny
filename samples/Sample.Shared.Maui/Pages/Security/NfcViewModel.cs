using Shiny.Nfc;

namespace Sample.Shared.Maui.Pages.Security;

[ShellMap<NfcPage>("nfc")]
public partial class NfcViewModel(INfcManager nfc) : ObservableObject
{
    const int MaxLog = 20;
    readonly List<string> log = new();
    CancellationTokenSource? scan;

    [ObservableProperty] string status = $"Capabilities: {nfc.Capabilities}";
    [ObservableProperty] string uri = "https://shinylib.net";
    [ObservableProperty] string text = "";
    [ObservableProperty] string scanButtonText = "Start Scan";
    [ObservableProperty] string result = "";

    static NfcScanOptions Options => new() { AlertMessage = "Hold your device near the tag" };


    [RelayCommand]
    async Task Check()
    {
        var access = await nfc.RequestAccess();
        this.Status = $"Capabilities: {nfc.Capabilities}\nAccess: {access}";
    }


    [RelayCommand]
    async Task Read()
    {
        try
        {
            var read = await nfc.ReadOne(Options);
            this.Result = read is null
                ? "The scan ended before a tag was tapped"
                : Describe(read.Identifier, read.Message);
        }
        catch (Exception ex)
        {
            this.Result = Error(ex);
        }
    }


    [RelayCommand]
    async Task Write()
    {
        try
        {
            var records = new List<NdefRecord>();
            if (!String.IsNullOrWhiteSpace(this.Uri))
                records.Add(NdefRecord.CreateUri(this.Uri.Trim()));

            if (!String.IsNullOrWhiteSpace(this.Text))
                records.Add(NdefRecord.CreateText(this.Text));

            if (records.Count == 0)
            {
                this.Result = "Enter a URI or text to write";
                return;
            }

            var message = new NdefMessage(records);
            var written = await nfc.WriteOne(message, Options);
            this.Result = written ? $"Wrote {message.Length} bytes" : "The scan ended before a tag was tapped";
        }
        catch (Exception ex)
        {
            this.Result = Error(ex);
        }
    }


    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.scan is not null)
        {
            this.StopScan();
            return;
        }

        this.scan = new CancellationTokenSource();
        this.ScanButtonText = "Stop Scan";
        this.log.Clear();
        this.Result = "Scanning - tap tags";
        try
        {
            await foreach (var tag in nfc.Scan(Options, this.scan.Token))
            {
                var message = await tag.ReadNdef(this.scan.Token);
                this.Log($"{DateTime.Now:T}  {Describe(tag.Identifier, message)}");
            }
            this.Log("Scan ended");
        }
        catch (OperationCanceledException)
        {
            this.Log("Scan stopped");
        }
        catch (Exception ex)
        {
            this.Log(Error(ex));
        }
        finally
        {
            this.scan?.Dispose();
            this.scan = null;
            this.ScanButtonText = "Start Scan";
        }
    }


    public void StopScan() => this.scan?.Cancel();


    void Log(string entry)
    {
        this.log.Insert(0, entry);
        if (this.log.Count > MaxLog)
            this.log.RemoveAt(this.log.Count - 1);

        this.Result = String.Join("\n\n", this.log);
    }


    static string Describe(byte[]? identifier, NdefMessage? message)
    {
        var id = identifier is null ? "no UID" : $"UID {Convert.ToHexString(identifier)}";
        if (message is null)
            return $"{id}: blank tag";

        var records = message.Records.Select(r => r switch
        {
            _ when r.TryGetUri(out var u) => $"URI {u}",
            _ when r.TryGetText(out var t) => $"Text '{t.Text}' ({t.LanguageCode})",
            _ when r.TryGetSmartPoster(out var p) => $"Smart poster {p.Uri}",
            _ when r.MimeType is { } mime => $"MIME {mime} ({r.Payload.Length} bytes)",
            _ => r.ToString()
        });
        return $"{id}, {message.Length} bytes:\n  " + String.Join("\n  ", records);
    }


    static string Error(Exception ex) => ex switch
    {
        NfcTagLostException => "The tag moved away - hold it still and try again",
        NfcTagReadOnlyException => "The tag is read-only",
        NdefMessageTooLargeException big => $"Too large: {big.MessageSize} bytes, the tag holds {big.MaxSize}",
        _ => $"{ex.GetType().Name}: {ex.Message}"
    };
}
