﻿using Manatee.Json;
using Manatee.Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace NLIP.iShare.Api.Configurations
{
    public static class JsonSchema
    {
        public static IServiceCollection AddJsonSchema(this IServiceCollection services)
        {
            services.AddSingleton<Func<string, IJsonSchema>>(srv => filename =>
            {
                var schemaJson = JsonValue.Parse(File.ReadAllText(filename));
                return new Manatee.Json.Serialization.JsonSerializer().Deserialize<IJsonSchema>(schemaJson);
            });

            return services;
        }
    }
}
