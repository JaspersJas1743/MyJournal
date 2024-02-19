using System.Reflection;

namespace MyJournal.Core.Registration;

public abstract class Credentials<T>
{
	public virtual string? GetCredential(string name)
	{
		PropertyInfo credential = GetType().GetProperty(name: name)
			?? throw new ArgumentException(message: $"Параметр `{name}` отсутствует", paramName: nameof(name));
		return credential.GetValue(obj: this)?.ToString();
	}
}