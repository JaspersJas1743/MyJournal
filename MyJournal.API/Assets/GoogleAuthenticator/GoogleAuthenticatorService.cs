using Google.Authenticator;

namespace MyJournal.API.Assets.GoogleAuthenticator;

public class GoogleAuthenticatorService : IGoogleAuthenticatorService
{
	private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

	public async Task<string> GenerateAuthenticationCode()
		=> String.Concat(Guid.NewGuid().ToString().ToUpper().Where(x => Base32Alphabet.Contains(x)).Take(10));

	public async Task<IGoogleAuthenticatorService.AuthenticationData> GenerateQrCode(string username, string authCode)
	{
		TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
		SetupCode code = tfa.GenerateSetupCode(
			issuer: "MyJournal",
			accountTitleNoSpaces: username,
			accountSecretKey: Base32Encoding.ToBytes(input: authCode),
			qrPixelsPerModule: 25,
			generateQrCode: true
		);
		byte[] imageBytes = Convert.FromBase64String(code.QrCodeSetupImageUrl.Split(',')[1]);
		using MemoryStream stream = new MemoryStream(imageBytes);
		return new IGoogleAuthenticatorService.AuthenticationData(
			QrCodeUrl: code.QrCodeSetupImageUrl,
			Code: code.ManualEntryKey
		);
	}

	public async Task<bool> VerifyCode(string code, string authCode)
	{
		TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
		return tfa.ValidateTwoFactorPIN(accountSecretKey: authCode, twoFactorCodeFromClient: code, secretIsBase32: true);
	}
}