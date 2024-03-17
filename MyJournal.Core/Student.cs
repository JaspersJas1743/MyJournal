using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Student : User
{
	private Student(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		ChatCollection chats,
		InterlocutorCollection interlocutors
	) : base(
		client: client,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information,
		chats: chats,
		interlocutors: interlocutors
	) { }

	public static async Task<Student> Create(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Student student = new Student(
			client: client,
			googleAuthenticatorService: googleAuthenticatorService,
			information: await GetUserInformation(client: client, cancellationToken: cancellationToken),
			chats: await ChatCollection.Create(client: client, cancellationToken: cancellationToken),
			interlocutors: await InterlocutorCollection.Create(client: client, cancellationToken: cancellationToken)
		);
		await student.ConnectToUserHub(cancellationToken: cancellationToken);
		return student;
	}
}