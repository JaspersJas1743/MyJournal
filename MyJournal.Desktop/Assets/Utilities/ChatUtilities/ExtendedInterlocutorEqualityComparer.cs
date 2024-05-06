using System.Collections.Generic;

namespace MyJournal.Desktop.Assets.Utilities.ChatUtilities;

public sealed class ExtendedInterlocutorEqualityComparer : IEqualityComparer<ExtendedInterlocutor>
{
	public bool Equals(ExtendedInterlocutor? x, ExtendedInterlocutor? y)
		=> x?.UserId == y?.UserId;

	public int GetHashCode(ExtendedInterlocutor obj)
		=> obj.GetHashCode();
}