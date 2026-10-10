using System.Runtime.InteropServices;
using System.Text;

namespace Shiny.Gamepad.Evdev;


/// <summary>
/// One open <c>/dev/input/event*</c> node.
/// </summary>
/// <remarks>
/// <para>Owns the file descriptor, the capability probe and the reader thread. Everything the
/// kernel will say about a controller comes from here: its name, its vendor and product ids, which
/// buttons and axes it has, what each axis's range and resting slop are, and whether it can
/// rumble.</para>
/// <para>The node is opened read-write when possible, because uploading a force-feedback effect is
/// a write. Most distributions grant that through the <c>input</c> group or a udev rule; where it
/// is refused the node is reopened read-only, the controller works normally, and rumble is simply
/// not offered.</para>
/// </remarks>
sealed unsafe class EvdevDevice : IDisposable
{
    readonly int fd;
    readonly int shutdownRead;
    readonly int shutdownWrite;
    readonly Dictionary<ushort, InputAbsInfo> axisRanges = new();
    Thread? reader;
    short effectId = -1;
    volatile bool disposed;


    EvdevDevice(string path, int fd, bool writable, int shutdownRead, int shutdownWrite)
    {
        this.Path = path;
        this.fd = fd;
        this.IsWritable = writable;
        this.shutdownRead = shutdownRead;
        this.shutdownWrite = shutdownWrite;
    }


    public string Path { get; }
    public bool IsWritable { get; }
    public string Name { get; private set; } = "Gamepad";
    public string? Unique { get; private set; }
    public InputId Id { get; private set; }
    public bool CanRumble { get; private set; }
    public bool IsAccelerometer { get; private set; }
    public IReadOnlyCollection<ushort> Buttons { get; private set; } = [];
    public IReadOnlyCollection<ushort> Axes { get; private set; } = [];


    /// <summary>
    /// Opens a device node and reads everything the kernel knows about it, or returns null when it
    /// cannot be opened.
    /// </summary>
    /// <remarks>
    /// Returning null rather than throwing is deliberate: <c>/dev/input</c> is full of nodes this
    /// process has no business reading - the power button, the lid switch, other users' devices -
    /// and enumeration has to step over them rather than stop at the first one.
    /// </remarks>
    public static EvdevDevice? TryOpen(string path)
    {
        // read-write first so rumble is available where permissions allow it; a read-only fallback
        // keeps a controller usable on a system that has not granted the input group
        var fd = EvdevNative.Open(path, EvdevCodes.O_RDWR | EvdevCodes.O_CLOEXEC);
        var writable = fd >= 0;

        if (!writable)
            fd = EvdevNative.Open(path, EvdevCodes.O_RDONLY | EvdevCodes.O_CLOEXEC);

        if (fd < 0)
            return null;

        // the reader blocks in poll(2), and closing a descriptor another thread is blocked on is
        // undefined; a pipe it also polls gives a defined way to wake it
        var pipeFds = stackalloc int[2];
        if (EvdevNative.Pipe(pipeFds) != 0)
        {
            EvdevNative.Close(fd);
            return null;
        }

        var device = new EvdevDevice(path, fd, writable, pipeFds[0], pipeFds[1]);
        try
        {
            device.Probe();
        }
        catch
        {
            device.Dispose();
            throw;
        }

        return device;
    }


    void Probe()
    {
        this.Name = this.ReadString(EvdevNative.GetName(256)) ?? "Gamepad";
        this.Unique = this.ReadString(EvdevNative.GetUniq(256));

        var id = default(InputId);
        if (EvdevNative.Ioctl(this.fd, EvdevNative.GetId(), &id) >= 0)
            this.Id = id;

        this.Buttons = this.ReadCodes(EvdevCodes.EV_KEY, EvdevCodes.KEY_MAX);
        this.Axes = this.ReadCodes(EvdevCodes.EV_ABS, EvdevCodes.ABS_MAX);

        foreach (var axis in this.Axes)
        {
            var info = default(InputAbsInfo);
            if (EvdevNative.Ioctl(this.fd, EvdevNative.GetAbsInfo(axis), &info) >= 0)
                this.axisRanges[axis] = info;
        }

        var force = this.ReadCodes(EvdevCodes.EV_FF, EvdevCodes.FF_MAX);
        this.CanRumble = this.IsWritable && force.Contains(EvdevCodes.FF_RUMBLE);

        // the kernel flags a motion-sensor node with INPUT_PROP_ACCELEROMETER, which is what tells
        // a DualSense's gyro node apart from its gamepad node - they are siblings with the same
        // name and overlapping axes, and nothing else distinguishes them
        var properties = new byte[(EvdevCodes.INPUT_PROP_MAX / 8) + 1];
        fixed (byte* buffer = properties)
        {
            if (EvdevNative.Ioctl(this.fd, EvdevNative.GetProperties((nuint)properties.Length), buffer) >= 0)
                this.IsAccelerometer = EvdevNative.HasBit(properties, EvdevCodes.INPUT_PROP_ACCELEROMETER);
        }
    }


