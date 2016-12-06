﻿using System;
using System.IO;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Configuration;
using Xamarin.UITest.Queries;

namespace Contoso.Forms.Test.UITests
{
    public class AppInitializer
    {
        public static IApp StartApp(Platform platform)
        {
            if (platform == Platform.Android)
            {
                return ConfigureApp.Android.EnableLocalScreenshots().StartApp(AppDataMode.DoNotClear);
            }

            return ConfigureApp.iOS.EnableLocalScreenshots().StartApp(AppDataMode.DoNotClear);
        }
    }
}
