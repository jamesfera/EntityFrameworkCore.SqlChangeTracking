// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.TestUtilities
{
    public class RelationalTestHelpers : TestHelpers
    {
        public Action<IServiceCollection> ConfigureServices { get; }

        public RelationalTestHelpers(Action<IServiceCollection> configureServices = null)
        {
            ConfigureServices = configureServices;
        }

        public static RelationalTestHelpers Instance { get; } = new RelationalTestHelpers();

        public override IServiceCollection AddProviderServices(IServiceCollection services)
        {
            ConfigureServices?.Invoke(services);
            return FakeRelationalOptionsExtension.AddEntityFrameworkRelationalDatabase(services);
        }
            

        public override void UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseFakeRelational();
        }

        public override LoggingDefinitions LoggingDefinitions { get; } = new TestRelationalLoggingDefinitions();
    }
}
