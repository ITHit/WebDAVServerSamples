// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;

namespace SharedMobile
{
    public class WebHostOptions
    {
        public WebHostOptions() { }

        public WebHostOptions(IConfiguration configuration)
            : this(configuration, string.Empty) { }

        public WebHostOptions(IConfiguration configuration, string applicationNameFallback)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? applicationNameFallback;
            StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
            DetailedErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[WebHostDefaults.EnvironmentKey];
            WebRoot = configuration[WebHostDefaults.WebRootKey];
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
            PreventHostingStartup = WebHostUtilities.ParseBool(configuration, WebHostDefaults.PreventHostingStartupKey);
            // Search the primary assembly and configured assemblies.
            HostingStartupAssemblies = $"{ApplicationName};{configuration[WebHostDefaults.HostingStartupAssembliesKey]}"
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var timeout = configuration[WebHostDefaults.ShutdownTimeoutKey];
            if (!string.IsNullOrEmpty(timeout)
                && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
            }
        }

        public string ApplicationName { get; set; }

        public bool PreventHostingStartup { get; set; }

        public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

        public bool DetailedErrors { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string StartupAssembly { get; set; }

        public string WebRoot { get; set; }

        public string ContentRootPath { get; set; }

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    }
}