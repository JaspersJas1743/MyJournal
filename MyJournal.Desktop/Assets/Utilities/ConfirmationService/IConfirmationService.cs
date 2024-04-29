using System.Threading.Tasks;
using MyJournal.Desktop.Views.ConfirmationCode;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ConfirmationService;

public interface IConfirmationService
{
	public static ConfirmationCodeWindow? Instance { get; protected set; }

	Task Сonfirm(string text, ReactiveCommand<string, CommandExecuteResult>? command);
}