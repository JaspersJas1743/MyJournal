namespace MyJournal.API.Assets.Email;

public class EmailOptions
{
	public string SmtpServer { get; set; } = null!;
	public int Port { get; set; }
	public string Sender { get; set; } = null!;
	public string Password { get; set; } = null!;
}

public static class EmailOptionsExtension
{
	public static EmailOptions GetEmailOptions(this IConfiguration configuration)
	{
		return configuration.GetSection(key: "Email").Get<EmailOptions>()
			   ?? throw new ArgumentNullException(message: "Данные Email отсутствуют или некорректны.", paramName: nameof(EmailOptions));
	}
}

