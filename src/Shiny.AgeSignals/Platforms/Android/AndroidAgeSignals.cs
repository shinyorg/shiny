using Com.Google.Android.Play.Agesignals;
using Shiny.AgeSignals.Infrastructure;
using GmsTask = Android.Gms.Tasks.Task;

namespace Shiny.AgeSignals;


/// <summary>
/// Google Play Age Signals 0.0.4: <c>requestAgeSignalsAccess</c> (which may show Play's sharing prompt) returns
/// SHARED / NOT_SHARED / VERIFICATION_REQUIRED, and only SHARED goes on to <c>checkAgeSignals</c>.
/// </summary>
public class AndroidAgeSignals(AndroidPlatform platform) : IAgeSignals
{
    // Play has no eligibility API - whether the law applies is decided inside the Play Store
    public Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default)
        => Task.FromResult(new AgeSignalsEligibility(true));


    public async Task<AgeSignal> RequestAgeRange(int gate1, int? gate2 = null, int? gate3 = null, CancellationToken cancellationToken = default)
    {
        // the gates are Apple's - Play's bands come from Play Console - but bad input is still bad input
        AgeSignalMapper.ValidateGates(gate1, gate2, gate3);

        var activity = platform.CurrentActivity
            ?? throw new AgeSignalsException(AgeSignalsErrorReason.NoUserInterface, "There is no current activity for Play to show its age sharing prompt from");

        var manager = AgeSignalsManagerFactory.Create(platform.AppContext);

        var accessRequest = AgeSignalsAccessRequest.InvokeBuilder().SetActivity(activity).Build();
        var access = await Await<AgeSignalsAccessResult>(manager.RequestAgeSignalsAccess(accessRequest), cancellationToken).ConfigureAwait(false);

        var status = access.AgeSignalsStatus()?.IntValue();
        if (status != AgeSignalMapper.PlayStatusShared)
            return AgeSignalMapper.FromPlayAccessStatus(status);

        var result = await Await<AgeSignalsResult>(
            manager.CheckAgeSignals(AgeSignalsRequest.InvokeBuilder().Build()),
            cancellationToken
        ).ConfigureAwait(false);

        var approvalDate = result.SignificantChangeApprovalDate();
        return AgeSignalMapper.FromPlayResult(
            result.AgeLower()?.IntValue(),
            result.AgeUpper()?.IntValue(),
            result.AgeRangeSource()?.IntValue(),
            result.SignificantChangeStatus()?.IntValue(),
            approvalDate == null ? null : DateTimeOffset.FromUnixTimeMilliseconds(approvalDate.Time),
            result.InstallId()
        );
    }


    // significant changes are declared in Play Console; the guardian's answer comes back on AgeSignal.SignificantChange
    public Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default)
        => Task.FromResult(false);


    // Play surfaces results through GMS Task completion listeners rather than awaitables
    static async Task<T> Await<T>(GmsTask task, CancellationToken cancellationToken) where T : Java.Lang.Object
    {
        var tcs = new TaskCompletionSource<GmsTask>(TaskCreationOptions.RunContinuationsAsynchronously);
        task.AddOnCompleteListener(new CompleteListener(tcs));

        // Play calls can't be cancelled - this only stops waiting
        await using var _ = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)).ConfigureAwait(false);
        var completed = await tcs.Task.ConfigureAwait(false);

        if (completed.IsSuccessful)
            return (T)completed.Result!;

        var ex = completed.Exception;
        throw ex switch
        {
            Com.Google.Android.Play.Agesignals.AgeSignalsException play => AgeSignalMapper.FromPlayError(play.ErrorCode, play.Message, play),
            Android.Gms.Common.Apis.ApiException api => AgeSignalMapper.FromPlayError(api.StatusCode, api.Message, api),
            _ => new AgeSignalsException(AgeSignalsErrorReason.Unknown, ex?.Message ?? "Play Age Signals failed", innerException: ex)
        };
    }


    sealed class CompleteListener(TaskCompletionSource<GmsTask> tcs) : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(GmsTask task) => tcs.TrySetResult(task);
    }
}
