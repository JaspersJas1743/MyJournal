using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using MyJournal.Core;
using MyJournal.Desktop.Models;
using MyJournal.Desktop.Models.Tasks;
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

	private static readonly string[] Images = new string[] { "Login", "Messages", "Tasks", "Marks", "Schedule" };
	private static readonly string[] Names = new string[] { "Профиль", "Диалоги", "Задания", "Оценки", "Занятия" };
	private static readonly MenuItemVM[] ContentsForTeacher;
    private static readonly MenuItemVM[] ContentsForStudent;
    private static readonly MenuItemVM[] ContentsForParent;
    private static readonly MenuItemVM[] ContentsForAdministrator;

	static RoleHelper()
	{
		App app = (Application.Current as App)!;
		ProfileVM profile = app.GetService<ProfileVM>();
		MessagesVM messages = app.GetService<MessagesVM>();
		TasksVM receivedTasks = app.GetService<ReceivedTasksVM>();
		TasksVM createdTasks = app.GetService<CreatedTasksVM>();
		TasksVM allTasks = app.GetService<AllTasksVM>();
		MarksVM receivedMarks = app.GetService<ReceivedMarksVM>();
		MarksVM createdMarks = app.GetService<CreatedMarksVM>();
		TimetableVM teacherTimetable = app.GetService<WorkTimetableVM>();
		TimetableVM studentTimetable = app.GetService<StudyTimetableVM>();
		TimetableVM administratorTimetable = app.GetService<CreatingTimetableVM>();

		ContentsForTeacher          = new MenuItemVM[] { profile, messages, createdTasks,	createdMarks,	teacherTimetable };
		ContentsForStudent          = new MenuItemVM[] { profile, messages, receivedTasks, receivedMarks,	studentTimetable };
		ContentsForParent			= new MenuItemVM[] { profile, messages, receivedTasks, receivedMarks,	studentTimetable };
		ContentsForAdministrator	= new MenuItemVM[] { profile, messages, allTasks,		createdMarks,	administratorTimetable };
	}

	public static IEnumerable<MenuItem> GetMenu(User user)
	{
		MenuItemVM[]? contents = GetContent(userType: user.GetType());
		if (contents is null)
			throw new NullReferenceException(message: $"Метод {nameof(GetContent)} вернул пустой список.");

		return Enumerable.Range(start: 0, count: Images.Length).Select(selector: index =>
		{
			MenuItemVM content = contents[index];
			content.SetUser(user: user);
			return new MenuItem(image: Images[index], header: Names[index], itemContent: content);
		});
	}

	public static string? GetRoleName(User user)
	{
		return typeof(RoleHelper).GetField(
			name: user.GetType().Name,
			bindingAttr: BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy
		)?.GetValue(obj: null)?.ToString();
	}

	private static MenuItemVM[]? GetContent(Type userType)
	{
		return typeof(RoleHelper).GetField(
			name: $"ContentsFor{userType.Name}",
			bindingAttr: BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase
		)?.GetValue(obj: null) as MenuItemVM[];
	}
}