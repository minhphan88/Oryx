﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Oryx.BuildScriptGeneratorCli
{
    internal static class BuildScriptGeneratorCliServiceCollectionExtensions
    {
        public static IServiceCollection AddCliServices(this IServiceCollection services)
        {
            services.AddSingleton<IEnvironmentSettingsProvider, DefaultEnvironmentSettingsProvider>();
            services.AddSingleton<IConsole, PhysicalConsole>();
            return services;
        }
    }
}
