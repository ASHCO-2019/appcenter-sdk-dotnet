﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AppCenter.Channel;
using Microsoft.AppCenter.Crashes.Ingestion.Models;
using Microsoft.AppCenter.Crashes.Utils;
using Microsoft.AppCenter.Ingestion.Models;
using Microsoft.AppCenter.Utils;
using Microsoft.AppCenter.Utils.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.AppCenter.Crashes.Test.Windows
{

    /// <summary>
    /// This class tests the error attachments functionality of the Crashes class.
    /// </summary>
    [TestClass]
    public class CrashesErrorAttachmentTest
    {
        private Mock<IChannelGroup> _mockChannelGroup;
        private Mock<IChannelUnit> _mockChannel;
        private Mock<IApplicationLifecycleHelper> _mockApplicationLifecycleHelper;

        [TestInitialize]
        public void InitializeCrashTest()
        {
            Crashes.Instance = new Crashes();
            _mockChannelGroup = new Mock<IChannelGroup>();
            _mockChannel = new Mock<IChannelUnit>();
            _mockApplicationLifecycleHelper = new Mock<IApplicationLifecycleHelper>();
            _mockChannelGroup.Setup(group => group.AddChannel(It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<TimeSpan>(), It.IsAny<int>()))
                .Returns(_mockChannel.Object);
            ApplicationLifecycleHelper.Instance = _mockApplicationLifecycleHelper.Object;
        }

        [TestCleanup]
        public void Cleanup()
        {
            // If a mock was set, reset it to null before moving on.
            ErrorLogHelper.Instance = null;
            ApplicationLifecycleHelper.Instance = null;
            Crashes.Instance = null;
        }

        [TestMethod]
        public void ProcessPendingCrashesSendsErrorAttachment()
        {
            var mockFile1 = Mock.Of<File>();
            var mockFile2 = Mock.Of<File>();
            var mockErrorLogHelper = Mock.Of<ErrorLogHelper>();
            ErrorLogHelper.Instance = mockErrorLogHelper;
            var expectedManagedErrorLog1 = new ManagedErrorLog {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                AppLaunchTimestamp = DateTime.Now,
                Device = new Microsoft.AppCenter.Ingestion.Models.Device()
            };
            var expectedManagedErrorLog2 = new ManagedErrorLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                AppLaunchTimestamp = DateTime.Now,
                Device = new Microsoft.AppCenter.Ingestion.Models.Device()
            };

            // Mock error attachment log so that Validate method does not throw.
            var mockErrorAttachment = Mock.Of<ErrorAttachmentLog>();

            // Stub get/read/delete error files.
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceGetErrorLogFiles()).Returns(new List<File> { mockFile1, mockFile2 });
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceReadErrorLogFile(mockFile1)).Returns(expectedManagedErrorLog1);
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceReadErrorLogFile(mockFile2)).Returns(expectedManagedErrorLog2);

            // Implement attachments callback.
            Crashes.GetErrorAttachments = errorReport =>
            {
                if (errorReport.Id == expectedManagedErrorLog1.Id.ToString())
                {
                    return new List<ErrorAttachmentLog> { mockErrorAttachment };
                }
                return null;
            };

            Crashes.SetEnabledAsync(true).Wait();
            Crashes.Instance.OnChannelGroupReady(_mockChannelGroup.Object, string.Empty);
            Crashes.Instance.ProcessPendingErrorsTask.Wait();

            // Verify attachment log has been queued to the channel. (And only one attachment log).
            _mockChannel.Verify(channel => channel.EnqueueAsync(mockErrorAttachment), Times.Once());
            _mockChannel.Verify(channel => channel.EnqueueAsync(It.IsAny<ErrorAttachmentLog>()), Times.Once());

            // Verify that the attachment has been modified with the right fields.
            Mock.Get(mockErrorAttachment).VerifySet(attachment => attachment.ErrorId = expectedManagedErrorLog1.Id);
            Mock.Get(mockErrorAttachment).VerifySet(attachment => attachment.Id = It.IsAny<Guid>());
        }

        [TestMethod]
        public void ProcessPendingCrashesIgnoresNullErrorAttachment()
        {
            var mockFile1 = Mock.Of<File>();
            var mockFile2 = Mock.Of<File>();
            var mockErrorLogHelper = Mock.Of<ErrorLogHelper>();
            ErrorLogHelper.Instance = mockErrorLogHelper;
            var expectedManagedErrorLog1 = new ManagedErrorLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                AppLaunchTimestamp = DateTime.Now,
                Device = new Microsoft.AppCenter.Ingestion.Models.Device()
            };

            // Stub get/read/delete error files.
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceGetErrorLogFiles()).Returns(new List<File> { mockFile1 });
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceReadErrorLogFile(mockFile1)).Returns(expectedManagedErrorLog1);

            // Implement attachments callback.
            Crashes.GetErrorAttachments = errorReport =>
            {
                if (errorReport.Id == expectedManagedErrorLog1.Id.ToString())
                {
                    return new List<ErrorAttachmentLog> { null };
                }
                return null;
            };

            Crashes.SetEnabledAsync(true).Wait();
            Crashes.Instance.OnChannelGroupReady(_mockChannelGroup.Object, string.Empty);
            Crashes.Instance.ProcessPendingErrorsTask.Wait();

            // Verify nothing has been enqueued.
            _mockChannel.Verify(channel => channel.EnqueueAsync(It.IsAny<ErrorAttachmentLog>()), Times.Never());
        }

        [TestMethod]
        public void ProcessPendingCrashesIgnoresInvalidErrorAttachmentWithoutCrashing()
        {
            var mockFile1 = Mock.Of<File>();
            var mockFile2 = Mock.Of<File>();
            var mockErrorLogHelper = Mock.Of<ErrorLogHelper>();
            ErrorLogHelper.Instance = mockErrorLogHelper;
            var expectedManagedErrorLog1 = new ManagedErrorLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                AppLaunchTimestamp = DateTime.Now,
                Device = new Microsoft.AppCenter.Ingestion.Models.Device()
            };
            var expectedManagedErrorLog2 = new ManagedErrorLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                AppLaunchTimestamp = DateTime.Now,
                Device = new Microsoft.AppCenter.Ingestion.Models.Device()
            };

            // Stub get/read/delete error files.
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceGetErrorLogFiles()).Returns(new List<File> { mockFile1, mockFile2 });
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceReadErrorLogFile(mockFile1)).Returns(expectedManagedErrorLog1);
            Mock.Get(ErrorLogHelper.Instance).Setup(instance => instance.InstanceReadErrorLogFile(mockFile2)).Returns(expectedManagedErrorLog2);

            // Create two valid and one invalid attachment.
            var mockInvalidErrorAttachment1 = Mock.Of<ErrorAttachmentLog>();
            Mock.Get(mockInvalidErrorAttachment1).Setup(attachment => attachment.Validate()).Throws(new ValidationException());
            var mockValidErrorAttachment1 = Mock.Of<ErrorAttachmentLog>();
            var mockValidErrorAttachment2 = Mock.Of<ErrorAttachmentLog>();

            // Implement attachments callback.
            Crashes.GetErrorAttachments = errorReport =>
            {
                if (errorReport.Id == expectedManagedErrorLog1.Id.ToString())
                {
                    return new List<ErrorAttachmentLog> { mockInvalidErrorAttachment1, mockValidErrorAttachment1 };
                }
                return new List<ErrorAttachmentLog> { mockValidErrorAttachment2 };
            };

            Crashes.SetEnabledAsync(true).Wait();
            Crashes.Instance.OnChannelGroupReady(_mockChannelGroup.Object, string.Empty);
            Crashes.Instance.ProcessPendingErrorsTask.Wait();

            // Verify all valid attachment logs has been queued to the channel, but not invalid one.
            _mockChannel.Verify(channel => channel.EnqueueAsync(mockInvalidErrorAttachment1), Times.Never());
            _mockChannel.Verify(channel => channel.EnqueueAsync(mockValidErrorAttachment1), Times.Once());
            _mockChannel.Verify(channel => channel.EnqueueAsync(mockValidErrorAttachment2), Times.Once());
        }
    }
}
