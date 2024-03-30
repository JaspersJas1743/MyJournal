using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.UserData;

public sealed class Security(
	Lazy<Phone> phone,
	Lazy<Email> email,
	Lazy<Password> password,
	AsyncLazy<SessionCollection> sessions
)
{
	#region Properties
	public Phone Phone => phone.Value;
	public Email Email => email.Value;
	public Password Password => password.Value;
	public async Task<SessionCollection> GetSessions()
		=> await sessions;

	internal bool SessionsAreCreated => sessions.IsValueCreated;
	#endregion
}