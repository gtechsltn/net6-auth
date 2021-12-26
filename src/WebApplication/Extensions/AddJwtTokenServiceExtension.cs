using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WebApplication.Models;

namespace WebApplication.Extensions
{
    public static class AddJwtTokenServiceExtension
    {
        public static void AddJwtTokenService(this IServiceCollection Services, IConfiguration Configuration)
        {
            // Add Jwt Settings
            var bindJwtSetting = new JwtSetting();
            Configuration.Bind("JsonWebTokenKey", bindJwtSetting);
            Services.AddSingleton(bindJwtSetting);

            Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = bindJwtSetting.ValidateIssuerSigningKey,
                        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(bindJwtSetting.IssuerSigningKey)),
                        ValidateIssuer = bindJwtSetting.ValidateIssuer,
                        ValidIssuer = bindJwtSetting.ValidIssuer,
                        ValidateAudience = bindJwtSetting.ValidateAudience,
                        ValidAudience = bindJwtSetting.ValidAudience,
                        RequireExpirationTime = bindJwtSetting.RequireExpirationTime,
                        ValidateLifetime = bindJwtSetting.RequireExpirationTime,
                        ClockSkew = TimeSpan.FromDays(1),
                    };
                });
        }
    }
}