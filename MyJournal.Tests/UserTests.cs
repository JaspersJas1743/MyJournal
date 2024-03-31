using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Core.Collections;
using MyJournal.Core.MessageBuilder;
using MyJournal.Core.SubEntities;
using MyJournal.Core.UserData;
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
		Security security = await user.GetSecurity();
		SessionCollection sessions = await security.GetSessions();
		_ = await sessions.CloseThis();
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
			Security security = await user.GetSecurity();
			SessionCollection sessions = await security.GetSessions();
			_ = await sessions.CloseThis();
			_ = await sessions.CloseThis();
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
			Security security = await user.GetSecurity();
			SessionCollection sessions = await security.GetSessions();
			_ = await sessions.CloseAll();
			ProfilePhoto photo = await user2.GetPhoto();
			await photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
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
			Security security = await user.GetSecurity();
			SessionCollection sessions = await security.GetSessions();
			_ = await sessions.CloseOthers();
			ProfilePhoto photo = await user2.GetPhoto();
			await photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
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
		Security security = await user.GetSecurity();
		SessionCollection sessions = await security.GetSessions();
		_ = await sessions.CloseOthers();
		ProfilePhoto photo = await user.GetPhoto();
		await photo.Download(folder: @"C:\Users\JaspersJas1743\Downloads");
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
		Security security = await user.GetSecurity();
		Email email = await security.GetEmail();
		await email.Change(confirmationCode: "658228", newEmail: newEmail);
		Assert.That(actual: email.Address, expression: Is.EqualTo(expected: newEmail));
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "658228", newEmail: "test@mail.ru");
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "658228", newEmail: "@mail.ru");
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "658228", newEmail: "test@.ru");
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "658228", newEmail: "test@");
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "658228", newEmail: "testmail.ru");
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
			Security security = await user.GetSecurity();
			Email email = await security.GetEmail();
			await email.Change(confirmationCode: "000000", newEmail: "testmail.ru");
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
		Security security = await user.GetSecurity();
		Phone phone = await security.GetPhone();
		await phone.Change(confirmationCode: "025134", newPhone: "+7(777)777-7777");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "025134", newPhone: "+7(777)777-7777");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "025134", newPhone: "123453");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "025134", newPhone: "9999999999");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "025134", newPhone: "7(999)999-9999");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "025134", newPhone: "79999999999");
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
			Security security = await user.GetSecurity();
			Phone phone = await security.GetPhone();
			await phone.Change(confirmationCode: "000000", newPhone: "+7(999)999-9999");
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
		Security security = await user.GetSecurity();
		Password password = await security.GetPassword();
		await password.Change(confirmationCode: "248771", currentPassword: "JaspersJas1743", newPassword: "Jaspers");
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
			Security security = await user.GetSecurity();
			Password password = await security.GetPassword();
			await password.Change(confirmationCode: "248771", currentPassword: "Jaspers", newPassword: "JaspersJas1743");
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
				password: "Jaspers",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			Security security = await user.GetSecurity();
			Password password = await security.GetPassword();
			await password.Change(confirmationCode: "248771", currentPassword: "Jaspers", newPassword: "Jaspers");
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
				password: "Jaspers",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			Security security = await user.GetSecurity();
			Password password = await security.GetPassword();
			await password.Change(confirmationCode: "248771", currentPassword: "Jaspers", newPassword: "J");
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
				password: "Jaspers",
				client: UserAuthorizationCredentials.Clients.Windows
			);
			User user = await service.SignIn(credentials: credentials);
			Security security = await user.GetSecurity();
			Password password = await security.GetPassword();
			await password.Change(confirmationCode: "248771", currentPassword: "Jaspers", newPassword: "Js");
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
			Security security = await user.GetSecurity();
			Password password = await security.GetPassword();
			await password.Change(confirmationCode: "000000", currentPassword: "JaspersJas1743", newPassword: "Js");
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
		ChatCollection chats = await user.GetChats();
		await chats.SetFilter(filter: null);
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
		ChatCollection chats = await user.GetChats();
		await chats.SetFilter(filter: String.Empty);
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
		ChatCollection chats = await user.GetChats();
		await chats.SetFilter(filter: "    ");
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
		IntendedInterlocutorCollection intendedInterlocutors = await user.GetIntendedInterlocutors();
		await intendedInterlocutors.SetFilter(filter: null);
		IntendedInterlocutor intendedInterlocutorWithFiveId = await intendedInterlocutors.FindById(id: 5);
		PersonalData personalData = await intendedInterlocutorWithFiveId.GetPersonalData();
		Assert.That(actual: personalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: personalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: personalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
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
		IntendedInterlocutorCollection intendedInterlocutors = await user.GetIntendedInterlocutors();
		await intendedInterlocutors.SetFilter(filter: String.Empty);
		IntendedInterlocutor intendedInterlocutorWithFiveId = await intendedInterlocutors.FindById(id: 5);
		PersonalData personalData = await intendedInterlocutorWithFiveId.GetPersonalData();
		Assert.That(actual: personalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: personalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: personalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
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
		IntendedInterlocutorCollection intendedInterlocutors = await user.GetIntendedInterlocutors();
		await intendedInterlocutors.SetFilter(filter: "   ");
		IntendedInterlocutor intendedInterlocutorWithFiveId = await intendedInterlocutors.FindById(id: 5);
		PersonalData personalData = await intendedInterlocutorWithFiveId.GetPersonalData();
		Assert.That(actual: personalData.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: personalData.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: personalData.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
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
		ChatCollection chats = await user.GetChats();
		Chat firstChat = await chats.FirstAsync();
		MessageCollection messages = await firstChat.GetMessages();
		Message lastMessage = await messages.LastAsync();
		Assert.That(actual: lastMessage.Sender.Surname, expression: Is.EqualTo(expected: "Смирнов"));
		Assert.That(actual: lastMessage.Sender.Name, expression: Is.EqualTo(expected: "Алексей"));
		Assert.That(actual: lastMessage.Sender.Patronymic, expression: Is.EqualTo(expected: "Игоревич"));
		Assert.That(actual: lastMessage.Text, expression: Is.EqualTo(expected: "Тестирование сообщения №1"));
		Assert.That(actual: lastMessage.Attachments?.Count(), expression: Is.EqualTo(expected: 1));
		Attachment? singleAttachment = lastMessage.Attachments?.Single();
		Assert.That(actual: singleAttachment?.Type, expression: Is.EqualTo(expected: Attachment.AttachmentType.Document));
		Assert.That(actual: singleAttachment?.LinkToFile, expression: Is.EqualTo(expected: "https://myjournal_assets.hb.ru-msk.vkcs.cloud/message_attachments/6a801ed1-fce8-4e1a-87d4-a4432ffdbd1a.docx"));
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
		ChatCollection chats = await user.GetChats();
		Chat firstChat = await chats.FirstAsync();
		const string message = "Тестирование сообщения :)";
		MessageCollection messages = await firstChat.GetMessages();
		IMessageBuilder builder = messages.CreateMessage().WithText(text: message);
		await builder.AddAttachment(pathToFile: @"C:\Users\JaspersJas1743\Downloads\985b88cf-5ee0-496f-98a9-88fb9e35cd32.docx");
		await builder.Build().Send();
		await Task.Delay(millisecondsDelay: 50);
		messages = await firstChat.GetMessages();
		Message lastMessage = await messages.LastAsync();
		PersonalData personalData = await user.GetPersonalData();
		Assert.That(actual: lastMessage.Sender.Surname, expression: Is.EqualTo(expected: personalData.Surname));
		Assert.That(actual: lastMessage.Sender.Name, expression: Is.EqualTo(expected: personalData.Name));
		Assert.That(actual: lastMessage.Sender.Patronymic, expression: Is.EqualTo(expected: personalData.Patronymic));
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
			ChatCollection chats = await user.GetChats();
			Chat firstChat = await chats.FirstAsync();
			const string message = "Тестирование сообщения";
			MessageCollection messages = await firstChat.GetMessages();
			IMessageBuilder builder = messages.CreateMessage().WithText(text: message);
			await builder.AddAttachment(pathToFile: @"C:\Users\JaspersJas1743\Downloads\Telegram Desktop\Внедрение_зависимостей_на_платформе_NET.pdf");
		});
	}
	#endregion
}