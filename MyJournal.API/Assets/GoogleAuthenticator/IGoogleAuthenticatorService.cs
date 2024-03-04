namespace MyJournal.API.Assets.GoogleAuthenticator;

public interface IGoogleAuthenticatorService
{
	public record AuthenticationData(string QrCodeUrl, string Code);

	Task<string> GenerateAuthenticationCode();
	Task<AuthenticationData> GenerateQrCode(string username, string authCode);
	Task<bool> VerifyCode(string code, string authCode);
}