    string? ReadString(nuint request)
    {
        var buffer = new byte[256];
        fixed (byte* pointer = buffer)
        {
            var length = EvdevNative.Ioctl(this.fd, request, pointer);
            if (length <= 0)
                return null;

            // the kernel includes the trailing NUL in the returned length
            var text = Encoding.UTF8.GetString(buffer, 0, length).TrimEnd('\0').Trim();

            return text.Length == 0 ? null : text;
        }
    }


    HashSet<ushort> ReadCodes(ushort eventType, int maxCode)
    {
        var bitmap = new byte[(maxCode / 8) + 1];
        var codes = new HashSet<ushort>();

        fixed (byte* buffer = bitmap)
        {
            if (EvdevNative.Ioctl(this.fd, EvdevNative.GetBits(eventType, (nuint)bitmap.Length), buffer) < 0)
                return codes;
        }

        for (var code = 0; code <= maxCode; code++)
        {
            if (EvdevNative.HasBit(bitmap, code))
                codes.Add((ushort)code);
        }

        return codes;
    }


    /// <summary>The range and resting slop of one axis, or null when the device has no such axis.</summary>
    public InputAbsInfo? GetAxisRange(ushort axis)
        => this.axisRanges.TryGetValue(axis, out var info) ? info : null;


    /// <summary>
    /// Normalises a raw axis value into -1..1, or 0..1 for a trigger.
    /// </summary>
    /// <remarks>
    /// <para>evdev reports raw driver units: a stick might run 0-255, -32768-32767 or 0-1023
    /// depending on the controller, and a trigger's range is unrelated to its neighbour's. The
    /// range comes from <c>EVIOCGABS</c>, so this is exact rather than assumed.</para>
    /// <para><c>flat</c> is the driver's own statement of how much the axis wobbles at rest, and
    /// is applied here for the same reason Android's is: it is the difference between centred and
    /// noisy, which the caller has no way to discover. It is not a gameplay deadzone.</para>
    /// </remarks>
    public float Normalise(ushort axis, int value, bool trigger)
    {
        if (!this.axisRanges.TryGetValue(axis, out var info) || info.Maximum <= info.Minimum)
            return 0f;

        var range = (float)(info.Maximum - info.Minimum);

        if (trigger)
            return Math.Clamp((value - info.Minimum) / range, 0f, 1f);

        var centre = (info.Minimum + info.Maximum) / 2f;
        if (info.Flat > 0 && MathF.Abs(value - centre) <= info.Flat)
            return 0f;

        return Math.Clamp((2f * (value - info.Minimum) / range) - 1f, -1f, 1f);
    }


    /// <summary>
    /// Starts a thread that reads events until the device is disposed.
    /// </summary>
    /// <remarks>
    /// A thread rather than async I/O because evdev offers no completion-port equivalent: the only
    /// way to wait for input is to block in <c>poll</c>. One thread per controller is the cost, and
    /// it is why the thread is a background one - a controller left connected must not keep the
    /// process alive.
    /// </remarks>
    public void StartReading(Action<InputEvent> onEvent, Action<Exception> onError)
    {
        this.reader = new Thread(() => this.ReadLoop(onEvent, onError))
        {
            IsBackground = true,
            Name = $"evdev {this.Path}"
        };
        this.reader.Start();
    }


