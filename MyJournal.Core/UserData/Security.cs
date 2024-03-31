using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.UserData;

public sealed class Security(
	AsyncLazy<Phone> phone,
	AsyncLazy<Email> email,
	AsyncLazy<Password> password,
	AsyncLazy<SessionCollection> sessions
)
{
	#region Properties
	internal bool SessionsAreCreated => sessions.IsValueCreated;
	internal bool PhoneIsCreated => phone.IsValueCreated;
	internal bool EmailIsCreated => email.IsValueCreated;
	#endregion

	#region Methods
	public async Task<Phone> GetPhone()
		=> await phone;

	public async Task<Email> GetEmail()
		=> await email;

	public async Task<Password> GetPassword()
		=> await password;

	public async Task<SessionCollection> GetSessions()
		=> await sessions;
	#endregion
}