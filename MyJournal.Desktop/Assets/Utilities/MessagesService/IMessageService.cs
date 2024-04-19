using System.Threading.Tasks;
using MsBox.Avalonia.Enums;

namespace MyJournal.Desktop.Assets.Utilities.MessagesService;

public interface IMessageService
{
	Task<ButtonResult> ShowWindow(string text, string title, ButtonEnum buttons, Icon image);
	Task<ButtonResult> ShowErrorWindow(string text);
	Task<ButtonResult> ShowWarningWindow(string text);
	Task<ButtonResult> ShowInformationWindow(string text);
	Task<ButtonResult> ShowMessageWindow(string text);

	Task<ButtonResult> ShowPopup(string text, string title, ButtonEnum buttons, Icon image);
	Task<ButtonResult> ShowErrorAsPopup(string text);
	Task<ButtonResult> ShowWarningAsPopup(string text);
	Task<ButtonResult> ShowInformationAsPopup(string text);
	Task<ButtonResult> ShowMessageAsPopup(string text);

	Task<ButtonResult> ShowDialog(string text, string title, ButtonEnum buttons, Icon image);
}
