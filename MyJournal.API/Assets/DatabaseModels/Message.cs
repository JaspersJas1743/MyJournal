using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class Message
{
    public int Id { get; set; }

    public int? ReplyMessage { get; set; }

    public int? ForwardedMessage { get; set; }

    public int SenderId { get; set; }

    public int? ChatId { get; set; }

    public string? Text { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? ReadedAt { get; set; }

    public virtual Chat? Chat { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual Message? ForwardedMessageNavigation { get; set; }

    public virtual ICollection<Message> InverseForwardedMessageNavigation { get; set; } = new List<Message>();

    public virtual ICollection<Message> InverseReplyMessageNavigation { get; set; } = new List<Message>();

    public virtual Message? ReplyMessageNavigation { get; set; }

    public virtual User Sender { get; set; } = null!;

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Chat> ChatsNavigation { get; set; } = new List<Chat>();
}
