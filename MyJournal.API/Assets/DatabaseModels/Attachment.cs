using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Attachment
{
    public int Id { get; set; }

    public string Link { get; set; } = null!;

    public int AttachmentTypeId { get; set; }

    public virtual AttachmentType AttachmentType { get; set; } = null!;

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
