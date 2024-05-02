using System.Collections;
using MyJournal.Core.SubEntities;
using MyJournal.Core.Utilities.Api;
using MyJournal.Core.Utilities.AsyncLazy;

namespace MyJournal.Core.Collections;

public abstract class LazyCollection<T> : IAsyncEnumerable<T> where T: ISubEntity
{
	#region Fields
	private bool _allItemsAreUploaded;

	protected readonly ApiClient Client;
	protected readonly AsyncLazy<List<T>> Collection;
	protected readonly int Count;

	protected int Offset;
	#endregion

	#region Constructor
	protected LazyCollection(
		ApiClient client,
		AsyncLazy<List<T>> collection,
		int offset,
		int count
	)
	{
		Client = client;
		Collection = collection;
		Offset = offset;
		Count = count;
		_allItemsAreUploaded = offset < Count;
	}
	#endregion

	#region Properties
	public int Length => Offset;
	public bool AllItemsAreUploaded => _allItemsAreUploaded;
	#endregion

	#region Methods
	#region Instance
	public async Task<T> GetByIndex(int index)
	{
		List<T> collection = await Collection;
		return collection[index: index];
	}

	public async Task<T> GetByIndex(Index index)
	{
		List<T> collection = await Collection;
		return collection[index];
	}

	public async Task<IEnumerable<T>> GetByRange(Range range)
	{
		List<T> collection = await Collection;
		return collection[range];
	}

	public async Task<IEnumerable<T>> GetByRange(int start, int end)
	{
		List<T> collection = await Collection;
		return collection[new Range(start: new Index(value: start), end: new Index(value: end))];
	}

	public async Task<IEnumerable<T>> GetByRange(Index start, Index end)
	{
		List<T> collection = await Collection;
		return collection[new Range(start: start, end: end)];
	}

	public async Task<T?> FindById(int? id)
	{
		List<T> collection = await Collection;
		return collection.Find(match: i => i.Id.Equals(id));
	}

	public virtual async Task LoadNext(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		if (_allItemsAreUploaded)
			return;

		int lengthBeforeLoading = Length;
		await Load(cancellationToken: cancellationToken);
		int lengthAfterLoading = Length;
		_allItemsAreUploaded = lengthAfterLoading == lengthBeforeLoading;
	}
	#endregion

	#region Virtual
	public virtual async Task Clear(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		List<T> collection = await Collection;
		collection.Clear();
		Offset = collection.Count;
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
		List<T> collection = await Collection;
		collection.Add(item: instance);
		Offset = collection.Count;
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
		List<T> collection = await Collection;
		collection.Insert(index: index, item: instance);
		Offset = collection.Count;
	}

	public new async Task<bool> Equals(object? obj)
	{
		if (!Collection.IsValueCreated)
			return ReferenceEquals(objA: Collection, objB: obj);

		List<T> currentCollection = await Collection;
		if (obj is not LazyCollection<T> collection)
			return false;

		return collection.Collection.IsValueCreated || currentCollection.SequenceEqual(second: await collection.Collection);
	}
	#endregion

	#region Abstract
	protected abstract Task Load(
		CancellationToken cancellationToken = default(CancellationToken)
	);
	#endregion

	#region IAsyncEnumerable<T>
	public async IAsyncEnumerator<T> GetAsyncEnumerator(
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		foreach (T item in await Collection)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return item;
		}
	}
	#endregion
	#endregion
}