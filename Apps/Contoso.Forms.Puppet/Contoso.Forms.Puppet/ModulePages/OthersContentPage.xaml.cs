// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Auth;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Data;
using Microsoft.AppCenter.Distribute;
using Microsoft.AppCenter.Push;
using Microsoft.AppCenter.Rum;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace Contoso.Forms.Puppet
{
    using XamarinDevice = Xamarin.Forms.Device;

    [Android.Runtime.Preserve(AllMembers = true)]
    public partial class OthersContentPage
    {
        static bool _rumStarted;

        static bool _eventFilterStarted;

        static OthersContentPage()
        {
            Data.RemoteOperationCompleted += (sender, eventArgs) =>
            {
                AppCenterLog.Info(App.LogTag, "Remote operation completed event=" + JsonConvert.SerializeObject(eventArgs) + " sender=" + sender);
            };
        }

        public OthersContentPage()
        {
            InitializeComponent();
            if (XamarinDevice.RuntimePlatform == XamarinDevice.iOS)
            {
                Icon = "handbag.png";
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var acEnabled = await AppCenter.IsEnabledAsync();
            DistributeEnabledSwitchCell.On = await Distribute.IsEnabledAsync();
            DistributeEnabledSwitchCell.IsEnabled = acEnabled;
            PushEnabledSwitchCell.On = await Push.IsEnabledAsync();
            PushEnabledSwitchCell.IsEnabled = acEnabled;
            RumEnabledSwitchCell.On = _rumStarted && await RealUserMeasurements.IsEnabledAsync();
            RumEnabledSwitchCell.IsEnabled = acEnabled;
            EventFilterEnabledSwitchCell.On = _eventFilterStarted && await EventFilterHolder.Implementation?.IsEnabledAsync();
            EventFilterEnabledSwitchCell.IsEnabled = acEnabled && EventFilterHolder.Implementation != null;
        }

        async void UpdateDistributeEnabled(object sender, ToggledEventArgs e)
        {
            await Distribute.SetEnabledAsync(e.Value);
        }

        async void UpdatePushEnabled(object sender, ToggledEventArgs e)
        {
            await Push.SetEnabledAsync(e.Value);
        }

        async void UpdateRumEnabled(object sender, ToggledEventArgs e)
        {
            if (!_rumStarted)
            {
                RealUserMeasurements.SetRumKey("b1919553367d44d8b0ae72594c74e0ff");
                AppCenter.Start(typeof(RealUserMeasurements));
                _rumStarted = true;
            }
            await RealUserMeasurements.SetEnabledAsync(e.Value);
        }

        async void UpdateEventFilterEnabled(object sender, ToggledEventArgs e)
        {
            if (EventFilterHolder.Implementation != null)
            {
                if (!_eventFilterStarted)
                {
                    AppCenter.Start(EventFilterHolder.Implementation.BindingType);
                    _eventFilterStarted = true;
                }
                await EventFilterHolder.Implementation.SetEnabledAsync(e.Value);
            }
        }

        async void RunMBaaSAsync(object sender, EventArgs e)
        {
            try
            {
                var userInfo = await Auth.SignInAsync();
                AppCenterLog.Info(App.LogTag, "Auth.SignInAsync succeeded accountId=" + userInfo.AccountId);
            }
            catch (Exception ex)
            {
                AppCenterLog.Error(App.LogTag, "Auth scenario failed", ex);
                Crashes.TrackError(ex);
            }
            try
            {
                var list = await Data.ListAsync<CustomDocument>(DefaultPartitions.UserDocuments);
                foreach (var doc in list)
                {
                    AppCenterLog.Info(App.LogTag, "List result=" + doc.Id);
                }
                var document = list.CurrentPage.Items.First();
                AppCenterLog.Info(App.LogTag, "List first result=" + JsonConvert.SerializeObject(document));
                document = await Data.DeleteAsync<CustomDocument>(document.Id, DefaultPartitions.UserDocuments);
                AppCenterLog.Info(App.LogTag, "Delete result=" + JsonConvert.SerializeObject(document));
            }
            catch (Exception ex)
            {
                AppCenterLog.Error(App.LogTag, "Data list/delete first scenario failed", ex);
                Crashes.TrackError(ex);
            }
            try
            {
                var objectCollection = new List<Uri>();
                objectCollection.Add(new Uri("http://google.com/"));
                objectCollection.Add(new Uri("http://microsoft.com/"));
                objectCollection.Add(new Uri("http://facebook.com/"));
                var primitiveCollection = new List<int>();
                primitiveCollection.Add(1);
                primitiveCollection.Add(2);
                primitiveCollection.Add(3);
                var dict = new Dictionary<string, Uri>();
                dict.Add("key1", new Uri("http://google.com/"));
                dict.Add("key2", new Uri("http://microsoft.com/"));
                dict.Add("key3", new Uri("http://facebook.com/"));
                var customDoc = new CustomDocument
                {
                    Id = Guid.NewGuid(),
                    TimeStamp = DateTime.UtcNow,
                    SomeNumber = 123,
                    SomeObject = dict,
                    SomePrimitiveArray = new int[] { 1, 2, 3 },
                    SomeObjectArray = new CustomDocument[] { 
                        new CustomDocument { 
                            Id = Guid.NewGuid(),
                            TimeStamp = DateTime.UtcNow,
                            SomeNumber = 123,
                            SomeObject = dict,
                            SomePrimitiveArray = new int[] { 1, 2, 3 },
                            SomeObjectCollection = objectCollection,
                            SomePrimitiveCollection = primitiveCollection  
                        } 
                    },
                    SomeObjectCollection = objectCollection,
                    SomePrimitiveCollection = primitiveCollection,
                    Custom = new CustomDocument
                    {
                        Id = Guid.NewGuid(),
                        TimeStamp = DateTime.UtcNow,
                        SomeNumber = 123,
                        SomeObject = dict,
                        SomePrimitiveArray = new int[] { 1, 2, 3 },
                        SomeObjectCollection = objectCollection,
                        SomePrimitiveCollection = primitiveCollection
                    }
                };
                var id = customDoc.Id.ToString();
                var document = await Data.ReplaceAsync(id, customDoc, DefaultPartitions.UserDocuments);
                AppCenterLog.Info(App.LogTag, "Replace result=" + JsonConvert.SerializeObject(document));
                document = await Data.ReadAsync<CustomDocument>(id, DefaultPartitions.UserDocuments);
                AppCenterLog.Info(App.LogTag, "Read result=" + JsonConvert.SerializeObject(document));
                await Data.ReplaceAsync(Guid.NewGuid().ToString(), customDoc, DefaultPartitions.UserDocuments);
                await Data.ReplaceAsync(Guid.NewGuid().ToString(), customDoc, DefaultPartitions.UserDocuments);
            }
            catch (Exception ex)
            {
                AppCenterLog.Error(App.LogTag, "Data person scenario failed", ex);
                Crashes.TrackError(ex);
            }
        }

        void SignOut(object sender, EventArgs e)
        {
            Auth.SignOut();
        }

        public class CustomDocument
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("timestamp")]
            public DateTime TimeStamp { get; set; }

            [JsonProperty("somenumber")]
            public int SomeNumber { get; set; }

            [JsonProperty("someprimitivearray")]
            public int[] SomePrimitiveArray { get; set; }

            [JsonProperty("someobjectarray")]
            public CustomDocument[] SomeObjectArray { get; set; }

            [JsonProperty("someprimitivecollection")]
            public IList SomePrimitiveCollection { get; set; }

            [JsonProperty("someobjectcollection")]
            public IList SomeObjectCollection { get; set; }

            [JsonProperty("someobject")]
            public Dictionary<string, Uri> SomeObject { get; set; }

            [JsonProperty("customdocument")]
            public CustomDocument Custom { get; set; }

            //[JsonProperty("String0000")]
            //public string String0000 { get; set; }
            //[JsonProperty("String0001")]
            //public string String0001 { get; set; }
            //[JsonProperty("String0002")]
            //public string String0002 { get; set; }
            //[JsonProperty("String0003")]
            //public string String0003 { get; set; }
            //[JsonProperty("String0004")]
            //public string String0004 { get; set; }
            //[JsonProperty("String0005")]
            //public string String0005 { get; set; }
            //[JsonProperty("String0006")]
            //public string String0006 { get; set; }
            //[JsonProperty("String0007")]
            //public string String0007 { get; set; }
            //[JsonProperty("String0008")]
            //public string String0008 { get; set; }
            //[JsonProperty("String0009")]
            //public string String0009 { get; set; }
            //[JsonProperty("String0010")]
            //public string String0010 { get; set; }
            //[JsonProperty("String0011")]
            //public string String0011 { get; set; }
            //[JsonProperty("String0012")]
            //public string String0012 { get; set; }
            //[JsonProperty("String0013")]
            //public string String0013 { get; set; }
            //[JsonProperty("String0014")]
            //public string String0014 { get; set; }
            //[JsonProperty("String0015")]
            //public string String0015 { get; set; }
            //[JsonProperty("String0016")]
            //public string String0016 { get; set; }
            //[JsonProperty("String0017")]
            //public string String0017 { get; set; }
            //[JsonProperty("String0018")]
            //public string String0018 { get; set; }
            //[JsonProperty("String0019")]
            //public string String0019 { get; set; }
            //[JsonProperty("String0020")]
            //public string String0020 { get; set; }
            //[JsonProperty("String0021")]
            //public string String0021 { get; set; }
            //[JsonProperty("String0022")]
            //public string String0022 { get; set; }
            //[JsonProperty("String0023")]
            //public string String0023 { get; set; }
            //[JsonProperty("String0024")]
            //public string String0024 { get; set; }
            //[JsonProperty("String0025")]
            //public string String0025 { get; set; }
            //[JsonProperty("String0026")]
            //public string String0026 { get; set; }
            //[JsonProperty("String0027")]
            //public string String0027 { get; set; }
            //[JsonProperty("String0028")]
            //public string String0028 { get; set; }
            //[JsonProperty("String0029")]
            //public string String0029 { get; set; }
            //[JsonProperty("String0030")]
            //public string String0030 { get; set; }
            //[JsonProperty("String0031")]
            //public string String0031 { get; set; }
            //[JsonProperty("String0032")]
            //public string String0032 { get; set; }
            //[JsonProperty("String0033")]
            //public string String0033 { get; set; }
            //[JsonProperty("String0034")]
            //public string String0034 { get; set; }
            //[JsonProperty("String0035")]
            //public string String0035 { get; set; }
            //[JsonProperty("String0036")]
            //public string String0036 { get; set; }
            //[JsonProperty("String0037")]
            //public string String0037 { get; set; }
            //[JsonProperty("String0038")]
            //public string String0038 { get; set; }
            //[JsonProperty("String0039")]
            //public string String0039 { get; set; }
            //[JsonProperty("String0040")]
            //public string String0040 { get; set; }
            //[JsonProperty("String0041")]
            //public string String0041 { get; set; }
            //[JsonProperty("String0042")]
            //public string String0042 { get; set; }
            //[JsonProperty("String0043")]
            //public string String0043 { get; set; }
            //[JsonProperty("String0044")]
            //public string String0044 { get; set; }
            //[JsonProperty("String0045")]
            //public string String0045 { get; set; }
            //[JsonProperty("String0046")]
            //public string String0046 { get; set; }
            //[JsonProperty("String0047")]
            //public string String0047 { get; set; }
            //[JsonProperty("String0048")]
            //public string String0048 { get; set; }
            //[JsonProperty("String0049")]
            //public string String0049 { get; set; }
            //[JsonProperty("String0050")]
            //public string String0050 { get; set; }
            //[JsonProperty("String0051")]
            //public string String0051 { get; set; }
            //[JsonProperty("String0052")]
            //public string String0052 { get; set; }
            //[JsonProperty("String0053")]
            //public string String0053 { get; set; }
            //[JsonProperty("String0054")]
            //public string String0054 { get; set; }
            //[JsonProperty("String0055")]
            //public string String0055 { get; set; }
            //[JsonProperty("String0056")]
            //public string String0056 { get; set; }
            //[JsonProperty("String0057")]
            //public string String0057 { get; set; }
            //[JsonProperty("String0058")]
            //public string String0058 { get; set; }
            //[JsonProperty("String0059")]
            //public string String0059 { get; set; }
            //[JsonProperty("String0060")]
            //public string String0060 { get; set; }
            //[JsonProperty("String0061")]
            //public string String0061 { get; set; }
            //[JsonProperty("String0062")]
            //public string String0062 { get; set; }
            //[JsonProperty("String0063")]
            //public string String0063 { get; set; }
            //[JsonProperty("String0064")]
            //public string String0064 { get; set; }
            //[JsonProperty("String0065")]
            //public string String0065 { get; set; }
            //[JsonProperty("String0066")]
            //public string String0066 { get; set; }
            //[JsonProperty("String0067")]
            //public string String0067 { get; set; }
            //[JsonProperty("String0068")]
            //public string String0068 { get; set; }
            //[JsonProperty("String0069")]
            //public string String0069 { get; set; }
            //[JsonProperty("String0070")]
            //public string String0070 { get; set; }
            //[JsonProperty("String0071")]
            //public string String0071 { get; set; }
            //[JsonProperty("String0072")]
            //public string String0072 { get; set; }
            //[JsonProperty("String0073")]
            //public string String0073 { get; set; }
            //[JsonProperty("String0074")]
            //public string String0074 { get; set; }
            //[JsonProperty("String0075")]
            //public string String0075 { get; set; }
            //[JsonProperty("String0076")]
            //public string String0076 { get; set; }
            //[JsonProperty("String0077")]
            //public string String0077 { get; set; }
            //[JsonProperty("String0078")]
            //public string String0078 { get; set; }
            //[JsonProperty("String0079")]
            //public string String0079 { get; set; }
            //[JsonProperty("String0080")]
            //public string String0080 { get; set; }
            //[JsonProperty("String0081")]
            //public string String0081 { get; set; }
            //[JsonProperty("String0082")]
            //public string String0082 { get; set; }
            //[JsonProperty("String0083")]
            //public string String0083 { get; set; }
            //[JsonProperty("String0084")]
            //public string String0084 { get; set; }
            //[JsonProperty("String0085")]
            //public string String0085 { get; set; }
            //[JsonProperty("String0086")]
            //public string String0086 { get; set; }
            //[JsonProperty("String0087")]
            //public string String0087 { get; set; }
            //[JsonProperty("String0088")]
            //public string String0088 { get; set; }
            //[JsonProperty("String0089")]
            //public string String0089 { get; set; }
            //[JsonProperty("String0090")]
            //public string String0090 { get; set; }
            //[JsonProperty("String0091")]
            //public string String0091 { get; set; }
            //[JsonProperty("String0092")]
            //public string String0092 { get; set; }
            //[JsonProperty("String0093")]
            //public string String0093 { get; set; }
            //[JsonProperty("String0094")]
            //public string String0094 { get; set; }
            //[JsonProperty("String0095")]
            //public string String0095 { get; set; }
            //[JsonProperty("String0096")]
            //public string String0096 { get; set; }
            //[JsonProperty("String0097")]
            //public string String0097 { get; set; }
            //[JsonProperty("String0098")]
            //public string String0098 { get; set; }
            //[JsonProperty("String0099")]
            //public string String0099 { get; set; }

        }
    }
}
