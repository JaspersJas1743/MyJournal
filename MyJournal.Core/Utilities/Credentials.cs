using System.ComponentModel;
using System.Reflection;

namespace MyJournal.Core.Utilities;

public abstract class Credentials<T>
{
	public virtual CT GetCredential<CT>(string name)
	{
		PropertyInfo credential = GetType().GetProperty(name: name)
			?? throw new ArgumentException(message: $"Параметр `{name}` отсутствует", paramName: nameof(name));

		TypeConverter converter = TypeDescriptor.GetConverter(type: typeof(CT));
		if (!converter.CanConvertFrom(sourceType: credential.PropertyType))
			throw new ArgumentException(message: $"Не удалось конвертировать `{credential.PropertyType}` в `{typeof(CT)}`", paramName: nameof(CT));

		return (CT)converter.ConvertFrom(value: credential.GetValue(obj: this)!)!;
	}
}