    void ReadLoop(Action<InputEvent> onEvent, Action<Exception> onError)
    {
        var events = stackalloc InputEvent[32];
        var fds = stackalloc PollFd[2];

        try
        {
            while (!this.disposed)
            {
                fds[0] = new PollFd { Fd = this.fd, Events = EvdevCodes.POLLIN };
                fds[1] = new PollFd { Fd = this.shutdownRead, Events = EvdevCodes.POLLIN };

                var ready = EvdevNative.Poll(fds, 2, -1);
                if (ready < 0)
                {
                    var error = Marshal.GetLastPInvokeError();

                    // EINTR: a signal arrived while blocked, which is not a failure
                    if (error == 4)
                        continue;

                    throw new GamepadException($"poll on '{this.Path}' failed with errno {error}");
                }

                if ((fds[1].Revents & EvdevCodes.POLLIN) != 0)
                    return;

                if ((fds[0].Revents & EvdevCodes.POLLIN) == 0)
                {
                    // POLLERR or POLLHUP - the controller was unplugged
                    if (fds[0].Revents != 0)
                        return;

                    continue;
                }

                // evdev delivers whole events, and several at once when the controller sends a
                // batch; reading a buffer's worth costs one syscall instead of one per axis
                var read = EvdevNative.Read(this.fd, events, (nuint)(sizeof(InputEvent) * 32));
                if (read <= 0)
                    return;

                var count = (int)(read / sizeof(InputEvent));
                for (var i = 0; i < count; i++)
                    onEvent(events[i]);
            }
        }
        catch (Exception ex)
        {
            if (!this.disposed)
                onError(ex);
        }
    }


    /// <summary>
    /// Uploads a rumble effect and plays it, replacing whatever was playing.
    /// </summary>
    /// <remarks>
    /// <para>evdev rumble is stateful in a way the rest of this library's <c>SetVibration</c> is
    /// not: an effect is uploaded once and given an id, then started and stopped by writing
    /// <c>EV_FF</c> events carrying that id. Re-uploading with the existing id in place replaces
    /// the effect without allocating a new slot - which matters, because controllers have very few
    /// slots and leaking them makes rumble stop working until the device is re-plugged.</para>
    /// <para>The effect's replay length is set to the maximum a <c>__u16</c> of milliseconds can
    /// hold, about 65 seconds. Like Android's long one-shot, this stands in for a "run until told
    /// otherwise" the kernel does not offer, and is replaced by the next call.</para>
    /// </remarks>
    public bool SetRumble(float low, float high)
    {
        if (!this.CanRumble || this.disposed)
            return false;

        if (low <= 0f && high <= 0f)
            return this.StopRumble();

        var effect = new FFEffect
        {
            Type = EvdevCodes.FF_RUMBLE,
            Id = this.effectId,
            Direction = 0,
            ReplayLength = ushort.MaxValue,
            ReplayDelay = 0,
            StrongMagnitude = (ushort)(Math.Clamp(low, 0f, 1f) * ushort.MaxValue),
            WeakMagnitude = (ushort)(Math.Clamp(high, 0f, 1f) * ushort.MaxValue)
        };

        if (EvdevNative.Ioctl(this.fd, EvdevNative.SetForceFeedback(), &effect) < 0)
            return false;

        this.effectId = effect.Id;

        return this.Play(1);
    }


    bool StopRumble()
    {
        if (this.effectId < 0)
            return true;

        return this.Play(0);
    }


    bool Play(int value)
    {
        var play = new InputEvent
        {
            Type = EvdevCodes.EV_FF,
            Code = (ushort)this.effectId,
            Value = value
        };

        return EvdevNative.Write(this.fd, &play, (nuint)sizeof(InputEvent)) > 0;
    }


    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;

        if (this.effectId >= 0)
        {
            this.StopRumble();

            var id = (int)this.effectId;
            EvdevNative.Ioctl(this.fd, EvdevNative.RemoveForceFeedback(), (void*)id);
            this.effectId = -1;
        }

        // wake the reader before closing anything it is polling
        var wake = (byte)1;
        EvdevNative.Write(this.shutdownWrite, &wake, 1);
        this.reader?.Join(TimeSpan.FromSeconds(1));
        this.reader = null;

        EvdevNative.Close(this.shutdownWrite);
        EvdevNative.Close(this.shutdownRead);
        EvdevNative.Close(this.fd);
    }
}
