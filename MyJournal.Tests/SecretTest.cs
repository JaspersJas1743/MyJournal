// using System.Diagnostics;
// using MyJournal.Core;
// using MyJournal.Core.Authorization;
//
// namespace MyJournal.Tests;
//
// [TestFixture]
// public class SecretTest
// {
// 	[Test]
// 	public async Task Test()
// 	{
// 		AuthorizationWithCredentialsService service = new AuthorizationWithCredentialsService();
// 		UserAuthorizationCredentials credentials = new UserAuthorizationCredentials(
// 			login: "Jaspers",
// 			password: "JaspersJas1743",
// 			client: UserAuthorizationCredentials.Clients.Windows
// 		);
// 		User user = await service.SignIn(credentials: credentials);
// 		user.SignedInUser += (userId, onlineAt) => Debug.WriteLine($"[{nameof(Test)}] {userId} вошел в сеть");
// 		user.SignedOutUser += (userId, onlineAt) => Debug.WriteLine($"[{nameof(Test)}] {userId} вышел из сети");
// 		user.UpdatedProfilePhoto += userId => Debug.WriteLine($"[{nameof(Test)}] {userId} сменил фотографию профиля");
// 		await Task.Delay(TimeSpan.FromSeconds(100));
// 	}
// }