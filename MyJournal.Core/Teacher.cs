using MyJournal.Core.Collections;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.FileService;
using MyJournal.Core.Utilities.GoogleAuthenticatorService;

namespace MyJournal.Core;

public sealed class Teacher : User
{
	private readonly Lazy<TaughtSubjectCollection> _toughtSubjectCollection;

	private Teacher(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		UserInformationResponse information,
		Lazy<ChatCollection> chats,
		Lazy<InterlocutorCollection> interlocutors,
		Lazy<IntendedInterlocutorCollection> intendedInterlocutors,
		Lazy<SessionCollection> sessions,
		Lazy<TaughtSubjectCollection> taughtSubjects
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
		_toughtSubjectCollection = taughtSubjects;
	}

	public TaughtSubjectCollection TaughtSubjects => _toughtSubjectCollection.Value;

	internal static async Task<Teacher> Create(
		ApiClient client,
		IFileService fileService,
		IGoogleAuthenticatorService googleAuthenticatorService,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		UserInformationResponse information = await GetUserInformation(client: client, cancellationToken: cancellationToken);
		Teacher teacher = new Teacher(
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
			taughtSubjects: new Lazy<TaughtSubjectCollection>(value: await TaughtSubjectCollection.Create(
				client: client,
				fileService: fileService,
				cancellationToken: cancellationToken
			))
		);
		await teacher.ConnectToUserHub(cancellationToken: cancellationToken);
		return teacher;
	}
}