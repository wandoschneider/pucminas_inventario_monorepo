using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Play.Common.Settings;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace Play.Common.Identity
{
    public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private const string AccessTokenParameter = "access_token";
        private const string MessageHubPath = "/messageHub";
        private readonly IConfiguration configuration;

        public ConfigureJwtBearerOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Configure(string name, JwtBearerOptions options)
        {
            if (name == JwtBearerDefaults.AuthenticationScheme)
            {
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings))
                                                    .Get<ServiceSettings>();

                options.Authority = serviceSettings.Authority;
                options.Audience = serviceSettings.ServiceName;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accesToken = context.Request.Query[AccessTokenParameter];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accesToken) && path.StartsWithSegments(MessageHubPath))
                        {
                            context.Token = accesToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        }

        public void Configure(JwtBearerOptions options)
        {
            Configure(Options.DefaultName, options);
        }
    }
}