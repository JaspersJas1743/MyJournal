using MyJournal.Core.Collections;

namespace MyJournal.Core.UserData;

public sealed class Security(
	Lazy<Phone> phone,
	Lazy<Email> email,
	Lazy<Password> password,
	Lazy<SessionCollection> sessions
)
{
	#region Properties
	public Phone Phone => phone.Value;
	public Email Email => email.Value;
	public Password Password => password.Value;
	public SessionCollection Sessions => sessions.Value;
	#endregion
}