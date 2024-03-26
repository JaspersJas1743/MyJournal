using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.TaskBuilder;

internal sealed class InitTaskBuilder : IInitTaskBuilder
{
	private readonly IFileService _fileService;
	private readonly int _classId;
	private readonly int _subjectId;

	private InitTaskBuilder(
		IFileService fileService
	) => _fileService = fileService;

	internal static InitTaskBuilder Create(
		IFileService fileService
	) => new InitTaskBuilder(fileService: fileService);

	public ITaskBuilder WithText(string text)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(value: text),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: DateTime.Today,
			classId: 0,
			subjectId: 0
		);
	}

	public async Task<ITaskBuilder> WithAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken))
	{
		Attachment attachment = await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		);
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: new Attachment[] { attachment },
			releasedAt: DateTime.Today,
			classId: 0,
			subjectId: 0
		);
	}

	public ITaskBuilder WithReleaseDate(DateTime dateOfRelease)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: dateOfRelease,
			classId: 0,
			subjectId: 0
		);
	}

	public ITaskBuilder ForClass(int classId)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: DateTime.Today,
			classId: classId,
			subjectId: 0
		);
	}

	public ITaskBuilder ForClass(Class @class)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: DateTime.Today,
			classId: @class.Id,
			subjectId: 0
		);
	}

	public ITaskBuilder ForSubject(int subjectId)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: DateTime.Today,
			classId: 0,
			subjectId: subjectId
		);
	}

	public ITaskBuilder ForSubject(Subject subject)
	{
		return TaskBuilder.Create(
			fileService: _fileService,
			builder: new StringBuilder(),
			attachments: Enumerable.Empty<Attachment>(),
			releasedAt: DateTime.Today,
			classId: 0,
			subjectId: subject.Id
		);
	}
}