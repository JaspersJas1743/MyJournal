using MyJournal.Core.Collections;

namespace MyJournal.Core.UserData;

public sealed class Security(
	Phone phone,
	Email email,
	Password password,
	SessionCollection sessions
)
{
	#region Properties
	public Phone Phone { get; init; } = phone;
	public Email Email { get; init; } = email;
	public Password Password { get; init; } = password;
	public SessionCollection Sessions { get; init; } = sessions;
	#endregion
}