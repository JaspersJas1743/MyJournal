namespace MyJournal.Desktop.Assets.Utilities;

public enum CommandExecuteResults
{
	Confirmed,
	Unconfirmed,
	Wrong
}

public sealed record CommandExecuteResult(CommandExecuteResults ExecuteResult, string Message);