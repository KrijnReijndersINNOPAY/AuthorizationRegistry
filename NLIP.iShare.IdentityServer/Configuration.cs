﻿using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using NLIP.iShare.Configuration;
using NLIP.iShare.Configuration.Configurations;
using NLIP.iShare.IdentityServer.Delegation;
using NLIP.iShare.IdentityServer.Services;
using NLIP.iShare.IdentityServer.Stores;
using NLIP.iShare.IdentityServer.Validation;

namespace NLIP.iShare.IdentityServer
{
    public static class Configuration
    {
        /// <summary>
        /// Allow all CORS policy for Identity Server clients
        /// </summary>
        /// <param name="services"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static IServiceCollection AddIdentityServerCors(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            var cors = new DefaultCorsPolicyService(loggerFactory.CreateLogger<DefaultCorsPolicyService>())
            {
                AllowAll = true
            };
            services.AddSingleton<ICorsPolicyService>(cors);
            return services;
        }

        /// <summary>
        /// Register the services and validators needed for a party that acts as a Service Provider
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddConsumer(this IIdentityServerBuilder builder)
        {
            var services = builder.Services;
            services
                .AddTransient<IAssertionManager, ClientAssertionManager>()
                .AddTransientDecorator<IClientStore, PublicClientStore>()
                ;

            builder.AddSecretValidators(
                typeof(ClientAssertionSecretValidator), 
                typeof(PrivateKeyJwtSecretValidator),
                typeof(PartyValidator)
                );
            services.AddTransientDecorator<ISecretValidator, ServiceConsumerSecretValidator>();
            return builder;
        }

        /// <summary>
        /// Register minimal Identity Server that is used and shared by any party from the iSHARE scheme
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="environment"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>

        public static IIdentityServerBuilder AddIdentityServer(this IServiceCollection services,
            IConfiguration configuration,
            IHostingEnvironment environment,
            ILoggerFactory loggerFactory)
        {
            services.AddIdentityServerCors(loggerFactory);

            var builder = services.AddIdentityServer(options =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        var partyDetailsOptions = serviceProvider.GetRequiredService<PartyDetailsOptions>();
                        if (environment.IsDevelopment())
                        {
                            IdentityModelEventSource.ShowPII = true;
                        }
                        options.IssuerUri = partyDetailsOptions.BaseUri;

                        var spaOptions = serviceProvider.GetService<SpaOptions>();
                        if (spaOptions != null)
                        {
                            options.UserInteraction.LoginUrl = spaOptions.BaseUri + "account/login";
                            options.UserInteraction.ErrorUrl = spaOptions.BaseUri + "access-denied";
                        }
                    })
                    .AddPki(configuration)
                    .AddDelegation()
                    .AddSecretParser<JwtBearerClientAssertionSecretParser>()
                    .AddCustomTokenRequestValidators(typeof(TokenRequestValidator))
                ;
            return builder;
        }

        public static IIdentityServerBuilder AddIdentityServerSigningCredentials(this IIdentityServerBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var partyDetailsOptions = serviceProvider.GetRequiredService<PartyDetailsOptions>();
            builder.AddSigningCredential(partyDetailsOptions.SigningCredential);
            return builder;
        }

        internal static IIdentityServerBuilder AddDelegation(this IIdentityServerBuilder identityServerBuilder)
        {
            var services = identityServerBuilder.Services;
            services.AddTransient<IDelegationMaskValidationService, DelegationMaskValidationService>();
            services.AddTransient<IDelegationTranslateService, DelegationTranslateService>();
            return identityServerBuilder;
        }
    }
}
