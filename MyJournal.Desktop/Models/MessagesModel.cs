using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.Collections;
using MyJournal.Core.SubEntities;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public sealed class MessagesModel : ModelBase
{
	private User _user;

	private ObservableCollectionExtended<Chat> _chats = new ObservableCollectionExtended<Chat>();

	public ObservableCollectionExtended<Chat> Chats
	{
		get => _chats;
		set => this.RaiseAndSetIfChanged(backingField: ref _chats, newValue: value);
	}

	public async Task SetUser(User user)
	{
		_user = user;
		ChatCollection chats = await _user.GetChats();
		Chats.Load(items: await chats.ToListAsync());
	}
}