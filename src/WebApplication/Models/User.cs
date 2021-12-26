namespace WebApplication.Models
{
    public class User
    {
        public string UserName { get; set; }
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}