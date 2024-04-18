using System;
using DynamicData.Binding;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Authorization;
using MyJournal.Desktop.ViewModels.Registration;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class WelcomeModel : ModelBase
{
	private bool _haveLeftDirection;
	private bool _haveRightDirection;
	private BaseVM _content;

	public WelcomeModel(AuthorizationVM authorizationVM)
	{
		authorizationVM.Presenter = this;
		Content = authorizationVM;
		this.WhenValueChanged(propertyAccessor: model => model.Content)
			.Subscribe(onNext: content =>
			{
				HaveLeftDirection = content is FirstStepOfRegistrationVM { HasMoveToNextStep: false };
				HaveRightDirection = !HaveLeftDirection;
			});
	}

	public BaseVM Content
	{
		get => _content;
		set => this.RaiseAndSetIfChanged(backingField: ref _content, newValue: value);
	}

	public bool HaveLeftDirection
	{
		get => _haveLeftDirection;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveLeftDirection, newValue: value);
	}

	public bool HaveRightDirection
	{
		get => _haveRightDirection;
		set => this.RaiseAndSetIfChanged(backingField: ref _haveRightDirection, newValue: value);
	}
}