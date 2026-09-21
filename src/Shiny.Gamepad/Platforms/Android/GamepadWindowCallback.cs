using Android.Views;
using Android.Views.Accessibility;

namespace Shiny.Gamepad;


/// <summary>
/// Wraps an activity's <see cref="Window.ICallback"/> so controller input can be seen on its way
/// into the view hierarchy.
/// </summary>
/// <remarks>
/// <para>Android delivers controller input as ordinary key and motion events dispatched to whatever
/// has focus. There is no listener to register and no service to run - a library that wants to see
/// that input either asks the app to forward every event by hand, or intercepts it. This
/// intercepts, so nothing is asked of the app.</para>
/// <para>The window callback is the right seam because it sits above the whole view hierarchy: the
/// events arrive before any view has decided whether to consume them, so a gamepad press is seen
/// even when a text field or a scroll view would otherwise have swallowed it.</para>
/// <para><b>Everything is forwarded to the original callback.</b> Observing is the default because
/// consuming would silently break the rest of the app - the D-pad would stop moving focus and B
/// would stop going back - and a library has no business deciding that. A game that wants the
/// input to itself sets <c>AndroidGamepadManager.ConsumeEvents</c>, which stops forwarding
/// controller events only; everything else still goes through untouched either way.</para>
/// <para>Every other member delegates verbatim. A null inner callback is possible in principle
/// between an activity being created and its window being attached, and is handled rather than
/// dereferenced.</para>
/// </remarks>
class GamepadWindowCallback(
    Window.ICallback? inner,
    Func<KeyEvent?, bool> onKey,
    Func<MotionEvent?, bool> onMotion
) : Java.Lang.Object, Window.ICallback
{
    /// <summary>The callback that was there before, so it can be put back on detach.</summary>
    public Window.ICallback? Inner => inner;


    public bool DispatchKeyEvent(KeyEvent? e)
    {
        var handled = onKey(e);
        var forwarded = inner?.DispatchKeyEvent(e) ?? false;

        return handled || forwarded;
    }


    public bool DispatchGenericMotionEvent(MotionEvent? e)
    {
        var handled = onMotion(e);
        var forwarded = inner?.DispatchGenericMotionEvent(e) ?? false;

        return handled || forwarded;
    }


    public bool DispatchKeyShortcutEvent(KeyEvent? e) => inner?.DispatchKeyShortcutEvent(e) ?? false;
    public bool DispatchPopulateAccessibilityEvent(AccessibilityEvent? e) => inner?.DispatchPopulateAccessibilityEvent(e) ?? false;
    public bool DispatchTouchEvent(MotionEvent? e) => inner?.DispatchTouchEvent(e) ?? false;
    public bool DispatchTrackballEvent(MotionEvent? e) => inner?.DispatchTrackballEvent(e) ?? false;

    public void OnActionModeFinished(ActionMode? mode) => inner?.OnActionModeFinished(mode);
    public void OnActionModeStarted(ActionMode? mode) => inner?.OnActionModeStarted(mode);
    public void OnAttachedToWindow() => inner?.OnAttachedToWindow();
    public void OnContentChanged() => inner?.OnContentChanged();
    public bool OnCreatePanelMenu(int featureId, IMenu menu) => inner?.OnCreatePanelMenu(featureId, menu) ?? false;
    public View? OnCreatePanelView(int featureId) => inner?.OnCreatePanelView(featureId);
    public void OnDetachedFromWindow() => inner?.OnDetachedFromWindow();
    public bool OnMenuItemSelected(int featureId, IMenuItem item) => inner?.OnMenuItemSelected(featureId, item) ?? false;
    public bool OnMenuOpened(int featureId, IMenu menu) => inner?.OnMenuOpened(featureId, menu) ?? false;
    public void OnPanelClosed(int featureId, IMenu menu) => inner?.OnPanelClosed(featureId, menu);
    public void OnPointerCaptureChanged(bool hasCapture) => inner?.OnPointerCaptureChanged(hasCapture);
    public bool OnPreparePanel(int featureId, View? view, IMenu menu) => inner?.OnPreparePanel(featureId, view, menu) ?? false;
    public void OnProvideKeyboardShortcuts(IList<KeyboardShortcutGroup>? data, IMenu? menu, int deviceId) => inner?.OnProvideKeyboardShortcuts(data, menu, deviceId);
    public bool OnSearchRequested() => inner?.OnSearchRequested() ?? false;
    public bool OnSearchRequested(SearchEvent? searchEvent) => inner?.OnSearchRequested(searchEvent) ?? false;
    public void OnWindowAttributesChanged(WindowManagerLayoutParams? attrs) => inner?.OnWindowAttributesChanged(attrs);
    public void OnWindowFocusChanged(bool hasFocus) => inner?.OnWindowFocusChanged(hasFocus);
    public ActionMode? OnWindowStartingActionMode(ActionMode.ICallback? callback) => inner?.OnWindowStartingActionMode(callback);
    public ActionMode? OnWindowStartingActionMode(ActionMode.ICallback? callback, ActionModeType type) => inner?.OnWindowStartingActionMode(callback, type);
}
