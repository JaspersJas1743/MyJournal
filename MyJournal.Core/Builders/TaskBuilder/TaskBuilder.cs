using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Constants.Controllers;
using MyJournal.Core.Utilities.FileService;

namespace MyJournal.Core.Builders.TaskBuilder;

internal sealed class TaskBuilder : ITaskBuilder
{
	private readonly Dictionary<string, Attachment> _attachments = new Dictionary<string, Attachment>();
	private string? _text = null;
	private readonly IFileService _fileService;
	private DateTime _releasedAt = DateTime.Now;
	private int _classId = 0;
	private int _subjectId = 0;

	private TaskBuilder(IFileService fileService)
		=> _fileService = fileService;

	internal static TaskBuilder Create(
		IFileService fileService
	) => new TaskBuilder(fileService: fileService);

	public ITaskBuilder SetText(string? text)
	{
		_text = text;
		return this;
	}

	public async Task<ITaskBuilder> AddAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_attachments.ContainsKey(key: pathToFile))
			throw new ArgumentException(message: "Файл уже загружен!");

		_attachments.Add(key: pathToFile, value: await Attachment.Create(
			fileService: _fileService,
			pathToFile: pathToFile,
			cancellationToken: cancellationToken
		));
		return this;
	}

	public async Task<ITaskBuilder> RemoveAttachment(string pathToFile, CancellationToken cancellationToken = default(CancellationToken))
	{
		Attachment attachment = _attachments[key: pathToFile];
		await _fileService.Delete(link: attachment.LinkToFile, cancellationToken: cancellationToken);
		_attachments.Remove(key: pathToFile);
		return this;
	}

	public ITaskBuilder SetDateOfRelease(DateTime dateOfRelease)
	{
		_releasedAt = dateOfRelease;
		return this;
	}

	public ITaskBuilder SetClass(int classId)
	{
		_classId = classId;
		return this;
	}

	public ITaskBuilder SetClass(Class @class)
	{
		_classId = @class.Id;
		return this;
	}

	public ITaskBuilder SetSubject(int subjectId)
	{
		_subjectId = subjectId;
		return this;
	}

	public ITaskBuilder SetSubject(Subject subject)
	{
		_subjectId = subject.Id;
		return this;
	}

	private sealed record CreateTasksRequest(int SubjectId, int ClassId, TaskContent Content, DateTime ReleasedAt);
	private sealed record CreateTasksResponse(string Message);

	public async Task<string> Save(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_classId == 0)
			throw new ArgumentException(message: "Не указан класс, для которого создается задача.", paramName: nameof(_classId));

		if (_subjectId == 0)
			throw new ArgumentException(message: "Не указана дисциплина, по которого создается задача.", paramName: nameof(_subjectId));

		IEnumerable<Attachment> attachments = _attachments.Values.Select(
			selector: a => Attachment.Create(
				linkToFile: a.LinkToFile!,
				type: a.Type
			)
		);

		CreateTasksResponse response = await _fileService.ApiClient.PostAsync<CreateTasksResponse, CreateTasksRequest>(
			apiMethod: TaskControllerMethods.CreateTask,
			arg: new CreateTasksRequest(
				SubjectId: _subjectId,
				ClassId: _classId,
				Content: new TaskContent(Text: _text, Attachments: attachments),
				ReleasedAt: _releasedAt
			),
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();
		return response.Message;
	}
}