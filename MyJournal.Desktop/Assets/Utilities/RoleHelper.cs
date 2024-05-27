using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using MyJournal.Core;
using MyJournal.Core.Authorization;
using MyJournal.Desktop.Assets.Controls;
using MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;
using MyJournal.Desktop.ViewModels;
using MyJournal.Desktop.ViewModels.Marks;
using MyJournal.Desktop.ViewModels.Profile;
using MyJournal.Desktop.ViewModels.Tasks;
using MyJournal.Desktop.ViewModels.Timetable;
using MenuItem = MyJournal.Desktop.Assets.Controls.MenuItem;

namespace MyJournal.Desktop.Assets.Utilities;

public static class RoleHelper
{
	private const string Teacher = "Преподаватель";
	private const string Student = "Ученик";
	private const string Parent = "Родитель";
	private const string Administrator = "Администратор";

	private static readonly App App;
	private static readonly string[] Images = new string[] { "Login", "Messages", "Tasks", "Marks", "Schedule" };
	private static readonly string[] Names = new string[] { "Профиль", "Диалоги", "Задания", "Оценки", "Занятия" };

	static RoleHelper()
		=> App = (Application.Current as App)!;

	public static async Task<IEnumerable<MenuItem>> GetMenu(Authorized<User> user)
	{
		MenuItemVM[] contents = GetContent(userRole: user.Role);
		if (contents is null)
			throw new NullReferenceException(message: $"Метод {nameof(GetContent)} вернул пустой список.");

		return await Task.WhenAll(tasks: Enumerable.Range(start: 0, count: Images.Length).Select(selector: async index =>
		{
			MenuItemVM content = contents[index];
			await content.SetUser(user: user.Instance);
			return new MenuItem(image: Images[index], header: Names[index], itemContent: content, itemType: IMenuConfigurationService.CurrentType);
		}));
	}

	public static IEnumerable<BaseMenuItem> GetBaseMenu()
		=> Enumerable.Range(start: 0, count: Images.Length).Select(selector: index => new BaseMenuItem(image: Images[index], header: Names[index]));

	public static string? GetRoleName(User user)
	{
		return typeof(RoleHelper).GetField(
			name: user.GetType().Name,
			bindingAttr: BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy
		)?.GetValue(obj: null)?.ToString();
	}

	private static MenuItemVM[] GetContent(UserRoles userRole)
	{
		ProfileVM profile = App.GetService<ProfileVM>();
		MessagesVM messages = App.GetService<MessagesVM>();
		TasksVM receivedTasks = App.GetService<ReceivedTasksVM>();
		TasksVM createdTasks = App.GetService<CreatedTasksVM>();
		MarksVM receivedMarks = App.GetService<ReceivedMarksVM>();
		MarksVM createdMarks = App.GetService<CreatedMarksVM>();
		BaseTimetableVM timetable = App.GetService<TimetableVM>();
		BaseTimetableVM creatingTimetable = App.GetService<CreatingTimetableVM>();

		return userRole switch
		{
			UserRoles.Teacher       => new MenuItemVM[] { profile, messages, createdTasks,	createdMarks,	timetable },
			UserRoles.Student       => new MenuItemVM[] { profile, messages, receivedTasks,	receivedMarks,	timetable },
			UserRoles.Parent		=> new MenuItemVM[] { profile, messages, receivedTasks,	receivedMarks,	timetable },
			UserRoles.Administrator => new MenuItemVM[] { profile, messages, createdTasks,	createdMarks,	creatingTimetable },
		};
	}
}