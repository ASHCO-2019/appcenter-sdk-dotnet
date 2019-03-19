// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Contoso.Forms.Test.UITests
{
    public static class LastSessionErrorReportHelper
    {
        public static IApp app;

        public static bool DeviceReported
        {
            get
            {
                return WaitForLabelToSay(TestStrings.DeviceLabel,
                                         TestStrings.DeviceReportedText);
            }
        }

        public static bool HasIosDetails
        {
            get
            {
                return WaitForLabelToSay(TestStrings.iOSDetailsLabel,
                                         TestStrings.HasiOSDetailsText);
            }
        }

        public static bool HasAndroidDetails
        {
            get
            {
                return WaitForLabelToSay(TestStrings.AndroidDetailsLabel,
                                         TestStrings.HasAndroidDetailsText);
            }
        }

        public static bool HasAppStartTime
        {
            get
            {
                return WaitForLabelToSayAnything(TestStrings.AppStartTimeLabel);
            }
        }

        public static bool HasAppErrorTime
        {
            get
            {
                return WaitForLabelToSayAnything(TestStrings.AppErrorTimeLabel);
            }
        }

        public static bool HasId
        {
            get
            {
                return WaitForLabelToSayAnything(TestStrings.IdLabel);
            }
        }

        public static bool VerifyExceptionType(string expectedType)
        {
            return WaitForLabelToSay(TestStrings.ExceptionTypeLabel, expectedType);
        }

        public static bool VerifyExceptionMessage(string expectedMessage)
        {
            return WaitForLabelToSay(TestStrings.ExceptionMessageLabel, expectedMessage);
        }

        static bool WaitForLabelToSay(string labelName, string text)
        {
            try
            {
                app.WaitFor(() =>
                {
                    AppResult[] results = app.Query(labelName);
                    if (results.Length < 1)
                        return false;
                    AppResult label = results[0];
                    return label.Text == text;
                }, timeout: TimeSpan.FromSeconds(5));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        static bool WaitForLabelToSayAnything(string labelName)
        {
            try
            {
                app.WaitFor(() =>
                {
                    AppResult[] results = app.Query(labelName);
                    if (results.Length < 1)
                        return false;
                    AppResult label = results[0];
                    return label.Text != "";
                }, timeout: TimeSpan.FromSeconds(5));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
