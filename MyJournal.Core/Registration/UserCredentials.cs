using MyJournal.Core.Utilities;

namespace MyJournal.Core.Registration;

public sealed class UserCredentials: Credentials<User>
{
	public string Login { get; set; } = null!;
	public string Password { get; set; } = null!;
	public string? RegistrationCode { get; set; } = null!;
}