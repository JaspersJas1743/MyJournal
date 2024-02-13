using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Chat
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int ChatTypeId { get; set; }

    public int LastMessage { get; set; }

    public string? LinkToPhoto { get; set; }

    public int? CreatorId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ChatType ChatType { get; set; } = null!;

    public virtual User? Creator { get; set; }

    public virtual Message LastMessageNavigation { get; set; } = null!;

    public virtual ICollection<Message> MessagesNavigation { get; set; } = new List<Message>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
