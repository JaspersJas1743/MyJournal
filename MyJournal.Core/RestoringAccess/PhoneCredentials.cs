using MyJournal.Core.Utilities;

namespace MyJournal.Core.RestoringAccess;

public class PhoneCredentials : Credentials<User>
{
	public string? Phone { get; set; }
}