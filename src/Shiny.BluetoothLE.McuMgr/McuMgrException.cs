namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// The SMP management error codes (<c>MGMT_ERR_*</c>) returned in a response's <c>rc</c>.
/// </summary>
public enum McuMgrError
{
    /// <summary>Success.</summary>
    Ok = 0,
    /// <summary>An unknown error occurred.</summary>
    Unknown = 1,
    /// <summary>The device ran out of memory.</summary>
    NoMemory = 2,
    /// <summary>The request contained an invalid value.</summary>
    InvalidValue = 3,
    /// <summary>The operation timed out on the device.</summary>
    Timeout = 4,
    /// <summary>The requested entry (file, image, stat group...) does not exist.</summary>
    NoEntry = 5,
    /// <summary>The device is in a state that does not allow the operation.</summary>
    BadState = 6,
    /// <summary>The response would not fit in the device's buffer.</summary>
    MessageTooLarge = 7,
    /// <summary>The command or group is not supported (not compiled into the firmware).</summary>
    NotSupported = 8,
    /// <summary>Data is corrupt.</summary>
    Corrupt = 9,
    /// <summary>The device is busy with another operation.</summary>
    Busy = 10,
    /// <summary>Access was denied.</summary>
    AccessDenied = 11,
    /// <summary>The request's protocol version is too old for the device.</summary>
    UnsupportedTooOld = 12,
    /// <summary>The request's protocol version is too new for the device.</summary>
    UnsupportedTooNew = 13,
    /// <summary>The start of the user-defined error range.</summary>
    PerUser = 256
}


/// <summary>
/// Thrown when a device answers an SMP request with an error.
/// </summary>
public class McuMgrException : Exception
{
    /// <summary>
    /// Creates an exception for a management error (<c>rc</c>).
    /// </summary>
    public McuMgrException(McuMgrError error, ushort group, byte command)
        : base($"MCUmgr request (group {group}, command {command}) failed: {error} ({(int)error})")
    {
        this.Error = error;
        this.Group = group;
        this.Command = command;
    }


    /// <summary>
    /// Creates an exception for an SMP v2 group error (<c>err.group</c> + <c>err.rc</c>).
    /// </summary>
    public McuMgrException(ushort group, byte command, int groupErrorCode)
        : base($"MCUmgr request (group {group}, command {command}) failed with group error {groupErrorCode}")
    {
        this.Group = group;
        this.Command = command;
        this.GroupErrorCode = groupErrorCode;
    }


    /// <summary>
    /// Creates an exception for a protocol problem with no device error code.
    /// </summary>
    public McuMgrException(string message, Exception? inner = null) : base(message, inner) { }


    /// <summary>The management error, when the device returned one (SMP v1 or a v2 transport-level error).</summary>
    public McuMgrError? Error { get; }

    /// <summary>The group of the failing request.</summary>
    public ushort? Group { get; }

    /// <summary>The command of the failing request.</summary>
    public byte? Command { get; }

    /// <summary>The group-specific error code from an SMP v2 <c>err</c> map. Its meaning depends on <see cref="Group"/>.</summary>
    public int? GroupErrorCode { get; }
}
