using System.Text;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.TaskBuilder;

internal sealed class TaskBuilder : ITaskBuilder
{
	private readonly StringBuilder _text = new StringBuilder();
	private readonly List<Attachment> _attachments = new List<Attachment>();
	private readonly IFileService _fileService;
	private DateTime _releasedAt;
	private int _classId;
	private int _subjectId;

	private TaskBuilder(
		IFileService fileService,
		StringBuilder builder,
		IEnumerable<Attachment> attachments,
		DateTime releasedAt,
		int classId,
		int subjectId
	)
	{
		_text.Append(value: builder);
		_attachments.AddRange(collection: attachments);
		_fileService = fileService;
		_releasedAt = releasedAt;
		_classId = classId;
		_subjectId = subjectId;
	}

	internal static TaskBuilder Create(
		IFileService fileService,
		StringBuilder builder,
		IEnumerable<Attachment> attachments,
		DateTime releasedAt,
		int classId,
		int subjectId
	)
	{
		return new TaskBuilder(
			fileService: fileService,
			builder: builder,
			attachments: attachments,
			releasedAt: releasedAt,
			classId: classId,
			subjectId: subjectId
		);
	}

	public ITaskBuilder AddText(string text)
	{
		_text.Append(value: text);
		return this;
	}

	public async Task<ITaskBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken))
	{
		_attachments.Add(item: await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		));
		return this;
	}

	public async Task<ITaskBuilder> RemoveAttachment(int index, CancellationToken cancellationToken = default(CancellationToken))
	{
		Attachment attachment = _attachments[index: index];
		await _fileService.Delete(link: attachment.LinkToFile, cancellationToken: cancellationToken);
		_attachments.Remove(attachment);
		return this;
	}

	public ITaskBuilder AddReleaseDate(DateTime dateOfRelease)
	{
		_releasedAt = dateOfRelease;
		return this;
	}

	public ITaskBuilder ForClass(int classId)
	{
		_classId = classId;
		return this;
	}

	public ITaskBuilder ForClass(Class @class)
	{
		_classId = @class.Id;
		return this;
	}

	public ITaskBuilder ForSubject(int subjectId)
	{
		_subjectId = subjectId;
		return this;
	}

	public ITaskBuilder ForSubject(Subject subject)
	{
		_subjectId = subject.Id;
		return this;
	}

	public ITaskPreserver Build()
	{
		if (_classId == 0)
			throw new ArgumentException(message: "Не указан класс, для которого создается задача.", paramName: nameof(_classId));

		if (_subjectId == 0)
			throw new ArgumentException(message: "Не указана дисциплина, по которого создается задача.", paramName: nameof(_subjectId));

		return TaskPreserver.Create(
			client: _fileService.ApiClient,
			content: new TaskContent(
				Text: _text.ToString(),
				Attachments: _attachments.Select(selector: a => new TaskAttachment(
					LinkToFile: a.LinkToFile!,
					AttachmentType: a.Type
				))
			),
			subjectId: _subjectId,
			classId: _classId,
			releasedAt: _releasedAt
		);
	}
}