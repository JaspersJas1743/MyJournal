using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum ChatTypes
{
    Multi,
    Single
}

public partial class ChatType
{
    public int Id { get; set; }

    public ChatTypes Type { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();
}
