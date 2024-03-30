using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.Collections;

public abstract class LazyCollection<T> : IEnumerable<T> where T: ISubEntity
{
	#region Fields
	private bool _allItemsAreUploaded;

	protected readonly ApiClient _client;
	protected readonly AsyncLazy<List<T>> _collection;
	protected readonly int _count;

	protected int _offset;
	#endregion

	#region Constructor
	protected LazyCollection(
		ApiClient client,
		AsyncLazy<List<T>> collection,
		int offset,
		int count
	)
	{
		_client = client;
		_collection = collection;
		_offset = offset;
		_count = count;
		_allItemsAreUploaded = offset < _count;
	}
	#endregion

	#region Properties

	public async Task<int> GetLength()
	{
		List<T> collection = await _collection;
		return collection.Count;
	}

	public async Task<T> GetById(int id)
	{
		List<T> collection = await _collection;
		return collection.Find(match: i => i.Id.Equals(id))
		   ?? throw new ArgumentOutOfRangeException(message: $"Объект с идентификатором {id} отсутствует или не загружен.", paramName: nameof(id));
	}
	#endregion

	#region Methods
	#region Instance
	public virtual async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (_allItemsAreUploaded)
			return;

		int lengthBeforeLoading = await GetLength();
		await Load(cancellationToken: cancellationToken);
		int lengthAfterLoading = await GetLength();
		_allItemsAreUploaded = lengthAfterLoading == lengthBeforeLoading;
	}
	#endregion

	#region Virtual
	public virtual async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<T> collection = await _collection;
		collection.Clear();
		_offset = collection.Count;
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
		List<T> collection = await _collection;
		collection.Add(item: instance);
		_offset = collection.Count;
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
		List<T> collection = await _collection;
		collection.Insert(index: index, item: instance);
		_offset = collection.Count;
	}

	public async Task<bool> Equals(object? obj)
	{
		List<T> currentCollection = await _collection;
		if (obj is LazyCollection<T> collection)
			return currentCollection.SequenceEqual(second: collection);
		return false;
	}
	#endregion

	#region Abstract
	protected abstract Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	);
	#endregion

	#region IEnumerable<T>
	public IEnumerator<T> GetEnumerator()
		=> _collection.GetAwaiter().GetResult().GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		GetEnumerator();
	#endregion
	#endregion
}