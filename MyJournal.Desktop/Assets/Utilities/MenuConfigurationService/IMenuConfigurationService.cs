using MyJournal.Desktop.Assets.Controls;

namespace MyJournal.Desktop.Assets.Utilities.MenuConfigurationService;

public interface IMenuConfigurationService
{
	public static MenuItemTypes CurrentType { get; protected set; }

	public delegate void ChangeMenuItemsTypeHandler(ChangeMenuItemsTypeEventArgs e);
	public static event ChangeMenuItemsTypeHandler? ChangeMenuItemsType;

	protected static void InvokeChangeMenuItemsTypeEvent(ChangeMenuItemsTypeEventArgs e)
		=> ChangeMenuItemsType?.Invoke(e: e);

	public void ChangeType(MenuItemTypes type);
}