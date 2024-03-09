using System;
using System.Collections.Generic;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class User
{
    public int Id { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    public int UserActivityStatusId { get; set; }

    public DateTime? OnlineAt { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Login { get; set; }

    public string? Password { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? RegistrationCode { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public string? LinkToPhoto { get; set; }

    public int UserRoleId { get; set; }

    public string? AuthorizationCode { get; set; }

    public virtual ICollection<Administrator> Administrators { get; set; } = new List<Administrator>();

    public virtual ICollection<Chat> CreatedChats { get; set; } = new List<Chat>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Parent> Parents { get; set; } = new List<Parent>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

    public virtual UserActivityStatus UserActivityStatus { get; set; } = null!;

    public virtual UserRole UserRole { get; set; } = null!;

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();
}
