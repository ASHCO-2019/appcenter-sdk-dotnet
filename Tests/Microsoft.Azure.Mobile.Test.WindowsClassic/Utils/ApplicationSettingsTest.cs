﻿using System.IO;
using System.Reflection;
using Microsoft.Azure.Mobile.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Mobile.Test.WindowsClassic.Utils
{
    [TestClass]
    public class ApplicationSettingsTest
    {
        private IApplicationSettings settings;

        [TestInitialize]
        public void InitializeMobileCenterTest()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var path = Path.Combine(Path.GetDirectoryName(location), "MobileCenter.config");
            File.Delete(path);
            settings = new ApplicationSettings();
        }

        /// <summary>
        /// Verify SetValue generic method behaviour
        /// </summary>
        [TestMethod]
        public void VerifySetValue()
        {
            const string key = "test";
            Assert.IsFalse(settings.ContainsKey(key));
            settings.SetValue(key, 42);
            Assert.IsTrue(settings.ContainsKey(key));
            Assert.AreEqual(42, settings.GetValue<int>(key));
        }

        /// <summary>
        /// Verify GetValue and SetValue generic method behaviour
        /// </summary>
        [TestMethod]
        public void VerifyGetValue()
        {
            const string key = "test";
            Assert.IsFalse(settings.ContainsKey(key));
            Assert.AreEqual(42, settings.GetValue(key, 42));
            Assert.IsTrue(settings.ContainsKey(key));
            Assert.AreEqual(42, settings.GetValue<int>(key));
            Assert.AreEqual(42, settings.GetValue(key, 0));
        }

        /// <summary>
        /// Verify remove values from settings
        /// </summary>
        [TestMethod]
        public void VerifyRemove()
        {
            const string key = "test";
            Assert.IsFalse(settings.ContainsKey(key));
            settings.SetValue(key, 42);
            Assert.IsTrue(settings.ContainsKey(key));
            Assert.AreEqual(42, settings.GetValue<int>(key));
            settings.Remove(key);
            Assert.IsFalse(settings.ContainsKey(key));
        }
    }
}
