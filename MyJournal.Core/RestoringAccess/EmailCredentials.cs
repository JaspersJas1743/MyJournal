using MyJournal.Core.Utilities;

namespace MyJournal.Core.RestoringAccess;

public class EmailCredentials : Credentials<User>
{
	public string? Email { get; set; }
}