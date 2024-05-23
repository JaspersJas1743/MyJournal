namespace MyJournal.Core.Authorization;

public class Authorized<T>(
	T instance,
	Type typeOfInstance,
	string token,
	UserRoles role
)
{
	public Type TypeOfInstance { get; } = typeOfInstance;
	public T Instance { get; } = instance;
	public string Token { get; } = token;
	public UserRoles Role { get; } = role;
}