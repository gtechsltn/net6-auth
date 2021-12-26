using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Helpers;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly JwtSetting _jwtSetting;

        private readonly ILogger<AccountController> _logger;

        public AccountController(JwtSetting jwtSetting, ILogger<AccountController> logger)
        {
            _jwtSetting = jwtSetting;
            _logger = logger;
        }

        private IEnumerable<User> logins = new List<User>()
        {
            new User()
            {
                Id = Guid.NewGuid(),
                Email = "manhng83@gmail.com",
                UserName = "Admin",
                Password = "Admin",
            },
            new User()
            {
                Id = Guid.NewGuid(),
                Email = "manhng83@gmail.com",
                UserName = "User",
                Password = "User",
            }
        };

        /// <summary>
        /// Generate an Access Token
        /// </summary>
        /// <param name="userLogin"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetToken(UserLogin userLogin)
        {
            _logger.LogInformation($"{nameof(GetToken)} started");

            try
            {
                var Token = new UserToken();
                var Valid = logins.Any(x => x.UserName.Equals(userLogin.UserName, StringComparison.OrdinalIgnoreCase));
                if (Valid)
                {
                    var user = logins.FirstOrDefault(x => x.UserName.Equals(userLogin.UserName, StringComparison.OrdinalIgnoreCase));
                    Token = JwtHelper.GenTokenKey(new UserToken()
                    {
                        Email = user.Email,
                        GuidId = Guid.NewGuid(),
                        UserName = user.UserName,
                        Id = user.Id,
                    }, _jwtSetting);
                }
                else
                {
                    return BadRequest($"wrong password");
                }
                return Ok(Token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get List of UserAccounts
        /// </summary>
        /// <returns>List Of UserAccounts</returns>
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetList()
        {
            _logger.LogInformation($"{nameof(GetList)} started");

            return Ok(logins);
        }
    }
}