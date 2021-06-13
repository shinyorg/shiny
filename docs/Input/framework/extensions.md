Title: Extensions
Order: 6
---

TODO

```csharp
OpenAppSettingsIf(this IDialogs dialogs, Func<Task<AccessState>> accessRequest, string deniedMessage, string restrictedMessage)
ICommand ConfirmCommand(this IDialogs dialogs, Func<Task> action, string question, string title = "Confirm", string okText = "OK", string cancelText = "Cancel")
IObservable<string> ObserveTimeAgo(this DateTimeOffset dt, Func<TimeSpan, string> transformer, TimeSpan? intervalTime = null)

void WhenAnyValueSelected<TViewModel, TRet>(this TViewModel viewModel, Expression<Func<TViewModel, TRet>> expression, Action<TRet> action) where TViewModel : ViewModel
this.WhenAnyValueSelected(x => x.MySelectedItem, x => {});

SubOnMainThread
IDisposable ApplyMaxLengthConstraint<T>(this T npc, Expression<Func<T, string>> expression, int maxLength) where T : INotifyPropertyChanged
IDisposable ApplyValueRangeConstraint<T>(this T npc, Expression<Func<T, int>> expression, int min, int max) where T : INotifyPropertyChanged
```