using System;
using System.Collections.Generic;
using System.Linq;
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
	private static readonly string[] Images = new string[] { "Login", "Messages", "Tasks", "Marks", "Schedule" };
	private static readonly string[] Names = new string[] { "Профиль", "Диалоги", "Задания", "Оценки", "Занятия" };
	private static readonly BaseVM[] ContentsForTeacher;
    private static readonly BaseVM[] ContentsForStudent;
    private static readonly BaseVM[] ContentsForParent;
    private static readonly BaseVM[] ContentsForAdministrator;

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

		ContentsForTeacher = new BaseVM[]		{ profile, messages, createdTasks,	createdMarks,	teacherTimetable };
		ContentsForStudent = new BaseVM[]		{ profile, messages, receivedTasks, receivedMarks,	studentTimetable };
		ContentsForParent = new BaseVM[]		{ profile, messages, receivedTasks, receivedMarks,	studentTimetable };
		ContentsForAdministrator = new BaseVM[] { profile, messages, allTasks,		createdMarks,	administratorTimetable };
	}

	public static IEnumerable<MenuItem> GetMenu(User user)
	{
		BaseVM[] contents = GetContent(userType: user.GetType());
		return Enumerable.Range(start: 0, count: Images.Length).Select(
			selector: index => new MenuItem(image: Images[index], header: Names[index], itemContent: contents[index])
		);
	}

	public static IEnumerable<MenuItem> GetMenu(Type userType)
	{
		BaseVM[] contents = GetContent(userType: userType);
		return Enumerable.Range(start: 0, count: Images.Length).Select(
			selector: index => new MenuItem(image: Images[index], header: Names[index], itemContent: contents[index])
		);
	}

	private static BaseVM[] GetContent(Type userType)
	{
		if (userType.IsEquivalentTo(other: typeof(Teacher)))
			return ContentsForTeacher;

		if (userType.IsEquivalentTo(other: typeof(Student)))
			return ContentsForStudent;

		if (userType.IsEquivalentTo(other: typeof(Parent)))
			return ContentsForParent;

		return ContentsForAdministrator;
	}
}