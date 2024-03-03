using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MyJournal.API.Assets.Email;

public class EmailSendingService : IEmailSendingService
{
	private readonly SmtpClient _client = new SmtpClient();

	public EmailSendingService(EmailOptions options)
	{
		_client.Connect(
			host: options.SmtpServer,
			port: options.Port,
			options: SecureSocketOptions.Auto
		);
		_client.Authenticate(
			userName: options.Sender,
			password: options.Password
		);
	}

	~EmailSendingService()
	{
		_client.Disconnect(quit: true);
		_client.Dispose();
	}

	public async Task SendEmail(
		IEnumerable<IEmailSendingService.Receiver> receivers,
		string subject,
		MimeEntity body,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		MimeMessage message = new MimeMessage()
		{
			Subject = subject,
			Sender = new MailboxAddress(name: "MyJournal", address: "myjournalinformation@gmail.com"),
			Body = body
		};
		foreach (IEmailSendingService.Receiver receiver in receivers)
			message.To.Add(address: new MailboxAddress(name: receiver.Name, address: receiver.Email));

		await _client.SendAsync(message: message, cancellationToken: cancellationToken);
	}

	public async Task SendHtml(
		IEnumerable<IEmailSendingService.Receiver> receivers,
		string subject,
		string body,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await SendEmail(
			receivers: receivers,
			subject: subject,
			body: new BodyBuilder() { HtmlBody = body }.ToMessageBody(),
			cancellationToken: cancellationToken
		);
	}

	public async Task SendText(
		IEnumerable<IEmailSendingService.Receiver> receivers,
		string subject,
		string text,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await SendEmail(
			receivers: receivers,
			subject: subject,
			body: new BodyBuilder() { TextBody = text }.ToMessageBody(),
			cancellationToken: cancellationToken
		);
	}
}