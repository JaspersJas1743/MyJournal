using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Administrator : User
{
	private readonly Lazy<ClassCollection> _classes;

	private Administrator(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		Lazy<ChatCollection> chats,
		Lazy<InterlocutorCollection> interlocutors,
		Lazy<IntendedInterlocutorCollection> intendedInterlocutors,
		Lazy<SessionCollection> sessions,
		Lazy<ClassCollection> classes
	) : base(
		client: client,
		fileService: fileService,
		googleAuthenticatorService: googleAuthenticatorService,
		information: information,
		chats: chats,
		interlocutors: interlocutors,
		intendedInterlocutors: intendedInterlocutors,
		sessions: sessions
	)
	{
		_classes = classes;
	}

	public ClassCollection Classes => _classes.Value;

	internal static async Task<Administrator> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Administrator administrator = new Administrator(
			client: client,
			fileService: fileService,
			googleAuthenticatorService: googleAuthenticatorService,
			information: information,
			chats: new Lazy<ChatCollection>(value: await ChatCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			interlocutors: new Lazy<InterlocutorCollection>(value: await InterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			intendedInterlocutors: new Lazy<IntendedInterlocutorCollection>(value: await IntendedInterlocutorCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			)),
			sessions: new Lazy<SessionCollection>(value: await SessionCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			)),
			classes: new Lazy<ClassCollection>(value: await ClassCollection.Create(
				client: client,
				cancellationToken: cancellationToken
			))
		);
		await administrator.ConnectToUserHub(cancellationToken: cancellationToken);
		return administrator;
	}
}