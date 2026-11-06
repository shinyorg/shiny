using System.Security.Cryptography;

namespace Shiny.BluetoothLE.McuMgr.Tests;


/// <summary>
/// A simulated MCUmgr device: answers SMP frames for the OS, image, file system, shell and stats groups and swaps
/// images on reconnect the way MCUboot does.
/// </summary>
public class FakeSmpDevice : ISmpTransport
{
    public record Slot(int Image, int SlotIndex, byte[] Hash, string Version)
    {
        public bool Active { get; set; }
        public bool Confirmed { get; set; }
        public bool Pending { get; set; }
        public bool Permanent { get; set; }
    }

    public int MaxFrameSize { get; set; } = 247;
    public int? BufferSize { get; set; }
    public bool SupportsV2 { get; set; } = true;
    public bool RevertOnSwap { get; set; }
    public byte[]? PartialUpload { get; set; }

    public List<Slot> Slots { get; } = new();
    public List<byte[]> SentFrames { get; } = new();
    public List<(SmpHeader Header, IReadOnlyDictionary<string, object?> Payload)> Requests { get; } = new();
    public Dictionary<int, MemoryStream> Uploads { get; } = new();
    public Dictionary<string, byte[]> Files { get; } = new();
    public int Resets { get; private set; }
    public int Reconnects { get; private set; }
    public Func<SmpHeader, IReadOnlyDictionary<string, object?>, Dictionary<string, object?>?>? Override { get; set; }


    public Task<byte[]> SendAsync(byte[] frame, CancellationToken cancellationToken)
    {
        this.SentFrames.Add(frame);
        var header = SmpHeader.Read(frame);
        var payload = SmpCbor.Decode(frame.AsMemory(SmpHeader.Size, header.Length));
        this.Requests.Add((header, payload));

        var version = this.SupportsV2 ? header.Version : SmpVersion.V1;
        var response = !this.SupportsV2 && header.Version == SmpVersion.V2
            ? new Dictionary<string, object?> { ["rc"] = (long)McuMgrError.UnsupportedTooNew }
            : this.Override?.Invoke(header, payload) ?? this.Handle(header, payload);

        var body = SmpCbor.Encode(response);
        var result = new byte[SmpHeader.Size + body.Length];
        new SmpHeader(header.Operation + 1, version, 0, (ushort)body.Length, header.Group, header.Sequence, header.Command).Write(result);
        body.CopyTo(result, SmpHeader.Size);
        return Task.FromResult(result);
    }


    public Task ReconnectAsync(TimeSpan delay, TimeSpan timeout, CancellationToken cancellationToken)
    {
        this.Reconnects++;
        var pending = this.Slots.Where(x => x.Pending).ToList();
        foreach (var next in pending)
        {
            var current = this.Slots.FirstOrDefault(x => x.Image == next.Image && x.SlotIndex == 0);
            next.Pending = false;
            if (!this.RevertOnSwap)
            {
                if (current != null)
                {
                    this.Slots.Remove(current);
                    this.Slots.Add(current with { SlotIndex = 1, Active = false });
                }
                this.Slots.Remove(next);
                this.Slots.Add(new Slot(next.Image, 0, next.Hash, next.Version) { Active = true, Confirmed = next.Permanent });
            }
        }
        return Task.CompletedTask;
    }


    Dictionary<string, object?> Handle(SmpHeader header, IReadOnlyDictionary<string, object?> request)
    {
        switch (header.Group, header.Command, header.Operation)
        {
            case (McuMgrGroups.Os, 0, SmpOperation.Write):
                return new() { ["r"] = request.GetString("d") };

            case (McuMgrGroups.Os, 5, SmpOperation.Write):
                this.Resets++;
                return new();

            case (McuMgrGroups.Os, 6, SmpOperation.Read):
                return this.BufferSize == null
                    ? new() { ["rc"] = (long)McuMgrError.NotSupported }
                    : new() { ["buf_size"] = this.BufferSize.Value, ["buf_count"] = 4 };

            case (McuMgrGroups.Image, 0, SmpOperation.Read):
                return this.State();

            case (McuMgrGroups.Image, 0, SmpOperation.Write):
                return this.SetState(request);

            case (McuMgrGroups.Image, 1, SmpOperation.Write):
                return this.Upload(request);

            case (McuMgrGroups.FileSystem, 0, SmpOperation.Read):
                return this.FileRead(request);

            case (McuMgrGroups.FileSystem, 0, SmpOperation.Write):
                return this.FileWrite(request);

            case (McuMgrGroups.Shell, 0, SmpOperation.Write):
                return new() { ["o"] = String.Join(' ', request.GetList("argv").Cast<string>()), ["ret"] = 0 };

            case (McuMgrGroups.Stats, 1, SmpOperation.Read):
                return new() { ["stat_list"] = new List<object?> { "smp_svr_stats" } };

            case (McuMgrGroups.Stats, 0, SmpOperation.Read):
                return new() { ["name"] = request.GetString("name"), ["fields"] = new Dictionary<string, object?> { ["ticks"] = 42L } };

            default:
                return new() { ["rc"] = (long)McuMgrError.NotSupported };
        }
    }


