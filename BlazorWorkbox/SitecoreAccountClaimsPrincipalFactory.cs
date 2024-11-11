using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace BlazorWorkbox
{
    public class SitecoreAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        private const string AdminClaimType = "http://www.sitecore.net/identity/claims/isAdmin";
        private const string SitecoreRoleClaimType = "role";

        public SitecoreAccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor)
        {
        }

        public async override ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
        {
            ClaimsPrincipal userAccount = await base.CreateUserAsync(account, options);
            var userIdentity = (ClaimsIdentity)userAccount.Identity;

            if (userIdentity.IsAuthenticated)
            {

                if (account.AdditionalProperties.TryGetValue(SitecoreRoleClaimType, out var roles))
                {
                    if (roles is JsonElement jsonRoles && jsonRoles.ValueKind == JsonValueKind.Array)
                    {
                        userIdentity.TryRemoveClaim(userIdentity.Claims.FirstOrDefault(c => c.Type == SitecoreRoleClaimType));
                        IEnumerable<Claim> claims = jsonRoles.EnumerateArray().Select(x => new Claim(ClaimTypes.Role, x.ToString().Replace("\\\\", "\\")));
                        userIdentity.AddClaims(claims);
                    }
                }

                if (account.AdditionalProperties.TryGetValue(AdminClaimType, out var admin) && admin is JsonElement adminJson && adminJson.ValueEquals("True"))
                {
                    userIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                }
            }

            return userAccount;
        }
    }
}
