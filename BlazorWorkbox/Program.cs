using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWorkbox;
using BlazorWorkbox.Models;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

AppSettings appSettings = new AppSettings();
builder.Configuration.GetSection("AppSettings").Bind(appSettings);

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = appSettings.IdentityAuthorityBaseUrl;
    options.ProviderOptions.ClientId = "BlazorWorkbox";
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Clear();
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("sitecore.profile");
    options.ProviderOptions.DefaultScopes.Add("sitecore.profile.api");
});

builder.Services.AddScoped(typeof(AccountClaimsPrincipalFactory<RemoteUserAccount>), typeof(SitecoreAccountClaimsPrincipalFactory));

builder.Services.Configure<AppSettings>(x => builder.Configuration.GetSection("AppSettings").Bind(x));

builder.Services.AddHttpClient<IGraphQLClient, GraphQLHttpClient>(x =>
{
    GraphQLHttpClientOptions graphQLHttpClientOptions = new GraphQLHttpClientOptions()
    {
        EndPoint = new Uri($"{appSettings.ContentManagementInstanceBaseUrl}/sitecore/api/authoring/graphql/v1")
    };

    return new GraphQLHttpClient(graphQLHttpClientOptions, new SystemTextJsonSerializer(x => x.Converters.Add(new GuidJsonConverter())), x);

}).AddHttpMessageHandler<SitecoreAuthorizationMessageHandler>();

builder.Services.AddTransient<SitecoreAuthorizationMessageHandler>();
builder.Services.AddScoped<DialogService>();

await builder.Build().RunAsync();