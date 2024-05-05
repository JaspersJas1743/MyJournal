using System;
using System.Threading.Tasks;
using MyJournal.Core.SubEntities;
using MyJournal.Core.UserData;
using ReactiveUI;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public sealed class ExtendedInterlocutor : ReactiveObject
{
	private string _fullName = String.Empty;
	private string? _photo = String.Empty;

	public int UserId { get; set; }

	public string FullName
	{
		get => _fullName;
		set => this.RaiseAndSetIfChanged(backingField: ref _fullName, newValue: value);
	}

	public string? Photo
	{
		get => _photo;
		set => this.RaiseAndSetIfChanged(backingField: ref _photo, newValue: value);
	}
}

public static class InterlocutorExtensions
{
	public static async Task<ExtendedInterlocutor> ToExtended(this IntendedInterlocutor interlocutor)
	{
		ProfilePhoto? profilePhoto = await interlocutor.GetPhoto();
		PersonalData personalData = await interlocutor.GetPersonalData();
		return new ExtendedInterlocutor()
		{
			UserId = interlocutor.Id,
			Photo = profilePhoto?.Link,
			FullName = $"{personalData.Surname} {personalData.Name}"
		};
	}
}