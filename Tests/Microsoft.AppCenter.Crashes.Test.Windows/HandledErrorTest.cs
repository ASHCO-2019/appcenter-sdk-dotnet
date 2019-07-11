﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AppCenter.Channel;
using Microsoft.AppCenter.Crashes.Ingestion.Models;
using Microsoft.AppCenter.Ingestion.Http;
using Microsoft.AppCenter.Ingestion.Models;
using Microsoft.AppCenter.Ingestion.Models.Serialization;
using Microsoft.AppCenter.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.AppCenter.Crashes.Test.Windows
{
    [TestClass]
    public class HandledErrorTest
    {
        private static IHttpNetworkAdapter StartCrashes()
        {
            var mockHttpNetworkAdapter = Mock.Of<IHttpNetworkAdapter>();
            var storage = new Storage.Storage(new StorageAdapter("test.db"));
            var ingestion = new IngestionHttp(mockHttpNetworkAdapter);
            var channelGroup = new ChannelGroup(ingestion, storage, "app secret");
            Crashes.Instance = new Crashes();
            Crashes.SetEnabledAsync(true).Wait();
            Crashes.Instance.OnChannelGroupReady(channelGroup, "app secret");
            return mockHttpNetworkAdapter;
        }

        [TestMethod]
        public void TrackErrorWithValidParameters()
        {
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    semaphore.Release();
                });
            var exception = new System.Exception("Something went wrong.");
            var properties = new Dictionary<string, string> { { "k1", "v1" }, { "p2", "v2" } };
            Crashes.TrackError(exception, properties);

            // Wait until the http layer sends the log.
            semaphore.Wait(1000);
            Assert.IsNotNull(actualLog);
            Assert.AreEqual(exception.Message, actualLog.Exception.Message);
            CollectionAssert.AreEquivalent(properties, actualLog.Properties as Dictionary<string, string>);
        }

        [TestMethod]
        public void TrackErrorWithTooManyProperties()
        {
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    semaphore.Release();
                });
            var exception = new System.Exception("Something went wrong.");
            var maxProperties = 20;
            var properties = new Dictionary<string, string>();
            for (int i = 0; i < maxProperties + 5; ++i)
            {
                properties[i.ToString()] = i.ToString();
            }
            Crashes.TrackError(exception, properties);

            // Wait until the http layer sends the log.
            semaphore.Wait(1000);
            Assert.IsNotNull(actualLog);
            Assert.AreEqual(exception.Message, actualLog.Exception.Message);
            CollectionAssert.IsSubsetOf(actualLog.Properties as Dictionary<string, string>, properties);
            Assert.AreEqual(maxProperties, actualLog.Properties.Count);
        }

        [TestMethod]
        public void TrackErrorWithoutProperties()
        {
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    semaphore.Release();
                });
            var exception = new System.Exception("Something went wrong.");
            Crashes.TrackError(exception);

            // Wait until the http layer sends the log.
            semaphore.Wait(1000);
            Assert.IsNotNull(actualLog);
            Assert.AreEqual(exception.Message, actualLog.Exception.Message);
            Assert.IsNull(actualLog.Properties);
        }

        [TestMethod]
        public void TrackErrorWithoutException()
        {
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    semaphore.Release();
                });
            Crashes.TrackError(null);

            // Check no log sent.
            semaphore.Wait(1000);
            Assert.IsNull(actualLog);
        }

        [TestMethod]
        public void TrackErrorWithInvalidPropertiesAreSkipped()
        {
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    semaphore.Release();
                });
            var exception = new System.Exception("Something went wrong.");
            var properties = new Dictionary<string, string>
            {
                [""] = "testEmptyKeySkipped",
                ["testNullValueSkipped"] = null,
                [new string('k', 126)] = "testKeyTooLong",
                ["testValueTooLong"] = new string('v', 126),
                ["normalKey"] = "normalValue"
            };
            Crashes.TrackError(exception, properties);

            // Wait until the http layer sends the log.
            semaphore.Wait(1000);
            Assert.IsNotNull(actualLog);
            Assert.AreEqual(exception.Message, actualLog.Exception.Message);

            // Check original dictionary not sent (copy is made).
            Assert.IsNotNull(actualLog.Properties);
            Assert.AreNotSame(properties, actualLog.Properties);

            // Check invalid values were skipped or truncated.
            Assert.AreEqual(3, actualLog.Properties.Count);
            Assert.AreEqual("normalValue", actualLog.Properties["normalKey"]);
            Assert.AreEqual("testKeyTooLong", actualLog.Properties[new string('k', 125)]);
            Assert.AreEqual(new string('v', 125), actualLog.Properties["testValueTooLong"]);
        }

        [TestMethod]
        public void TrackErrorWhenDisabled()
        {
            var logCount = 0;
            var mockHttpNetworkAdapter = StartCrashes();
            var semaphore = new SemaphoreSlim(0);
            HandledErrorLog actualLog = null;
            Mock.Get(mockHttpNetworkAdapter).Setup(adapter => adapter.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback((string uri, string method, IDictionary<string, string> headers, string content, CancellationToken cancellation) =>
                {
                    actualLog = JsonConvert.DeserializeObject<LogContainer>(content, LogSerializer.SerializationSettings).Logs.Single() as HandledErrorLog;
                    ++logCount;
                    semaphore.Release();
                });

            // If we disable the SDK.
            Crashes.SetEnabledAsync(false).Wait();

            // When we track an error.
            var exception = new System.Exception("Something went wrong.");
            var properties = new Dictionary<string, string> { { "k1", "v1" }, { "p2", "v2" } };
            Crashes.TrackError(exception, properties);

            // Then it's not sent.
            semaphore.Wait(1000);
            Assert.IsNull(actualLog);

            // If we re-enable.
            Crashes.SetEnabledAsync(true).Wait();

            // When we track an error.
            Crashes.TrackError(exception, properties);

            // Then it's sent.
            semaphore.Wait(1000);
            Assert.IsNotNull(actualLog);
            Assert.AreEqual(1, logCount);
            Assert.AreEqual(exception.Message, actualLog.Exception.Message);
            CollectionAssert.AreEquivalent(properties, actualLog.Properties as Dictionary<string, string>);
        }
    }
}
