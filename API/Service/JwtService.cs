using API.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Service
{
    public class JwtService
    {
        private readonly string key = string.Empty;
        private readonly int duration;

        //Constructor
        public JwtService(IConfiguration configuration)
        {
            key = configuration["Jwt:Key"]!;
            duration = int.Parse(configuration["Jwt:Duration"]!);
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.key));
            var signingKey = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var claims = new[]
            {
                new Claim("firstName", user.FirstName !),
                new Claim("lastName", user.LastName !),
                new Claim("email", user.Email !),
                new Claim("mobileNumber", user.MobileNumber !),
                new Claim("userType", user.UserType.ToString()),
                new Claim("accountStatus", user.AccountStatus.ToString()),
                new Claim("createdOn", user.CreatedOn.ToString()),
            };

            var jwtToken = new JwtSecurityToken(
                issuer: "blumbit",
                audience: "blumbit",
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(duration),
                signingKey);

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }
    }
}
