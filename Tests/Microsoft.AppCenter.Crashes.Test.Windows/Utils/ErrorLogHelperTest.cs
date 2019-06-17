﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AppCenter.Crashes.Ingestion.Models;
using Microsoft.AppCenter.Crashes.Utils;
using Microsoft.AppCenter.Ingestion.Models.Serialization;
using Microsoft.AppCenter.Utils;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.AppCenter.Crashes.Test.Windows.Utils
{
    [TestClass]
    public class ErrorLogHelperTest
    {
        [TestInitialize]
        public void SetUp()
        {
            ErrorLogHelper.ProcessInformation = Mock.Of<IProcessInformation>();
            ErrorLogHelper.DeviceInformationHelper = Mock.Of<IDeviceInformationHelper>();
            ErrorLogHelper.FileHelper = Mock.Of<FileHelper>();
        }

        [TestMethod]
        public void CreateErrorLog()
        {
            // Set up an exception. This is needed because inner exceptions cannot be mocked.
            System.Exception exception;
            try
            {
                throw new AggregateException("mainException", new System.Exception("innerException1"), new System.Exception("innerException2", new System.Exception("veryInnerException")));
            }
            catch (System.Exception e)
            {
                exception = e;
            }

            // Mock device information.
            var device = new Microsoft.AppCenter.Ingestion.Models.Device("sdkName", "sdkVersion", "osName", "osVersion", "locale", 1,
                "appVersion", "appBuild", null, null, "model", "oemName", "osBuild", null, "screenSize", null, null, "appNamespace", null, null, null, null);
            Mock.Get(ErrorLogHelper.DeviceInformationHelper).Setup(instance => instance.GetDeviceInformationAsync()).Returns(Task.FromResult(device));

            // Mock process information.
            var parentProcessId = 0;
            var parentProcessName = "parentProcessName";
            var processArchitecture = "processArchitecture";
            var processId = 1;
            var processName = "processName";
            var processStartTime = DateTime.Now;
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ParentProcessId).Returns(parentProcessId);
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ParentProcessName).Returns(parentProcessName);
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ProcessArchitecture).Returns(processArchitecture);
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ProcessId).Returns(processId);
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ProcessName).Returns(processName);
            Mock.Get(ErrorLogHelper.ProcessInformation).SetupGet(instance => instance.ProcessStartTime).Returns(processStartTime);

            // Create the error log.
            var log = ErrorLogHelper.CreateErrorLogAsync(exception).Result;

            // Validate the result.
            Assert.AreEqual(exception.StackTrace, log.Exception.StackTrace);
            Assert.AreEqual(exception.Message, log.Exception.Message);
            Assert.AreEqual(3, log.Exception.InnerExceptions.Count, 3);
            Assert.AreEqual((exception as AggregateException).InnerExceptions[0].Message, log.Exception.InnerExceptions[0].Message);
            Assert.AreEqual((exception as AggregateException).InnerExceptions[1].Message, log.Exception.InnerExceptions[1].Message);
            Assert.AreEqual((exception as AggregateException).InnerExceptions[1].InnerException.Message, log.Exception.InnerExceptions[1].InnerExceptions[0].Message);
            Assert.AreEqual(device.SdkName, log.Device.SdkName);
            Assert.AreEqual(device.SdkVersion, log.Device.SdkVersion);
            Assert.AreEqual(device.OsName, log.Device.OsName);
            Assert.AreEqual(device.OsVersion, log.Device.OsVersion);
            Assert.AreEqual(device.Locale, log.Device.Locale);
            Assert.AreEqual(device.TimeZoneOffset, log.Device.TimeZoneOffset);
            Assert.AreEqual(device.AppVersion, log.Device.AppVersion);
            Assert.AreEqual(device.AppBuild, log.Device.AppBuild);
            Assert.AreEqual(device.WrapperSdkVersion, log.Device.WrapperSdkVersion);
            Assert.AreEqual(device.WrapperSdkName, log.Device.WrapperSdkName);
            Assert.AreEqual(device.Model, log.Device.Model);
            Assert.AreEqual(device.OemName, log.Device.OemName);
            Assert.AreEqual(device.OsBuild, log.Device.OsBuild);
            Assert.AreEqual(device.OsApiLevel, log.Device.OsApiLevel);
            Assert.AreEqual(device.ScreenSize, log.Device.ScreenSize);
            Assert.AreEqual(device.CarrierName, log.Device.CarrierName);
            Assert.AreEqual(device.CarrierCountry, log.Device.CarrierCountry);
            Assert.AreEqual(device.AppNamespace, log.Device.AppNamespace);
            Assert.AreEqual(device.LiveUpdateDeploymentKey, log.Device.LiveUpdateDeploymentKey);
            Assert.AreEqual(device.LiveUpdatePackageHash, log.Device.LiveUpdatePackageHash);
            Assert.AreEqual(device.LiveUpdateReleaseLabel, log.Device.LiveUpdateReleaseLabel);
            Assert.AreEqual(device.WrapperRuntimeVersion, log.Device.WrapperRuntimeVersion);
            Assert.AreEqual(parentProcessId, log.ParentProcessId);
            Assert.AreEqual(parentProcessName, log.ParentProcessName);
            Assert.AreEqual(processArchitecture, log.Architecture);
            Assert.AreEqual(processId, log.ProcessId);
            Assert.AreEqual(processName, log.ProcessName);
            Assert.AreEqual(processStartTime, log.AppLaunchTimestamp);
            Assert.IsTrue(log.Fatal);
        }

        [TestMethod]
        public void GetSingleErrorLogFile()
        {
            var id = Guid.NewGuid();
            var expectedFileInfo = new FileInfo("file");
            var fileInfoList = new List<FileInfo> { expectedFileInfo };
            Mock.Get(ErrorLogHelper.FileHelper).Setup(instance => instance.EnumerateFiles($"{id}.json")).Returns(fileInfoList);

            // Retrieve the error log by the ID.
            var errorLogFileInfo = ErrorLogHelper.GetStoredErrorLogFile(id);

            // Validate the contents.
            Assert.AreSame(expectedFileInfo, errorLogFileInfo);
        }

        [TestMethod]
        public void GetErrorLogFiles()
        {
            // Mock multiple error log files.
            var expectedFileInfo1 = new FileInfo("file");
            var expectedFileInfo2 = new FileInfo("file2");
            var fileInfoList = new List<FileInfo> { expectedFileInfo1, expectedFileInfo2 };
            Mock.Get(ErrorLogHelper.FileHelper).Setup(instance => instance.EnumerateFiles("*.json")).Returns(fileInfoList);

            // Retrieve the error logs.
            var errorLogFileInfos = ErrorLogHelper.GetErrorLogFiles().ToList();

            // Validate the contents.
            Assert.AreEqual(fileInfoList.Count, errorLogFileInfos.Count);
            foreach (var fileInfo in errorLogFileInfos)
            {
                Assert.IsNotNull(fileInfo);
                CollectionAssert.Contains(fileInfoList, fileInfo);
                fileInfoList.Remove(fileInfo);
            }
        }

        [TestMethod]
        public void GetLastErrorLogFile()
        {
            using (ShimsContext.Create())
            {

                // Mock multiple error log files.
                var oldFileInfo = new System.IO.Fakes.ShimFileInfo();
                var oldFileSystemInfo = new System.IO.Fakes.ShimFileSystemInfo(oldFileInfo)
                {
                    LastWriteTimeGet = () => DateTime.Now.AddDays(-200)
                };
                var recentFileInfo = new System.IO.Fakes.ShimFileInfo();
                var recentFileSystemInfo = new System.IO.Fakes.ShimFileSystemInfo(oldFileInfo)
                {
                    LastWriteTimeGet = () => DateTime.Now
                };
                var fileInfoList = new List<FileInfo> { oldFileInfo, recentFileInfo };
                Mock.Get(ErrorLogHelper.FileHelper).Setup(instance => instance.EnumerateFiles("*.json")).Returns(fileInfoList);

                // Retrieve the error logs.
                var errorLogFileInfo = ErrorLogHelper.GetLastErrorLogFile();

                // Validate the contents.
                Assert.AreSame(recentFileInfo.Instance, errorLogFileInfo);
            }
        }

        [TestMethod]
        public void SaveErrorLogFile()
        {
            var errorLog = new ManagedErrorLog
            {
                Id = Guid.NewGuid(),
                ProcessId = 123
            };
            var fileName = errorLog.Id + ".json";
            var serializedErrorLog = LogSerializer.Serialize(errorLog);
            ErrorLogHelper.SaveErrorLogFile(errorLog);
            Mock.Get(ErrorLogHelper.FileHelper).Verify(instance => instance.CreateFile(fileName, serializedErrorLog), Times.Once);
        }

        [TestMethod]
        public void RemoveStoredErrorLogFile()
        {
            using (ShimsContext.Create())
            {
                var fileInfo = new System.IO.Fakes.ShimFileInfo();
                var count = 0;
                fileInfo.Delete = () => { count++; };
                var fileInfoList = new List<FileInfo> { fileInfo };
                var id = Guid.NewGuid();
                Mock.Get(ErrorLogHelper.FileHelper).Setup(instance => instance.EnumerateFiles($"{id}.json")).Returns(fileInfoList);
                ErrorLogHelper.RemoveStoredErrorLogFile(id);
                Assert.AreEqual(1, count);
            }
        }
    }
}
