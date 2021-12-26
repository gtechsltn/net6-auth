using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApplication.Models;

namespace WebApplication.Helpers
{
    public static class JwtHelper
    {
        public static IEnumerable<Claim> GetClaims(this UserToken userAccount, Guid Id)
        {
            IEnumerable<Claim> claims = new Claim[]
            {
                new Claim("Id",userAccount.Id.ToString()),
                new Claim(ClaimTypes.Name, userAccount.UserName),
                new Claim(ClaimTypes.Email, userAccount.Email),
                new Claim(ClaimTypes.NameIdentifier,Id.ToString()),
                new Claim(ClaimTypes.Expiration,DateTime.UtcNow.AddDays(1).ToString("MMM ddd dd yyyy HH:mm:ss tt") )
            };
            return claims;
        }

        public static IEnumerable<Claim> GetClaims(this UserToken userAccount, out Guid Id)
        {
            Id = Guid.NewGuid();
            return userAccount.GetClaims(Id);
        }

        public static UserToken GenTokenKey(UserToken model, JwtSetting jwtSetting)
        {
            try
            {
                var userToken = new UserToken();
                if (model == null) throw new ArgumentException(nameof(model));

                // Get secret key
                var key = System.Text.Encoding.ASCII.GetBytes(jwtSetting.IssuerSigningKey);
                Guid Id = Guid.Empty;
                DateTime expireTime = DateTime.UtcNow.AddDays(1);
                userToken.Validaty = expireTime.TimeOfDay;
                var jwtToken = new JwtSecurityToken(
                    issuer: jwtSetting.ValidIssuer,
                    audience: jwtSetting.ValidAudience,
                    claims: model.GetClaims(out Id),
                    notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                    expires: new DateTimeOffset(expireTime).DateTime,
                    signingCredentials: new SigningCredentials
                    (new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                );

                userToken.Token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
                var idRefreshToken = Guid.NewGuid();
                userToken.UserName = model.UserName;
                userToken.Id = model.Id;
                userToken.GuidId = Id;
                userToken.RefreshToken = idRefreshToken.ToString();
                userToken.Email = model.Email;
                return userToken;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}