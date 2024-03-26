using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Collections;
using MyJournal.Core.MessageBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Tests;

public class UserTests
{
	#region SetUp
	private ServiceProvider _serviceProvider = null!;

	[SetUp]
	public void Setup()
	{
		ServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddApiClient();
		serviceCollection.AddGoogleAuthenticator();
		serviceCollection.AddFileService();
		serviceCollection.AddTransient<IAuthorizationService<User>, AuthorizationWithCredentialsService>();
		_serviceProvider = serviceCollection.BuildServiceProvider();
	}

	[TearDown]
	public async Task Teardown()
	{
		await _serviceProvider.DisposeAsync();
	}
	#endregion

	#region SessionsTest
	[Test]
	public async Task UserSignOutThisSession_WithOneTry_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		_ = await user.Security.Sessions.CloseThis();
		Assert.Pass();
	}

	[Test]
	public void UserSignOutThisSession_WithTwoTry_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			_ = await user.Security.Sessions.CloseThis();
			_ = await user.Security.Sessions.CloseThis();
		});
	}

	[Test]
	public void UserSignOutAllSession_WithTryingGetDataFromAnySession_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			User user2 = await service2.SignIn(credentials: credentials);
			await user.Security.Sessions.CloseAll();
			await user2.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
		});
	}

	[Test]
	public void UserSignOutOthersSession_WithTryingGetDataFromOtherSession_ShouldThrowException()
	{
		_ = Assert.ThrowsAsync<UnauthorizedAccessException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			IAuthorizationService<User> service2 = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			User user2 = await service2.SignIn(credentials: credentials);
			await user.Security.Sessions.CloseOthers();
			await user2.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
		});
	}

	[Test]
	public async Task UserSignOutOthersSession_WithTryingGetDataFromThisSession_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.Sessions.CloseOthers();
		await user.Photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
	}
	#endregion

	#region ChangeEmail
	[Test]
	public async Task UserChangeEmail_WithCorrectEmail_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		string newEmail = "test@mail.ru";
		await user.Security.Email.Change(confirmationCode: "490679", newEmail: newEmail);
		Assert.That(actual: user.Security.Email.Address, expression: Is.EqualTo(expected: newEmail));
	}

	[Test]
	public void UserChangeEmail_WithUsedEmail_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "490679", newEmail: "test@mail.ru");
		});
	}

	[Test]
	public void UserChangeEmail_WithIncorrectEmailName_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "490679", newEmail: "@mail.ru");
		});
	}

	[Test]
	public void UserChangeEmail_WithIncorrectEmailDomain_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "490679", newEmail: "test@.ru");
		});
	}

	[Test]
	public void UserChangeEmail_WithoutEmailDomain_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "490679", newEmail: "test@");
		});
	}

	[Test]
	public void UserChangeEmail_WithoutAt_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "490679", newEmail: "testmail.ru");
		});
	}

	[Test]
	public void UserChangeEmail_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Email.Change(confirmationCode: "000000", newEmail: "testmail.ru");
		});
	}
	#endregion

	#region ChangePhone
	[Test]
	public async Task UserChangePhone_WithCorrectPhone_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "+7(777)777-7777");
	}

	[Test]
	public void UserChangePhone_WithUsedPhone_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "+7(777)777-7777");
		});
	}

	[Test]
	public void UserChangePhone_WithIncorrectPhone_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "123453");
		});
	}

	[Test]
	public void UserChangePhone_WithIncorrectFormat_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "9999999999");
		});
	}

	[Test]
	public void UserChangePhone_WithoutPlus_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "7(999)999-9999");
		});
	}

	[Test]
	public void UserChangePhone_WithIncorrectFormat2_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "680078", newPhone: "79999999999");
		});
	}

	[Test]
	public void UserChangePhone_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Phone.Change(confirmationCode: "000000", newPhone: "+7(999)999-9999");
		});
	}
	#endregion

	#region ChangePassword
	[Test]
	public async Task UserChangePassword_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Security.Password.Change(confirmationCode: "761545", currentPassword: "JaspersJas1743", newPassword: "Jaspers");
	}

	[Test]
	public void UserChangePassword_WithIncorrectCurrentPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password.Change(confirmationCode: "761545", currentPassword: "Jaspers", newPassword: "JaspersJas1743");
		});
	}

	[Test]
	public void UserChangePassword_WithEqualsCurrentAndNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password.Change(confirmationCode: "761545", currentPassword: "Jaspers", newPassword: "Jaspers");
		});
	}

	[Test]
	public void UserChangePassword_WithIncorrectFormatNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password.Change(confirmationCode: "761545", currentPassword: "Jaspers", newPassword: "J");
		});
	}

	[Test]
	public void UserChangePassword_WithShortNewPassword_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password.Change(confirmationCode: "761545", currentPassword: "Jaspers", newPassword: "Js");
		});
	}

	[Test]
	public void UserChangePassword_WithIncorrectCode_ShouldThrowException()
	{
		Assert.ThrowsAsync<ArgumentException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "Jaspers",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			await user.Security.Password.Change(confirmationCode: "000000", currentPassword: "JaspersJas1743", newPassword: "Js");
		});
	}
	#endregion

	#region Chats
	[Test]
	public async Task UserGetChats_WithNullFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: null);
	}

	[Test]
	public async Task UserGetChats_WithEmptyFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: String.Empty);
	}

	[Test]
	public async Task UserGetChats_WithFilterIsWhiteSpace_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.Chats.SetFilter(filter: "    ");
	}
	#endregion

	#region Interlocutors
	[Test]
	public async Task UserGetInterlocutors_WithNullFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.IntendedInterlocutors.SetFilter(filter: null);
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
	}

	[Test]
	public async Task UserGetInterlocutors_WithEmptyFilter_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.IntendedInterlocutors.SetFilter(filter: String.Empty);
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
	}

	[Test]
	public async Task UserGetInterlocutors_WithFilterIsWhiteSpace_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		await user.IntendedInterlocutors.SetFilter(filter: "   ");
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: user.IntendedInterlocutors[5].PersonalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
	}
	#endregion

	#region Messages
	[Test]
	public async Task UserMessages_WithGetMessages_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		Chat firstChat = user.Chats.First();
		Message lastMessage = firstChat.Messages.Last();
		Assert.That(actual: lastMessage.Sender.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: lastMessage.Sender.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: lastMessage.Sender.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		Assert.That(actual: lastMessage.Text, expression: Is.EqualTo(expected: "Всем привет :)"));
		Assert.That(actual: lastMessage.Attachments?.Count(), expression: Is.EqualTo(expected: 1));
		Attachment? singleAttachment = lastMessage.Attachments?.Single();
		Assert.That(actual: singleAttachment?.Type, expression: Is.EqualTo(expected: Attachment.AttachmentType.Document));
		Assert.That(actual: singleAttachment?.LinkToFile, expression: Is.EqualTo(expected: "https://myjournal_assets.hb.ru-msk.vkcs.cloud/message_attachments/c7394f9f-093e-4784-adad-8b19cff27451.docx"));
	}

	[Test]
	public async Task UserSendMessage_WithGetSentMessage_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		User user = await service.SignIn(credentials: credentials);
		Chat firstChat = user.Chats.First();
		const string message = "Тестирование сообщения";
		IMessageBuilder builder = firstChat.Messages.CreateMessage().WithText(text: message);
		await builder.AddAttachment(pathToFile: @"C:\Users\JaspersJas1743\Downloads\985b88cf-5ee0-496f-98a9-88fb9e35cd32.docx");
		await builder.Build().Send();
		await Task.Delay(millisecondsDelay: 250);
		Message lastMessage = firstChat.Messages.Last();
		Assert.That(actual: lastMessage.Sender.Surname, expression: Is.EqualTo(expected: user.PersonalData.Surname));
		Assert.That(actual: lastMessage.Sender.Name, expression: Is.EqualTo(expected: user.PersonalData.Name));
		Assert.That(actual: lastMessage.Sender.Patronymic, expression: Is.EqualTo(expected: user.PersonalData.Patronymic));
		Assert.That(actual: lastMessage.Text, expression: Is.EqualTo(expected: message));
		Assert.That(actual: lastMessage.Attachments?.Count(), expression: Is.EqualTo(expected: 1));
		Attachment? singleAttachment = lastMessage.Attachments?.Single();
		Assert.That(actual: singleAttachment?.Type, expression: Is.EqualTo(expected: Attachment.AttachmentType.Document));
	}

	[Test]
	public async Task UserSendMessage_WithHighAttachment_ShouldThrowException()
	{
		Assert.ThrowsAsync<ApiException>(code: async () =>
		{
			IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
			UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
				login: "Jaspers",
				password: "JaspersJas1743",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			Chat firstChat = user.Chats.First();
			const string message = "Тестирование сообщения";
			IMessageBuilder builder = firstChat.Messages.CreateMessage().WithText(text: message);
			await builder.AddAttachment(pathToFile: @"C:\Users\JaspersJas1743\Downloads\Telegram Desktop\Внедрение_зависимостей_на_платформе_NET.pdf");
		});
	}
	#endregion

	#region Lessons
	[Test]
	public async Task StudentGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = student.StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstStudyingSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubject secondStudyingSubject = studyingSubjects[index: 1];
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubject thirdStudyingSubject = studyingSubjects[index: 2];
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubject fourStudyingSubject = studyingSubjects[index: 3];
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task TeacherGetTaughtSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection studyingSubjects = teacher.TaughtSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstTaughtSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaughtSubject secondTaughtSubject = teacher.TaughtSubjects[index: 1];
		Assert.That(actual: secondTaughtSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: secondTaughtSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTaughtSubject.Class.Id, expression: Is.EqualTo(expected: 11));
		Assert.That(actual: secondTaughtSubject.Class.Name, expression: Is.EqualTo(expected: "11 класс"));
	}

	[Test]
	public async Task AdministratorGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		StudyingSubjectInClassCollection studyingSubjects = administrator.Classes[index: 10].StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstStudyingSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		StudyingSubjectInClass secondStudyingSubject = studyingSubjects[index: 1];
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		StudyingSubjectInClass thirdStudyingSubject = studyingSubjects[index: 2];
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		StudyingSubjectInClass fourStudyingSubject = studyingSubjects[index: 3];
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}

	[Test]
	public async Task ParentGetStudyingSubjects_WithCorrectData_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test5",
			password: "test5test5",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Parent? parent = await service.SignIn(credentials: credentials) as Parent;
		WardSubjectStudyingCollection studyingSubjects = parent.WardSubjectsStudying;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		WardSubjectStudying firstStudyingSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstStudyingSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		WardSubjectStudying secondStudyingSubject = studyingSubjects[index: 1];
		Assert.That(actual: secondStudyingSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		Assert.That(actual: secondStudyingSubject.Id, expression: Is.EqualTo(expected: 37));
		Assert.That(actual: secondStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: secondStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "Иванов"));
		Assert.That(actual: secondStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "Иван"));
		Assert.That(actual: secondStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "Иванович"));
		WardSubjectStudying thirdStudyingSubject = studyingSubjects[index: 2];
		Assert.That(actual: thirdStudyingSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdStudyingSubject.Id, expression: Is.EqualTo(expected: 47));
		Assert.That(actual: thirdStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 2));
		Assert.That(actual: thirdStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test2"));
		Assert.That(actual: thirdStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test2"));
		WardSubjectStudying fourStudyingSubject = studyingSubjects[index: 3];
		Assert.That(actual: fourStudyingSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: fourStudyingSubject.Id, expression: Is.EqualTo(expected: 72));
		Assert.That(actual: fourStudyingSubject.Teacher.Id, expression: Is.EqualTo(expected: 3));
		Assert.That(actual: fourStudyingSubject.Teacher.Surname, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Name, expression: Is.EqualTo(expected: "test3"));
		Assert.That(actual: fourStudyingSubject.Teacher.Patronymic, expression: Is.EqualTo(expected: "test3"));
	}
	#endregion

	#region Student Tasks
	[Test]
	public async Task StudentGetAssignedTasks_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = student.StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = firstSubject.Tasks;
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		AssignedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		StudyingSubject secondSubject = studyingSubjects[index: 1];
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		AssignedTaskCollection secondStudyingSubjectTasks = secondSubject.Tasks;
		Assert.That(actual: secondStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 0));
		StudyingSubject thirdSubject = studyingSubjects[index: 2];
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		AssignedTaskCollection thirdStudyingSubjectTasks = thirdSubject.Tasks;
		Assert.That(actual: thirdStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 2));
		firstTask = thirdStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		secondTask = thirdStudyingSubjectTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		StudyingSubject fourSubject = studyingSubjects[index: 3];
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		AssignedTaskCollection fourStudyingSubjectTasks = fourSubject.Tasks;
		Assert.That(actual: fourStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 1));
		firstTask = fourStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToUncompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = student.StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Uncompleted);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		AssignedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToCompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = student.StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Completed);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 1));
		AssignedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task StudentGetAssignedTasks_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "Jaspers",
			password: "JaspersJas1743",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Student? student = await service.SignIn(credentials: credentials) as Student;
		StudyingSubjectCollection studyingSubjects = student.StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubject firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		AssignedTaskCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: AssignedTaskCollection.AssignedTaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		AssignedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
		AssignedTask thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: AssignedTask.TaskCompletionStatus.Expired));
	}
	#endregion

	#region Administrator Tasks
	[Test]
	public async Task AdministratorGetTasksAssignedTo11Class_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		StudyingSubjectInClassCollection studyingSubjects = administrator.Classes[index: 10].StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = firstSubject.Tasks;
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToClass firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		TaskAssignedToClass secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaskAssignedToClass thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		StudyingSubjectInClass secondSubject = studyingSubjects[index: 1];
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		TaskAssignedToClassCollection secondStudyingSubjectTasks = secondSubject.Tasks;
		Assert.That(actual: secondStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 0));
		StudyingSubjectInClass thirdSubject = studyingSubjects[index: 2];
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaskAssignedToClassCollection thirdStudyingSubjectTasks = thirdSubject.Tasks;
		Assert.That(actual: thirdStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 2));
		firstTask = thirdStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = thirdStudyingSubjectTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		StudyingSubjectInClass fourSubject = studyingSubjects[index: 3];
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		TaskAssignedToClassCollection fourStudyingSubjectTasks = fourSubject.Tasks;
		Assert.That(actual: fourStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 1));
		firstTask = fourStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}

	[Test]
	public async Task AdministratorGetAssignedTasksTo11Class_WithChangeStatusToNotExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		StudyingSubjectInClassCollection studyingSubjects = administrator.Classes[index: 10].StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: TaskAssignedToClassCollection.TaskCompletionStatus.NotExpired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task AdministratorGetAssignedTasksTo11Class_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test4",
			password: "test4test4",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Administrator? administrator = await service.SignIn(credentials: credentials) as Administrator;
		StudyingSubjectInClassCollection studyingSubjects = administrator.Classes[index: 10].StudyingSubjects;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		StudyingSubjectInClass firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToClassCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: TaskAssignedToClassCollection.TaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToClass firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		TaskAssignedToClass secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaskAssignedToClass thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}
	#endregion

	#region Teacher Tasks
	[Test]
	public async Task TeacherGetCreatedTasks_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = teacher.TaughtSubjects;
		Assert.That(actual: taughtSubjects.Length, expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = taughtSubjects[index: 0];
		CreatedTaskCollection allTasks = allSubjects.Tasks;
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		CreatedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		CreatedTask secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = taughtSubjects[index: 0];
		CreatedTaskCollection firstTaughtSubjectTasks = firstTaughtSubject.Tasks;
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		firstTask = firstTaughtSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = firstTaughtSubjectTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithChangeStatusToNotExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = teacher.TaughtSubjects;
		Assert.That(actual: taughtSubjects.Length, expression: Is.EqualTo(expected: 2));
		TaughtSubject singleSubject = taughtSubjects[index: 0];
		CreatedTaskCollection allTasks = singleSubject.Tasks;
		await allTasks.SetCompletionStatus(status: CreatedTaskCollection.TaskCompletionStatus.NotExpired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 0));
	}

	[Test]
	public async Task TeacherGetCreatedTasks_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test2",
			password: "test2test2",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Teacher? teacher = await service.SignIn(credentials: credentials) as Teacher;
		TaughtSubjectCollection taughtSubjects = teacher.TaughtSubjects;
		Assert.That(actual: taughtSubjects.Length, expression: Is.EqualTo(expected: 2));
		TaughtSubject allSubjects = taughtSubjects[index: 0];
		CreatedTaskCollection allTasks = allSubjects.Tasks;
		await allTasks.SetCompletionStatus(status: CreatedTaskCollection.TaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		CreatedTask firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		CreatedTask secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));
		TaughtSubject firstTaughtSubject = taughtSubjects[index: 0];
		CreatedTaskCollection firstTaughtSubjectTasks = firstTaughtSubject.Tasks;
		Assert.That(actual: firstTaughtSubjectTasks.Length, expression: Is.EqualTo(expected: 2));
		firstTask = firstTaughtSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 1));
		Assert.That(actual: firstTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 1));
		secondTask = firstTaughtSubjectTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ClassName, expression: Is.EqualTo(expected: "11 класс"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfCompletedTask, expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CountOfUncompletedTask, expression: Is.EqualTo(expected: 2));	}
	#endregion

	#region Parent Ward Tasks
	[Test]
	public async Task ParentGetTasksAssignedToWard_WithDefaultValues_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test5",
			password: "test5test5",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Parent? parent = await service.SignIn(credentials: credentials) as Parent;
		WardSubjectStudyingCollection wardStudyingSubjects = parent.WardSubjectsStudying;
		Assert.That(actual: wardStudyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		WardSubjectStudying firstSubject = wardStudyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToWardCollection allTasks = firstSubject.Tasks;
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToWard firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		TaskAssignedToWard secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		TaskAssignedToWard thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		WardSubjectStudying secondSubject = wardStudyingSubjects[index: 1];
		Assert.That(actual: secondSubject.Name, expression: Is.EqualTo(expected: "Русский язык"));
		TaskAssignedToWardCollection secondStudyingSubjectTasks = secondSubject.Tasks;
		Assert.That(actual: secondStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 0));
		WardSubjectStudying thirdSubject = wardStudyingSubjects[index: 2];
		Assert.That(actual: thirdSubject.Name, expression: Is.EqualTo(expected: "Физическая культура"));
		TaskAssignedToWardCollection thirdStudyingSubjectTasks = thirdSubject.Tasks;
		Assert.That(actual: thirdStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 2));
		firstTask = thirdStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		secondTask = thirdStudyingSubjectTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		WardSubjectStudying fourSubject = wardStudyingSubjects[index: 3];
		Assert.That(actual: fourSubject.Name, expression: Is.EqualTo(expected: "Проектная деятельность"));
		TaskAssignedToWardCollection fourStudyingSubjectTasks = fourSubject.Tasks;
		Assert.That(actual: fourStudyingSubjectTasks.Count(), expression: Is.EqualTo(expected: 1));
		firstTask = fourStudyingSubjectTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task ParentGetTasksAssignedToWard_WithChangeStatusToUncompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test5",
			password: "test5test5",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Parent? parent = await service.SignIn(credentials: credentials) as Parent;
		WardSubjectStudyingCollection studyingSubjects = parent.WardSubjectsStudying;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		WardSubjectStudying firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToWardCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: TaskAssignedToWardCollection.AssignedTaskCompletionStatus.Uncompleted);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 2));
		TaskAssignedToWard firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		TaskAssignedToWard secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task ParentGetTasksAssignedToWard_WithChangeStatusToCompleted_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test5",
			password: "test5test5",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Parent? parent = await service.SignIn(credentials: credentials) as Parent;
		WardSubjectStudyingCollection studyingSubjects = parent.WardSubjectsStudying;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		WardSubjectStudying firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToWardCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: TaskAssignedToWardCollection.AssignedTaskCompletionStatus.Completed);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 1));
		TaskAssignedToWard firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
	}

	[Test]
	public async Task ParentGetTasksAssignedToWard_WithChangeStatusToExpired_ShouldPassed()
	{
		IAuthorizationService<User> service = _serviceProvider.GetService<IAuthorizationService<User>>()!;
		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
			login: "test5",
			password: "test5test5",
			client: UserAuthorizationCredentials.Clients.Windows
		);
		Parent? parent = await service.SignIn(credentials: credentials) as Parent;
		WardSubjectStudyingCollection studyingSubjects = parent.WardSubjectsStudying;
		Assert.That(actual: studyingSubjects.Length, expression: Is.EqualTo(expected: 4));
		WardSubjectStudying firstSubject = studyingSubjects[index: 0];
		Assert.That(actual: firstSubject.Name, expression: Is.EqualTo(expected: "Все дисциплины"));
		TaskAssignedToWardCollection allTasks = firstSubject.Tasks;
		await allTasks.SetCompletionStatus(status: TaskAssignedToWardCollection.AssignedTaskCompletionStatus.Expired);
		Assert.That(actual: allTasks.Length, expression: Is.EqualTo(expected: 3));
		TaskAssignedToWard firstTask = allTasks.ElementAt(index: 0);
		Assert.That(actual: firstTask.Id, expression: Is.EqualTo(expected: 5));
		Assert.That(actual: firstTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: firstTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: firstTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: firstTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: firstTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		TaskAssignedToWard secondTask = allTasks.ElementAt(index: 1);
		Assert.That(actual: secondTask.Id, expression: Is.EqualTo(expected: 6));
		Assert.That(actual: secondTask.LessonName, expression: Is.EqualTo(expected: "Проектная деятельность"));
		Assert.That(actual: secondTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-25T12:00:00")));
		Assert.That(actual: secondTask.Content.Text, expression: Is.EqualTo(expected: "Тестирование"));
		Assert.That(actual: secondTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: secondTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
		TaskAssignedToWard thirdTask = allTasks.ElementAt(index: 2);
		Assert.That(actual: thirdTask.Id, expression: Is.EqualTo(expected: 7));
		Assert.That(actual: thirdTask.LessonName, expression: Is.EqualTo(expected: "Физическая культура"));
		Assert.That(actual: thirdTask.ReleasedAt, expression: Is.EqualTo(expected: DateTime.Parse(s: "2024-03-22T12:00:00")));
		Assert.That(actual: thirdTask.Content.Text, expression: Is.EqualTo(expected: "Тестовая задача"));
		Assert.That(actual: thirdTask.Content.Attachments?.Count(), expression: Is.EqualTo(expected: 0));
		Assert.That(actual: thirdTask.CompletionStatus, expression: Is.EqualTo(expected: TaskAssignedToWard.TaskCompletionStatus.Expired));
	}
	#endregion
}