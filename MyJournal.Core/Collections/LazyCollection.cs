using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;

namespace MyJournal.Core.Collections;

public abstract class LazyCollection<T> : IEnumerable<T>
	where T: ISubEntity
{
	#region Fields
	protected readonly ApiClient _client;
	protected readonly Lazy<List<T>> _collection = new Lazy<List<T>>(value: new List<T>());
	protected readonly int _count;

	protected int _offset;
	protected bool _allItemsAreUploaded;
	#endregion

	#region Constructor
	protected LazyCollection(
		ApiClient client,
		IEnumerable<T> collection,
		int count
	)
	{
		_client = client;
		_collection.Value.AddRange(collection: collection);
		_offset = _collection.Value.Count;
		_count = count;
		_allItemsAreUploaded = _collection.Value.Count < _count;
	}
	#endregion

	#region Properties
	public int Length => _collection.Value.Count;

	public bool IsLoaded => _collection.IsValueCreated;

	public T this[int id]
		=> _collection.Value.Find(match: i => i.Id.Equals(id))
		   ?? throw new ArgumentOutOfRangeException(message: $"Объект с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id));
	#endregion

	#region Methods
	#region Instance
	public virtual async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (_allItemsAreUploaded)
			return;

		int lengthBeforeLoading = Length;
		await Load(cancellationToken: cancellationToken);
		_allItemsAreUploaded = Length == lengthBeforeLoading;
	}
	#endregion

	#region Virtual
	public virtual async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Value.Clear();
		_offset = _collection.Value.Count;
		_allItemsAreUploaded = false;
	}

	internal abstract Task Append(
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	internal async Task Append(
		T instance,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Value.Add(item: instance);
		_offset = _collection.Value.Count;
	}

	internal abstract Task Insert(
		int index,
		int id,
		CancellationToken cancellationToken = default(CancellationToken)
	);

	internal async Task Insert(
		int index,
		T instance,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		_collection.Value.Insert(index: index, item: instance);
		_offset = _collection.Value.Count;
	}
	#endregion

	#region Abstract
	protected abstract Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	);
	#endregion

	#region IEnumerable<T>
	public IEnumerator<T> GetEnumerator()
		=> _collection.Value.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
	#endregion

	#region Overriden
	public override bool Equals(object? obj)
	{
		if (obj is LazyCollection<T> collection)
			return _collection.Value.SequenceEqual(second: collection);
		return false;
	}
	#endregion
	#endregion
}