    Dictionary<string, object?> State() => new()
    {
        ["images"] = this.Slots
            .OrderBy(x => x.Image)
            .ThenBy(x => x.SlotIndex)
            .Select(x => (object?)new Dictionary<string, object?>
            {
                ["image"] = x.Image,
                ["slot"] = x.SlotIndex,
                ["version"] = x.Version,
                ["hash"] = x.Hash,
                ["bootable"] = true,
                ["pending"] = x.Pending,
                ["confirmed"] = x.Confirmed,
                ["active"] = x.Active,
                ["permanent"] = x.Permanent
            })
            .ToList()
    };


    Dictionary<string, object?> SetState(IReadOnlyDictionary<string, object?> request)
    {
        var hash = request.GetBytes("hash");
        var confirm = request.GetBoolean("confirm") ?? false;
        var slot = hash == null
            ? this.Slots.FirstOrDefault(x => x.Active)
            : this.Slots.FirstOrDefault(x => x.Hash.AsSpan().SequenceEqual(hash));

        if (slot == null)
            return new() { ["err"] = new Dictionary<string, object?> { ["group"] = (long)McuMgrGroups.Image, ["rc"] = 8L } };

        if (slot.Active)
        {
            slot.Confirmed |= confirm;
        }
        else
        {
            slot.Pending = true;
            slot.Permanent = confirm;
        }
        return this.State();
    }


    Dictionary<string, object?> Upload(IReadOnlyDictionary<string, object?> request)
    {
        // like Zephyr, the image number only comes with the first chunk
        var off = request.GetInt64("off")!.Value;
        if (off == 0)
            this.uploadImage = (int)(request.GetInt64("image") ?? 0);
        var image = this.uploadImage;
        var data = request.GetBytes("data")!;

        if (off == 0)
        {
            Assert.NotNull(request.GetBytes("sha"));
            Assert.NotNull(request.GetInt64("len"));
            this.Uploads[image] = new MemoryStream();
            this.Uploads[image].SetLength(request.GetInt64("len")!.Value);
            this.UploadSha = request.GetBytes("sha");

            if (this.PartialUpload != null)
            {
                this.Uploads[image].Write(this.PartialUpload);
                return new() { ["off"] = (long)this.PartialUpload.Length };
            }
        }

        var ms = this.Uploads[image];
        ms.Position = off;
        ms.Write(data);
        var next = off + data.Length;

        var response = new Dictionary<string, object?> { ["off"] = next };
        if (next == ms.Length)
        {
            var bytes = ms.ToArray();
            response["match"] = SHA256.HashData(bytes).AsSpan().SequenceEqual(this.UploadSha);

            var parsed = McuBootImage.Parse(bytes);
            this.Slots.RemoveAll(x => x.Image == image && x.SlotIndex == 1);
            this.Slots.Add(new Slot(image, 1, parsed.Hash, parsed.Version.ToString()));
        }
        return response;
    }

    byte[]? UploadSha;
    int uploadImage;


    Dictionary<string, object?> FileRead(IReadOnlyDictionary<string, object?> request)
    {
        var name = request.GetString("name")!;
        if (!this.Files.TryGetValue(name, out var file))
            return new() { ["rc"] = (long)McuMgrError.NoEntry };

        var off = (int)request.GetInt64("off")!.Value;
        var length = Math.Min(100, file.Length - off);
        var response = new Dictionary<string, object?> { ["off"] = off, ["data"] = file.AsSpan(off, length).ToArray() };
        if (off == 0)
            response["len"] = file.Length;
        return response;
    }


    Dictionary<string, object?> FileWrite(IReadOnlyDictionary<string, object?> request)
    {
        var name = request.GetString("name")!;
        var off = (int)request.GetInt64("off")!.Value;
        var data = request.GetBytes("data")!;
        if (off == 0)
            this.Files[name] = new byte[request.GetInt64("len")!.Value];

        data.CopyTo(this.Files[name], off);
        return new() { ["off"] = off + data.Length };
    }
}
