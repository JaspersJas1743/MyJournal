namespace MyJournal.Desktop.Assets.Utilities;

public static class WordFormulator
{
	public static string GetForm(int count, string[] forms)
	{
		if (count % 100 >= 11 && count % 100 <= 19)
			return forms[0];

		return $"{count} " + (count % 10) switch
		{
			1 => forms[1],
			2 or 3 or 4 => forms[2],
			_ => forms[0]
		};
	}
}