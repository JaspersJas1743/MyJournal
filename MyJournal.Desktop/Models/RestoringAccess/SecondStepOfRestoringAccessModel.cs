using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyJournal.Core;
using MyJournal.Core.RestoringAccess;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace MyJournal.Desktop.Models.RestoringAccess;

public class SecondStepOfRestoringAccessModel : ModelWithErrorMessage
{
	private string _entryCode = String.Empty;

	public SecondStepOfRestoringAccessModel()
	{
		ToNextStep = ReactiveCommand.CreateFromTask(execute: MoveToNextStep, canExecute: ValidationContext.Valid);
	}

	public IRestoringAccessService<User> RestoringAccessService { get; set; }

	public string EntryCode
	{
		get => _entryCode;
		set => this.RaiseAndSetIfChanged(backingField: ref _entryCode, newValue: value);
	}

	public int CountOfCell => 6;

	public ReactiveCommand<Unit, Unit> ToNextStep { get; }

	public async Task MoveToNextStep()
	{
		HaveError = !await RestoringAccessService.VerifyAuthenticationCode(code: EntryCode);
		if (HaveError)
			Observable.Timer(dueTime: TimeSpan.FromSeconds(value: 3)).Subscribe(onNext: _ => HaveError = false);
		else
		{
			// MessageBus.Current.SendMessage(message: new ChangeWelcomeVMContentEventArgs(
			// 	newVMType: typeof(),
			// 	directionOfTransitionAnimation: PageTransition.Direction.Left
			// ));
		}
	}

	protected override void SetValidationRule()
	{
		this.ValidationRule(
			viewModelProperty: model => model.EntryCode,
			isPropertyValid: code => code?.Length == CountOfCell,
			message: "Регистрационный код имеет некорректный формат."
		);
	}
}