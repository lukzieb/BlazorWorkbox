using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components;
using BlazorWorkbox.Models;
using Microsoft.Extensions.Options;

namespace BlazorWorkbox
{
    public class SitecoreAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public SitecoreAuthorizationMessageHandler(IOptions<AppSettings> appSettings, IAccessTokenProvider provider, NavigationManager navigation) : base(provider, navigation)
        {
            ConfigureHandler(authorizedUrls: new[] {
                appSettings.Value.ContentManagementInstanceBaseUrl
            });
        }
    }
}
