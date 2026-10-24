using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

public class TokenService(IConfiguration config)
{
    public string Create(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("uid", user.Id.ToString()),
                new Claim("name", user.FullName),
                new Claim("email", user.Email)
            }),
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

public static class PrincipalExtensions
{
    public static int UserId(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirst("uid")?.Value, out var id) ? id : 0;
}
