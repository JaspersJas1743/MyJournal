using System.Runtime.CompilerServices;

namespace MyJournal.Core.Utilities.AsyncLazy;

public sealed class AsyncLazy<T> : Lazy<Task<T>>
{
	public AsyncLazy(Func<T> valueFactory)
		: base(valueFactory: () => Task.Factory.StartNew(function: valueFactory))
	{ }

	public AsyncLazy(Func<Task<T>> valueFactory)
		: base(valueFactory: () => Task.Factory.StartNew(function: valueFactory).Unwrap())
	{ }

	public TaskAwaiter<T> GetAwaiter()
		=> Value.GetAwaiter();
}