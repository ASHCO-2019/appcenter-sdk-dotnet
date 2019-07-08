// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Contoso.WPF.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppCenter.LogLevel = LogLevel.Verbose;
            Crashes.ShouldAwaitUserConfirmation = ConfirmationHandler;
            Crashes.ShouldProcessErrorReport = ShouldProcess;

            // Event handlers.
            Crashes.SendingErrorReport += SendingErrorReportHandler;
            Crashes.SentErrorReport += SentErrorReportHandler;
            Crashes.FailedToSendErrorReport += FailedToSendErrorReportHandler;
            AppCenter.Start("f4e2a83d-3052-4884-8176-8b2c50277d16", typeof(Analytics), typeof(Crashes));
            Crashes.HasCrashedInLastSessionAsync().ContinueWith(hasCrashed =>
            {
                Log("Crashes.HasCrashedInLastSession=" + hasCrashed.Result);
            });
            Crashes.GetLastSessionCrashReportAsync().ContinueWith(task =>
            {
                Log("Crashes.LastSessionCrashReport.Exception=" + task.Result?.Exception);
            });
        }

        private static bool ShouldProcess(ErrorReport report)
        {
            Log("Determining whether to process error report");
            return true;
        }

        private static bool ConfirmationHandler()
        {
            Current.Dispatcher.Invoke(() =>
            {
                var dialog = new UserConfirmationDialog();
                if (dialog.ShowDialog() ?? false)
                {
                    Crashes.NotifyUserConfirmation(dialog.ClickResult);
                }
            });
            return true;
        }

        private static void SendingErrorReportHandler(object sender, SendingErrorReportEventArgs e)
        {
            Log("Sending error report");
            var report = e.Report;
            if (report.Exception != null)
            {
                Log($"ErrorId: {report.Id}");
            }
        }

        static void SentErrorReportHandler(object sender, SentErrorReportEventArgs e)
        {
            Log("Sent error report");
            var report = e.Report;
            if (report.Exception != null)
            {
                Log($"ErrorId: {report.Id}");
            }
            else
            {
                Log("No system exception was found");
            }
        }

        private static void FailedToSendErrorReportHandler(object sender, FailedToSendErrorReportEventArgs e)
        {
            Log("Failed to send error report");
            var report = e.Report;
            if (report.Exception != null)
            {
                Log($"ErrorId: {report.Id}");
            }
            if (e.Exception != null)
            {
                Log("There is an exception associated with the failure");
            }
        }

        private static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            System.Diagnostics.Debug.WriteLine($"{timestamp} [AppCenterDemo] Info: {message}");
        }
    }
}
