namespace MyJournal.Core.RestoringAccess;

public sealed class VerificationResult(bool isSuccess, string errorMessage)
{
	public bool IsSuccess { get; } = isSuccess;
	public string ErrorMessage { get; } = errorMessage;
}