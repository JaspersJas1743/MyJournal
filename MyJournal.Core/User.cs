using Microsoft.AspNetCore.SignalR.Client;
using MyJournal.Core.Utilities;

namespace MyJournal.Core;

public sealed class User
{
	private HubConnection _userHubConnection;

	private User(string surname, string name, string? patronymic, string? phone = null, string? email = null, string? activity = null, DateTime? onlineAt = null)
	{
		Surname = surname;
		Name = name;
		Patronymic = patronymic;
		Phone = phone;
		Email = email;
		Activity = activity is null ? null : Enum.Parse<ActivityStatus>(value: activity);
		OnlineAt = onlineAt;
	}

	#region Records
	private record SignOutResponse(string Message);
	private record UserInformation(string Surname, string Name, string? Patronymic, string? Phone, string? Email);
	#endregion

	#region Enums
	private enum SignOutOptions
	{
		SignOut,
		SignOutAll,
		SignOutAllExceptThis
	}

	public enum ActivityStatus
	{
		Online,
		Offline
	}
	#endregion

	#region Properties
	public string Surname { get; init; }
	public string Name { get; init; }
	public string? Patronymic { get; init; }
	public string? Phone { get; init; }
	public string? Email { get; init; }
	public ActivityStatus? Activity { get; init; }
	public DateTime? OnlineAt { get; init; }
	#endregion

	public delegate void SignedInUserHandler(int userId);
	public event SignedInUserHandler? SignedInUser;
	public delegate void SignedOutUserHandler(int userId);
	public event SignedOutUserHandler? SignedOutUser;

	public static async Task<User> Create(string token, CancellationToken cancellationToken = default(CancellationToken))
	{
		ApiClient.Token = token;
		UserInformation response = await ApiClient.GetAsync<UserInformation>(
			apiMethod: "User/GetInformation",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		User createdUser = new User(
			surname: response.Surname,
			name: response.Name,
			patronymic: response.Patronymic,
			phone: response.Phone,
			email: response.Email
		);

		createdUser._userHubConnection = DefaultHubConnectionBuilder.CreateHubConnection(url: "https://localhost:7267/hub/User");

		await createdUser._userHubConnection.StartAsync(cancellationToken: cancellationToken);
		createdUser._userHubConnection.On<int>(methodName: "SetOnline",  handler: userId => createdUser.SignedInUser?.Invoke(userId: userId));
		createdUser._userHubConnection.On<int>(methodName: "SetOffline", handler: userId => createdUser.SignedOutUser?.Invoke(userId: userId));

		return createdUser;
	}

	private async Task<string> SignOut(SignOutOptions options, CancellationToken cancellationToken = default(CancellationToken))
	{
		SignOutResponse response = await ApiClient.PostAsync<SignOutResponse>(
			apiMethod: $"Account/{options.ToString()}",
			cancellationToken: cancellationToken
		) ?? throw new InvalidOperationException();

		return response.Message;
	}

	private async Task SetActivityStatus(
		ActivityStatus activity,
		CancellationToken cancellationToken = default(CancellationToken)
	)
	{
		await ApiClient.PostAsync(
			apiMethod: $"User/Set{activity.ToString()}",
			cancellationToken: cancellationToken
		);
	}

	public async Task SetOffline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Offline, cancellationToken: cancellationToken);

	public async Task SetOnline(CancellationToken cancellationToken = default(CancellationToken))
		=> await SetActivityStatus(activity: ActivityStatus.Online, cancellationToken: cancellationToken);

	public async Task<string> SignOut(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOut, cancellationToken: cancellationToken);

	public async Task<string> SignOutAll(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOutAll, cancellationToken: cancellationToken);

	public async Task<string> SignOutAllExceptThis(CancellationToken cancellationToken = default(CancellationToken))
		=> await SignOut(options: SignOutOptions.SignOutAllExceptThis, cancellationToken: cancellationToken);
}