namespace Shiny.BluetoothLE.AccessorySetup.Infrastructure;


/// <summary>
/// Turns AccessorySetupKit's event stream into one awaited result. <c>showPicker</c>'s completion handler only says
/// whether the sheet appeared; what the user did arrives later as session events - <c>accessoryAdded</c> (and
/// <c>pickerSetupFailed</c>) while the sheet is up, then <c>pickerDidDismiss</c> when it goes away. Dismissal is the
/// only event every outcome ends with, so that is what completes the task: with the added accessory, with null if
/// the user backed out or nothing was found, or with the setup failure.
/// </summary>
sealed class PickerFlow
{
    readonly TaskCompletionSource<SetupAccessory?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    SetupAccessory? added;
    Exception? setupFailure;


    public Task<SetupAccessory?> Task => this.tcs.Task;
    public bool IsCompleted => this.tcs.Task.IsCompleted;


    /// <summary>The <c>showPicker</c> completion handler: null error means the sheet is up.</summary>
    public void OnShowCompleted(Exception? error)
    {
        if (error != null)
            this.tcs.TrySetException(error);
    }


    /// <summary>The picker was dismissed or the user cancelled before it appeared - a null result, not an error.</summary>
    public void OnCancelled() => this.tcs.TrySetResult(null);


    public void OnAccessoryAdded(SetupAccessory accessory) => this.added = accessory;


    public void OnSetupFailed(Exception error) => this.setupFailure = error;


    public void OnDismissed()
    {
        // an accessory that made it through setup wins over a failure the sheet reported and recovered from
        if (this.added != null)
            this.tcs.TrySetResult(this.added);
        else if (this.setupFailure != null)
            this.tcs.TrySetException(this.setupFailure);
        else
            this.tcs.TrySetResult(null);
    }


    /// <summary>The session went away (invalidated) with the picker still up.</summary>
    public void OnInvalidated() => this.tcs.TrySetException(new AccessorySetupException(
        AccessorySetupErrorCode.Failed,
        "The accessory session was invalidated while the picker was showing"
    ));


    public void OnCancelToken(CancellationToken token) => this.tcs.TrySetCanceled(token);
}
