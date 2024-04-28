using System.Reactive;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyJournal.Core;
using MyJournal.Core.SubEntities;
using MyJournal.Desktop.Models.Profile;
using ReactiveUI;

namespace MyJournal.Desktop.ViewModels.Profile;

public sealed class ProfileSessionsVM(ProfileSessionsModel model) : MenuItemVM(model: model)
{
	public ObservableCollectionExtended<Session> Sessions
	{
		get => model.Sessions;
		set => model.Sessions = value;
	}

	public ReactiveCommand<Unit, Unit> CloseAllSessions => model.CloseAllSessions;
	public ReactiveCommand<Unit, Unit> CloseOtherSessions => model.CloseOtherSessions;

	public override async Task SetUser(User user)
		=> await model.SetUser(user: user);
}