using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Biblioteca.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Biblioteca.Api.Utils;

public static class AuthUtils
{
	public static string Hash(string s) =>
		Convert.ToHexString(
			System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(s)));

	public static string CreateJwt(AppUser user, string jwtKey)
	{
		var creds = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
			SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.Name),
			new Claim(ClaimTypes.Email, user.Email),
			new Claim(ClaimTypes.Role, user.Role.ToString())
		};

		var token = new JwtSecurityToken(
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: DateTime.UtcNow.AddHours(8),
			signingCredentials: creds);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
