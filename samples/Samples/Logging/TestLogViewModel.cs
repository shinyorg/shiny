using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Samples.Infrastructure;


namespace Samples.Logging
{
    public class TestLogViewModel : ViewModel
    {
        public TestLogViewModel(IDialogs dialogs, ILogger<TestLogViewModel> logger)
        {
            this.Test = ReactiveCommand.Create<string>(args =>
            {
                switch (args)
                {
                    case "critical":
                        logger.LogCritical("This is a critical test");
                        break;

                    case "error":
                        logger.LogError("This is an error test");
                        break;

                    case "warning":
                        logger.LogWarning("This is an error test");
                        break;

                    case "info":
                        logger.LogInformation("This is an info test");
                        break;

                    case "debug":
                        logger.LogDebug("This is a debug test");
                        break;
                }
                dialogs.Snackbar("Sent log");
            });
        }


        public ICommand Test { get; }
    }
}
