namespace MyJournal.Core.Authorization;

public class Authorized<T>(T instance, Type typeOfInstance, string token)
{
	public Type TypeOfInstance { get; } = typeOfInstance;
	public T Instance { get; } = instance;
	public string Token { get; } = token;
}