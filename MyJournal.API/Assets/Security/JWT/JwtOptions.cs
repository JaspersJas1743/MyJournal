using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MyJournal.API.Assets.Security.JWT;

public sealed class JwtOptions
{
	public string SecretKey { get; set; } = null!;
	public SymmetricSecurityKey SymmetricKey { get; set; } = null!;
	public string Issuer { get; set; } = null!;
	public string Audience { get; set; } = null!;

	public static JwtOptions GetFromEnvironmentVariables()
	{
		JwtOptions jwtOptions = new JwtOptions()
		{
			Audience = Environment.GetEnvironmentVariable(variable: "JwtOptionsAudience")
				?? throw new ArgumentException(message: "Параметр JwtOptionsAudience отсутствует или некорректен.", paramName: "JwtOptionsAudience"),
			Issuer = Environment.GetEnvironmentVariable(variable: "JwtOptionsIssuer")
				?? throw new ArgumentException(message: "Параметр JwtOptionsIssuer отсутствует или некорректен.", paramName: "JwtOptionsIssuer"),
			SecretKey = Environment.GetEnvironmentVariable(variable: "JwtOptionsSecretKey")
				?? throw new ArgumentException(message: "Параметр JwtOptionsSecretKey отсутствует или некорректен.", paramName: "JwtOptionsSecretKey")
		};
		jwtOptions.SymmetricKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: jwtOptions.SecretKey));
		return jwtOptions;
	}
}