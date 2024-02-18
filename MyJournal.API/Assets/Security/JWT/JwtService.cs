using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using MyJournal.API.Assets.DatabaseModels;

namespace MyJournal.API.Assets.Security.JWT;

public sealed class JwtService(JwtOptions options) : IJwtService
{
	public string Generate(User tokenOwner, int sessionId)
	{
		JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
			issuer: options.Issuer,
			audience: options.Audience,
			claims: new Claim[]
			{
				new Claim(type: MyJournalClaimTypes.Login, value: tokenOwner.Login!),
				new Claim(type: MyJournalClaimTypes.Password, value: tokenOwner.Password!),
				new Claim(type: MyJournalClaimTypes.Identifier, value: tokenOwner.Id.ToString()),
				new Claim(type: MyJournalClaimTypes.Role, value: tokenOwner.UserRole.Role.ToString()),
				new Claim(type: MyJournalClaimTypes.Session, value: sessionId.ToString()),
			},
			signingCredentials: new SigningCredentials(
				key: options.SymmetricKey,
				algorithm: SecurityAlgorithms.HmacSha256
			)
		);

		return new JwtSecurityTokenHandler().WriteToken(token: jwtSecurityToken);
	}
}