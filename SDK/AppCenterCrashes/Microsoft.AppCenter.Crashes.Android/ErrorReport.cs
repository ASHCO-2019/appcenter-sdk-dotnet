// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.AppCenter.Crashes
{
    using AndroidExceptionDataManager = Com.Microsoft.Appcenter.Crashes.WrapperSdkExceptionManager;
    using AndroidErrorReport = Com.Microsoft.Appcenter.Crashes.Model.AndroidErrorReport;

    public partial class ErrorReport
    {
        internal ErrorReport(AndroidErrorReport androidReport)
        {
            Id = androidReport.Id;
            AppStartTime = DateTimeOffset.FromUnixTimeMilliseconds(androidReport.AppStartTime.Time);
            AppErrorTime = DateTimeOffset.FromUnixTimeMilliseconds(androidReport.AppErrorTime.Time);
            Device = androidReport.Device == null ? null : new Device(androidReport.Device);
            object androidThrowable;
            try
            {
                androidThrowable = androidReport.Throwable;
            }
            catch (Exception e)
            {
                AppCenterLog.Debug(Crashes.LogTag, "Cannot read throwable from java point of view, probably a .NET exception", e);
                androidThrowable = null;
            }
            AndroidDetails = new AndroidErrorDetails(androidThrowable, androidReport.ThreadName);
            iOSDetails = null;
            byte[] exceptionBytes = AndroidExceptionDataManager.LoadWrapperExceptionData(Java.Util.UUID.FromString(Id));
            if (exceptionBytes != null)
            {
                Exception = CrashesUtils.DeserializeException(exceptionBytes);
            }
        }
    }
}
