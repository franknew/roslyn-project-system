﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class OptionsSettingsTests
    {
        [Fact]
        public void OptionsSettings_Constructor_AllNull_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                new OptionsSettings(null, null);
            });
        }

        [Fact]
        public void OptionsSettings_Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                new OptionsSettings(null, IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void OptionsSettings_Constructor_NullAsProjectThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => {
                new OptionsSettings(SVsServiceProviderFactory.Create(), null);
            });
        }

        [Fact]
        public void OptionsSettings_Constructor_Success()
        {
            var optionnsSettings = new OptionsSettings(SVsServiceProviderFactory.Create(), IProjectThreadingServiceFactory.Create());

            Assert.True(optionnsSettings != null);
        }

        [Fact]
        public void OptionsSettings_GetPropertiesValue_UIThread_Failure()
        {
            COMException exception = Assert.ThrowsAsync<COMException>(async () =>
            {
                var optionnsSettings = new OptionsSettings(SVsServiceProviderFactory.Create(), IProjectThreadingServiceFactory.Create());
                var task = Task.Run(() =>
                {
                    optionnsSettings.GetPropertiesValue("foo", "foo", "foo", true);
                });
                await task;
            }).Result;

            Assert.Contains("UI thread", exception.Message);
        }

        [Fact]
        public  void OptionsSettings_GetPropertiesValue__UIThread_Success()
        {
            var results = Task.Run(async () =>
            {
                var threadingService = IProjectThreadingServiceFactory.Create();
                var optionsSettingsFactory = IOptionsSettingsFactory.Implement((string category, string page, string property, bool defaultValue) =>
                {
                    threadingService.VerifyOnUIThread();
                    return defaultValue;
                });

                await threadingService.SwitchToUIThread();
                return optionsSettingsFactory.GetPropertiesValue("foo", "foo", "foo", true);
            }).Result;
            Assert.True(results);
        }

        [Fact]
        public void OptionsSettings_GetPropertiesValue__FalseValue()
        {
            var results = Task.Run(async () =>
            {
                var threadingService = IProjectThreadingServiceFactory.Create();
                var optionsSettingsFactory = IOptionsSettingsFactory.Implement((string category, string page, string property, bool defaultValue) =>
                {
                    threadingService.VerifyOnUIThread();
                    return defaultValue;
                });
                await threadingService.SwitchToUIThread();
                return optionsSettingsFactory.GetPropertiesValue("foo", "foo", "foo", false);
            }).Result;

            Assert.False(results);
        }

        [Fact]
        public void OptionsSettings_GetPropertiesValue__IntValue()
        {
            var results = Task.Run(async () =>
            {
                var threadingService = IProjectThreadingServiceFactory.Create();
                var optionsSettingsFactory = IOptionsSettingsFactory.Implement((string category, string page, string property, bool defaultValue) =>
                {
                    threadingService.VerifyOnUIThread();
                    return defaultValue;
                });

                await threadingService.SwitchToUIThread();
                return optionsSettingsFactory.GetPropertiesValue("foo", "foo", "foo", 5);
            }).Result;

            Assert.False(results==5);
        }
    }
}
