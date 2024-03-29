using Microsoft.Extensions.DependencyInjection;
using MyJournal.Core;
using MyJournal.Core.Authorization;
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
}