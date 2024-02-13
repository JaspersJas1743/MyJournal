using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public enum AttachmentTypes
{
    Photo,
    Document
}

public partial class AttachmentType
{
    public int Id { get; set; }

    public AttachmentTypes Type { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
