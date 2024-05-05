using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Desktop.Views.ChatCreation;

namespace MyJournal.Desktop.Assets.Utilities.ChatCreationService;

public interface IChatCreationService
{
	public static ChatCreationWindow? Instance { get; protected set; }

	Task<bool> Create(User user);
}