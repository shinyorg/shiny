using Android.Hardware.Input;

namespace Shiny.Gamepad;


/// <summary>
/// Forwards <see cref="InputManager.IInputDeviceListener"/> callbacks to the manager.
/// </summary>
/// <remarks>
/// A separate type because implementing a Java interface requires deriving from
/// <see cref="Java.Lang.Object"/>, and the manager's own base class is shared with the platforms
/// that have no JNI. A thin forwarder costs one object and keeps the abstraction intact.
/// </remarks>
class AndroidInputDeviceListener(Action<int> added, Action<int> removed, Action<int> changed)
    : Java.Lang.Object, InputManager.IInputDeviceListener
{
    public void OnInputDeviceAdded(int deviceId) => added(deviceId);
    public void OnInputDeviceRemoved(int deviceId) => removed(deviceId);
    public void OnInputDeviceChanged(int deviceId) => changed(deviceId);
}
