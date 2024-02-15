using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MyJournal.API.Assets.Security.JWT;

public class JwtOptions
{
	public string SecretKey { get; set; } = null!;
	public SymmetricSecurityKey SymmetricKey { get; set; } = null!;
	public string Issuer { get; set; } = null!;
	public string Audience { get; set; } = null!;
}

public static class JwtOptionsExtension
{
	public static JwtOptions GetJwtOptions(this IConfiguration configuration)
	{
		JwtOptions jwtOptions = configuration.GetSection(key: "JwtOptions").Get<JwtOptions>()
			?? throw new ArgumentNullException(message: "Данные для генерации jwt-токена отсутствуют или некорректны", paramName: nameof(JwtOptions));
		jwtOptions.SymmetricKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: jwtOptions.SecretKey));
		return jwtOptions;
	}
}