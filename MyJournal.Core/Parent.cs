using MyJournal.Core.Chats;
using MyJournal.Core.Interlocutors;
using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Parent : User
{
	private Parent(
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

	internal static async Task<Parent> Create(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Parent parent = new Parent(
			client: client,
			googleAuthenticatorService: googleAuthenticatorService,
			information: information,
			chats: await ChatCollection.Create(client: client, cancellationToken: cancellationToken),
			interlocutors: await InterlocutorCollection.Create(client: client, cancellationToken: cancellationToken)
		);
		await parent.ConnectToUserHub(cancellationToken: cancellationToken);
		return parent;
	}
}