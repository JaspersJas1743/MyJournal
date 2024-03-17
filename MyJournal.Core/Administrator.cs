using MyJournal.Core.Utilities;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Administrator : User
{
	private Administrator(
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

	public static async Task<Administrator> Create(
		ApiClient client,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		Administrator administrator = new Administrator(
			client: client,
			googleAuthenticatorService: googleAuthenticatorService,
			information: await GetUserInformation(client: client, cancellationToken: cancellationToken),
			chats: await ChatCollection.Create(client: client, cancellationToken: cancellationToken),
			interlocutors: await InterlocutorCollection.Create(client: client, cancellationToken: cancellationToken)
		);
		await administrator.ConnectToUserHub(cancellationToken: cancellationToken);
		return administrator;
	}
}