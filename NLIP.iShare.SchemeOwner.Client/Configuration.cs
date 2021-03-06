﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLIP.iShare.Configuration;
using NLIP.iShare.Configuration.Configurations;
using NLIP.iShare.TokenClient;

namespace NLIP.iShare.SchemeOwner.Client
{
    public static class Configuration
    {
        public static IServiceCollection AddSchemeOwnerClient(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment environment)
        {
            var options = services.ConfigureOptions<SchemeOwnerClientOptions>(configuration,
                "SchemeOwner",
                new ConfigurationOptionsValidator
                {
                    Environment = environment.EnvironmentName
                });

            services.AddTokenClient(new TokenSource
            {
                BaseUri = new Uri(options.BaseUri),
                Thumbprint = options.Thumbprint
            });

            services.AddTransient<SchemeOwnerClient>();

            return services;
        }
    }
}
