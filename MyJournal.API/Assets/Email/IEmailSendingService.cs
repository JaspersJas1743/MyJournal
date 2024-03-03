namespace MyJournal.API.Assets.Email;

public interface IEmailSendingService
{
	sealed record Receiver(string Name, string Email);

	Task SendHtml(
		IEnumerable<Receiver> receivers,
		string subject,
		string body,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	Task SendText(
		IEnumerable<Receiver> receivers,
		string subject,
		string text,
		CancellationToken cancellationToken = default(CancellationToken)
	);
}