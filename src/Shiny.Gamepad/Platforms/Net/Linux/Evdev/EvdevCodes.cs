namespace Shiny.Gamepad.Evdev;


/// <summary>
/// The pieces of <c>linux/input-event-codes.h</c> a controller needs.
/// </summary>
/// <remarks>
/// These are kernel ABI - they have not changed since evdev was introduced and cannot, because
/// every existing binary depends on them.
/// </remarks>
static class EvdevCodes
{
    // event types
    public const ushort EV_SYN = 0x00;
    public const ushort EV_KEY = 0x01;
    public const ushort EV_ABS = 0x03;
    public const ushort EV_FF = 0x15;

    public const int EV_MAX = 0x1f;
    public const int KEY_MAX = 0x2ff;
    public const int ABS_MAX = 0x3f;
    public const int FF_MAX = 0x7f;
    public const int INPUT_PROP_MAX = 0x1f;

    // device properties
    public const int INPUT_PROP_ACCELEROMETER = 0x06;

    /// <summary>
    /// The buttons the kernel's gamepad specification defines, named by position on the pad.
    /// </summary>
    /// <remarks>
    /// The kernel's legacy aliases are actively misleading here: <c>BTN_X</c> is defined as
    /// <c>BTN_NORTH</c> and <c>BTN_Y</c> as <c>BTN_WEST</c>, which is the opposite of the Xbox
    /// layout everything else in this library uses. Working from the compass names avoids
    /// inheriting that swap.
    /// </remarks>
    public const ushort BTN_SOUTH = 0x130;
    public const ushort BTN_EAST = 0x131;
    public const ushort BTN_C = 0x132;
    public const ushort BTN_NORTH = 0x133;
    public const ushort BTN_WEST = 0x134;
    public const ushort BTN_Z = 0x135;
    public const ushort BTN_TL = 0x136;
    public const ushort BTN_TR = 0x137;
    public const ushort BTN_TL2 = 0x138;
    public const ushort BTN_TR2 = 0x139;
    public const ushort BTN_SELECT = 0x13a;
    public const ushort BTN_START = 0x13b;
    public const ushort BTN_MODE = 0x13c;
    public const ushort BTN_THUMBL = 0x13d;
    public const ushort BTN_THUMBR = 0x13e;

    public const ushort BTN_DPAD_UP = 0x220;
    public const ushort BTN_DPAD_DOWN = 0x221;
    public const ushort BTN_DPAD_LEFT = 0x222;
    public const ushort BTN_DPAD_RIGHT = 0x223;

    // the touchpad click on a DualShock 4 or DualSense
    public const ushort BTN_TOUCHPAD = 0x14a;

    // absolute axes - the kernel gamepad spec maps triggers onto Z and RZ, and the D-pad onto HAT0
    public const ushort ABS_X = 0x00;
    public const ushort ABS_Y = 0x01;
    public const ushort ABS_Z = 0x02;
    public const ushort ABS_RX = 0x03;
    public const ushort ABS_RY = 0x04;
    public const ushort ABS_RZ = 0x05;
    public const ushort ABS_HAT0X = 0x10;
    public const ushort ABS_HAT0Y = 0x11;

    // force feedback
    public const ushort FF_RUMBLE = 0x50;

    // open(2) flags, as Linux defines them
    public const int O_RDONLY = 0x0000;
    public const int O_RDWR = 0x0002;
    public const int O_NONBLOCK = 0x0800;
    public const int O_CLOEXEC = 0x80000;

    // poll(2)
    public const short POLLIN = 0x0001;
}
