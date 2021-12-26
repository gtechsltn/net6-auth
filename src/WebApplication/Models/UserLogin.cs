using System.ComponentModel.DataAnnotations;

namespace WebApplication.Models
{
    public class UserLogin
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public UserLogin()
        {
        }
    }
}