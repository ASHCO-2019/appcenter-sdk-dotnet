﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Directory = Microsoft.AppCenter.Utils.Files.Directory;
using File = Microsoft.AppCenter.Utils.Files.File;

namespace Microsoft.AppCenter.Crashes.Utils
{
    public partial class ErrorLogHelper
    {
        internal Func<string, FileMode, Stream> NewFileStream { get; set; } = (name, mode) => new FileStream(name, FileMode.Create);

        internal Func<BinaryFormatter> NewBinaryFormatter { get; set; } = () => new BinaryFormatter();

        private void SaveExceptionFile(Directory directory, string fileName, Exception exception)
        {
            try
            {
                using (var fileStream = NewFileStream(Path.Combine(directory.FullName, fileName), FileMode.Create))
                {
                    var formatter = NewBinaryFormatter();
                    formatter.Serialize(fileStream, exception);
                }
            }
            catch (Exception e)
            {
                if (!exception.GetType().IsSerializable)
                {
                    // Note that this still saves an empty file which acts as a marker for error report life cycle. Same as Android SDK.
                    AppCenterLog.Warn(Crashes.LogTag, $"Cannot serialize {exception.GetType().FullName} exception for client side inspection. " +
                                                      "If you want to have access to the exception in the callbacks, please add a Serializable attribute " +
                                                      "and a deserialization constructor to the exception class.");
                }
                else
                {
                    AppCenterLog.Warn(Crashes.LogTag, "Failed to serialize exception for client side inspection.", e);
                }
            }
        }

        /// <summary>
        /// Reads an exception file from the given file.
        /// </summary>
        /// <param name="file">The file that contains exception.</param>
        /// <returns>An exception instance or null if the file doesn't contain an exception.</returns>
        public virtual Exception InstanceReadExceptionFile(File file)
        {
            try
            {
                using (var fileStream = NewFileStream(file.FullName, FileMode.Open))
                {
                    var formatter = NewBinaryFormatter();
                    return formatter.Deserialize(fileStream);
                }
            }
            catch (Exception e)
            {
                AppCenterLog.Warn(Crashes.LogTag, "Failed to deserialize exception for client side inspection.", e);
                return null;
            }
        }
    }
}
