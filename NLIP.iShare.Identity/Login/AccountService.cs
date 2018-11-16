﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Identity;
using NLIP.iShare.Configuration.Configurations;
using NLIP.iShare.Models;

namespace NLIP.iShare.Identity.Login
{
    public class AccountService<TIdentity> : IAccountService<TIdentity> 
        where TIdentity : class, IAspNetUser
    {
        private readonly UserManager<TIdentity> _userManager;
        private readonly UrlEncoder _urlEncoder;
        private readonly SpaOptions _spaOptions;

        public AccountService(UserManager<TIdentity> userManager, UrlEncoder urlEncoder, SpaOptions spaOptions)
        {
            _userManager = userManager;
            _urlEncoder = urlEncoder;
            _spaOptions = spaOptions;
        }

        public async Task<Response<TIdentity>> CheckCredentials(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null && await _userManager.CheckPasswordAsync(user, password))
            {
                return Response<TIdentity>.ForSuccess(user);
            }
            return Response<TIdentity>.ForError("Credentials are not valid");
        }
        public async Task<Response> EnableAuthenticator(EnableAuthenticatorRequest request, string userId)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user.TwoFactorEnabled)
            {
                return Response.ForError("2FA already enabled");
            }
            if (request.Code.IsNullOrEmpty())
            {
                return Response.ForError("Code is missing");
            }
            var verificationCode = CleanCode(request.Code);

            var is2FaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
            if (!is2FaTokenValid)
            {
                return Response.ForError("2FA token is not valid");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
            {
                return Response.ForErrors(result.Errors.Select(c => $"{c.Code} - {c.Description}"));
            }

            return Response.ForSuccess();
        }

        public async Task<Response<TIdentity>> Verify(string code, string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                return Response<TIdentity>.ForError("Credentials are not valid");
            }

            if (code.IsNullOrEmpty())
            {
                return Response<TIdentity>.ForError("Code is missing");
            }

            var authenticatorCode = CleanCode(code);

            var is2FaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider, authenticatorCode);
            if (!is2FaTokenValid)
            {
                return Response<TIdentity>.ForError("2FA token is not valid");
            }
            return Response<TIdentity>.ForSuccess();
        }

        public async Task<Response<AuthenticatorKey>> GetAuthenticatorKey(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user.TwoFactorEnabled)
            {
                return Response<AuthenticatorKey>.ForError("2FA already enabled");
            }

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return Response<AuthenticatorKey>.ForSuccess(new AuthenticatorKey
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey)
            });
        }

        public async Task<Response<TIdentity>> Login(LoginRequest request)
        {
            var result = await CheckCredentials(request.Username, request.Password);

            if (!result.Success)
            {
                return Response<TIdentity>.ForError(LoginErrorMessages.InvalidCredentials);
            }

            if (_spaOptions.TwoFactorEnabled.HasValue && _spaOptions.TwoFactorEnabled.Value)
            {
                if (result.Model.TwoFactorEnabled)
                {
                    if (request.TwoFactorCode.IsNullOrEmpty())
                    {
                        return Response<TIdentity>.ForError(LoginErrorMessages.TwoFactorCodeRequired);
                    }
                    var result2Fa = await Verify(request.TwoFactorCode, request.Username, request.Password);
                    if (!result2Fa.Success)
                    {
                        return Response<TIdentity>.ForError(LoginErrorMessages.InvalidCredentials);
                    }
                }
                else
                {
                    return Response<TIdentity>.ForError(LoginErrorMessages.TwoFactorSetupRequired);
                }
            }

            return Response<TIdentity>.ForSuccess(result.Model);

        }
        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLower(CultureInfo.CurrentCulture);
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
            => $"otpauth://totp/{_urlEncoder.Encode(_spaOptions.ApplicationName)}:{_urlEncoder.Encode(email)}?secret={unformattedKey}&issuer={_spaOptions.ApplicationName}&digits=6";

        private static string CleanCode(string code) => code
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
