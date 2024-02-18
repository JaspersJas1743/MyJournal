using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyJournal.API.Assets.DatabaseModels;

public partial class MyJournalContext : DbContext
{
    public MyJournalContext()
    {
    }

    public MyJournalContext(DbContextOptions<MyJournalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Administrator> Administrators { get; set; }

    public virtual DbSet<Assessment> Assessments { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AttachmentType> AttachmentTypes { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<ChatType> ChatTypes { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<CommentsOnGrade> CommentsOnGrades { get; set; }

    public virtual DbSet<DaysOfWeek> DaysOfWeeks { get; set; }

    public virtual DbSet<EducationPeriod> EducationPeriods { get; set; }

    public virtual DbSet<EducationPeriodForClass> EducationPeriodForClasses { get; set; }

    public virtual DbSet<FinalGradesForEducationPeriod> FinalGradesForEducationPeriods { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GradeType> GradeTypes { get; set; }

    public virtual DbSet<Holiday> Holidays { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<LessonTiming> LessonTimings { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MyJournalClient> MyJournalClients { get; set; }

    public virtual DbSet<Parent> Parents { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SessionActivityStatus> SessionActivityStatuses { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskCompletionResult> TaskCompletionResults { get; set; }

    public virtual DbSet<TaskCompletionStatus> TaskCompletionStatuses { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeachersLesson> TeachersLessons { get; set; }

    public virtual DbSet<TypeOfDay> TypesOfDays { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivityStatus> UserActivityStatuses { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Administ__3214EC07FEABE096");

            entity.HasOne(d => d.User).WithMany(p => p.Administrators)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Administr__UserI__10566F31");
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assessme__3214EC07F3670E31");

            entity.Property(e => e.Datetime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Comment).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("FK__Assessmen__Comme__282DF8C2");

            entity.HasOne(d => d.Grade).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Assessmen__Grade__2739D489");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Assessmen__Lesso__25518C17");

            entity.HasOne(d => d.Student).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Assessmen__Stude__2645B050");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attachme__3214EC076993C8A8");

            entity.Property(e => e.AttachmentName).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.AttachmentType).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.AttachmentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attachmen__Attac__19DFD96B");
        });

        modelBuilder.Entity<AttachmentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attachme__3214EC07B59B9E0B");

            entity.HasIndex(e => e.Type, "UQ__Attachme__F9B8A48B1C061D46").IsUnique();

            entity.Property(e => e.Type).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AttachmentTypes>(v)
            );

            entity.Property(e => e.Type).HasMaxLength(8);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Chats__3214EC073BF79996");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.ChatType).WithMany(p => p.Chats)
                .HasForeignKey(d => d.ChatTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chats__ChatTypeI__1332DBDC");

            entity.HasOne(d => d.Creator).WithMany(p => p.Chats)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK__Chats__CreatorId__151B244E");

            entity.HasOne(d => d.LastMessageNavigation).WithMany(p => p.Chats)
                .HasForeignKey(d => d.LastMessage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Chats__LastMessa__14270015");

            entity.HasMany(d => d.Attachments).WithMany(p => p.Chats)
                .UsingEntity<Dictionary<string, object>>(
                    "ChatsAttachment",
                    r => r.HasOne<Attachment>().WithMany()
                        .HasForeignKey("AttachmentsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Att__Attac__40058253"),
                    l => l.HasOne<Chat>().WithMany()
                        .HasForeignKey("ChatsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Att__Chats__3F115E1A"),
                    j =>
                    {
                        j.HasKey("ChatsId", "AttachmentsId").HasName("PK__Chats_At__E9507F4B74E3D91C");
                        j.ToTable("Chats_Attachments");
                        j.IndexerProperty<int>("ChatsId").HasColumnName("Chats_Id");
                        j.IndexerProperty<int>("AttachmentsId").HasColumnName("Attachments_Id");
                    });

            entity.HasMany(d => d.Messages).WithMany(p => p.ChatsNavigation)
                .UsingEntity<Dictionary<string, object>>(
                    "ChatsMessage",
                    r => r.HasOne<Message>().WithMany()
                        .HasForeignKey("MessagesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Mes__Messa__3864608B"),
                    l => l.HasOne<Chat>().WithMany()
                        .HasForeignKey("ChatsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Mes__Chats__37703C52"),
                    j =>
                    {
                        j.HasKey("ChatsId", "MessagesId").HasName("PK__Chats_Me__322961A54AC0FFAD");
                        j.ToTable("Chats_Messages");
                        j.IndexerProperty<int>("ChatsId").HasColumnName("Chats_Id");
                        j.IndexerProperty<int>("MessagesId").HasColumnName("Messages_Id");
                    });

            entity.HasMany(d => d.Users).WithMany(p => p.ChatsNavigation)
                .UsingEntity<Dictionary<string, object>>(
                    "ChatsUser",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Use__Users__3493CFA7"),
                    l => l.HasOne<Chat>().WithMany()
                        .HasForeignKey("ChatsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Chats_Use__Chats__339FAB6E"),
                    j =>
                    {
                        j.HasKey("ChatsId", "UsersId").HasName("PK__Chats_Us__529B2B7DA0B8DF8D");
                        j.ToTable("Chats_Users");
                        j.IndexerProperty<int>("ChatsId").HasColumnName("Chats_Id");
                        j.IndexerProperty<int>("UsersId").HasColumnName("Users_Id");
                    });
        });

        modelBuilder.Entity<ChatType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatType__3214EC0723B88264");

            entity.HasIndex(e => e.Type, "UQ__ChatType__86785823CEF85C70").IsUnique();

            entity.Property(e => e.Type).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ChatTypes>(v)
            );

            entity.Property(e => e.Type)
                .HasMaxLength(6)
                .HasColumnName("ChatType");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Classes__3214EC07E926922B");

            entity.Property(e => e.Name).HasMaxLength(9);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Clients__3214EC0705F81109");

            entity.HasIndex(e => e.ClientName, "UQ__Clients__65800DA001DBF1AC").IsUnique();

            entity.Property(e => e.ClientName).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<Clients>(v)
            );

            entity.Property(e => e.ClientName).HasMaxLength(7);
        });

        modelBuilder.Entity<CommentsOnGrade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Comments__3214EC077D0CED9B");

            entity.HasIndex(e => e.Comment, "UQ__Comments__239A9B1CA52CFDFE").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(3);
            entity.Property(e => e.Description)
                .HasMaxLength(25)
                .HasDefaultValueSql("((1))");
        });

        modelBuilder.Entity<DaysOfWeek>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DaysOfWe__3214EC07B2D33FF6");

            entity.ToTable("DaysOfWeek");

            entity.HasIndex(e => e.DayOfWeek, "UQ__DaysOfWe__00D400DDB723065B").IsUnique();

            entity.Property(e => e.DayOfWeek).HasMaxLength(11);

            entity.HasOne(d => d.TypeOfDayNavigation).WithMany(p => p.DaysOfWeeks)
                .HasForeignKey(d => d.TypeOfDay)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DaysOfWee__TypeO__30C33EC3");
        });

        modelBuilder.Entity<EducationPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Educatio__3214EC07246FF154");

            entity.HasIndex(e => e.Period, "UQ__Educatio__1672D2575D7F6DB0").IsUnique();

            entity.Property(e => e.Period).HasMaxLength(12);
        });

        modelBuilder.Entity<EducationPeriodForClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Educatio__3214EC07DE7C0949");

            entity.HasOne(d => d.Class).WithMany(p => p.EducationPeriodForClasses)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Education__Class__1BC821DD");

            entity.HasOne(d => d.EducationPeriod).WithMany(p => p.EducationPeriodForClasses)
                .HasForeignKey(d => d.EducationPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Education__Educa__1CBC4616");

            entity.HasMany(d => d.Lessons).WithMany(p => p.EducationPeriodForClasses)
                .UsingEntity<Dictionary<string, object>>(
                    "EducationPeriodForClassesLesson",
                    r => r.HasOne<Lesson>().WithMany()
                        .HasForeignKey("LessonsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Education__Lesso__47A6A41B"),
                    l => l.HasOne<EducationPeriodForClass>().WithMany()
                        .HasForeignKey("EducationPeriodForClassesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Education__Educa__46B27FE2"),
                    j =>
                    {
                        j.HasKey("EducationPeriodForClassesId", "LessonsId").HasName("PK__Educatio__1109C3E96CD12F6E");
                        j.ToTable("EducationPeriodForClasses_Lessons");
                        j.IndexerProperty<int>("EducationPeriodForClassesId").HasColumnName("EducationPeriodForClasses_Id");
                        j.IndexerProperty<int>("LessonsId").HasColumnName("Lessons_Id");
                    });
        });

        modelBuilder.Entity<FinalGradesForEducationPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FinalGra__3214EC07F7FC4C0B");

            entity.HasOne(d => d.EducationPeriod).WithMany(p => p.FinalGradesForEducationPeriods)
                .HasForeignKey(d => d.EducationPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FinalGrad__Educa__2BFE89A6");

            entity.HasOne(d => d.Grade).WithMany(p => p.FinalGradesForEducationPeriods)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FinalGrad__Grade__2CF2ADDF");

            entity.HasOne(d => d.Lesson).WithMany(p => p.FinalGradesForEducationPeriods)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FinalGrad__Lesso__2B0A656D");

            entity.HasOne(d => d.Student).WithMany(p => p.FinalGradesForEducationPeriods)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FinalGrad__Stude__2A164134");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Grades__3214EC079EF12C03");

            entity.HasIndex(e => e.Grade1, "UQ__Grades__DF0ADB7AAD4E5716").IsUnique();

            entity.Property(e => e.Grade1)
                .HasMaxLength(1)
                .HasColumnName("Grade");

            entity.HasOne(d => d.GradeType).WithMany(p => p.Grades)
                .HasForeignKey(d => d.GradeTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Grades__GradeTyp__29221CFB");
        });

        modelBuilder.Entity<GradeType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GradeTyp__3214EC0795B951E1");

            entity.HasIndex(e => e.Type, "UQ__GradeTyp__27CE1B67B3EA2C91").IsUnique();

            entity.Property(e => e.Type).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<GradeTypes>(v)
            );

            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .HasColumnName("GradeType");

            entity.HasMany(d => d.CommentsOnGrades).WithMany(p => p.GradeTypes)
                .UsingEntity<Dictionary<string, object>>(
                    "GradeTypesCommentsOnGrade",
                    r => r.HasOne<CommentsOnGrade>().WithMany()
                        .HasForeignKey("CommentsOnGradesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__GradeType__Comme__4F47C5E3"),
                    l => l.HasOne<GradeType>().WithMany()
                        .HasForeignKey("GradeTypesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__GradeType__Grade__4E53A1AA"),
                    j =>
                    {
                        j.HasKey("GradeTypesId", "CommentsOnGradesId").HasName("PK__GradeTyp__D9464A7EB2E7E942");
                        j.ToTable("GradeTypes_CommentsOnGrades");
                        j.IndexerProperty<int>("GradeTypesId").HasColumnName("GradeTypes_Id");
                        j.IndexerProperty<int>("CommentsOnGradesId").HasColumnName("CommentsOnGrades_Id");
                    });
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Holidays__3214EC07F5192BF8");

            entity.HasOne(d => d.EducationPeriod).WithMany(p => p.Holidays)
                .HasForeignKey(d => d.EducationPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Holidays__Educat__1AD3FDA4");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lessons__3214EC07FCD21C09");

            entity.HasIndex(e => e.Name, "UQ__Lessons__737584F6B9A4F8DC").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(75);
        });

        modelBuilder.Entity<LessonTiming>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LessonTi__3214EC079287F8A4");

            entity.HasOne(d => d.Class).WithMany(p => p.LessonTimings)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LessonTim__Class__2FCF1A8A");

            entity.HasOne(d => d.DayOfWeek).WithMany(p => p.LessonTimings)
                .HasForeignKey(d => d.DayOfWeekId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LessonTim__DayOf__2EDAF651");

            entity.HasOne(d => d.Lesson).WithMany(p => p.LessonTimings)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LessonTim__Lesso__2DE6D218");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC07540C9FDD");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.ReadedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Chat).WithMany(p => p.MessagesNavigation)
                .HasForeignKey(d => d.ChatId)
                .HasConstraintName("FK__Messages__ChatId__18EBB532");

            entity.HasOne(d => d.ForwardedMessageNavigation).WithMany(p => p.InverseForwardedMessageNavigation)
                .HasForeignKey(d => d.ForwardedMessage)
                .HasConstraintName("FK__Messages__Forwar__17036CC0");

            entity.HasOne(d => d.ReplyMessageNavigation).WithMany(p => p.InverseReplyMessageNavigation)
                .HasForeignKey(d => d.ReplyMessage)
                .HasConstraintName("FK__Messages__ReplyM__160F4887");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Sender__17F790F9");

            entity.HasMany(d => d.Attachments).WithMany(p => p.Messages)
                .UsingEntity<Dictionary<string, object>>(
                    "MessagesAttachment",
                    r => r.HasOne<Attachment>().WithMany()
                        .HasForeignKey("AttachmentsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Messages___Attac__3C34F16F"),
                    l => l.HasOne<Message>().WithMany()
                        .HasForeignKey("MessagesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Messages___Messa__3B40CD36"),
                    j =>
                    {
                        j.HasKey("MessagesId", "AttachmentsId").HasName("PK__Messages__9531520F32B7E405");
                        j.ToTable("Messages_Attachments");
                        j.IndexerProperty<int>("MessagesId").HasColumnName("Messages_Id");
                        j.IndexerProperty<int>("AttachmentsId").HasColumnName("Attachments_Id");
                    });
        });

        modelBuilder.Entity<MyJournalClient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MyJourna__3214EC074EF1C694");

            entity.Property(e => e.ClientName)
                .HasMaxLength(14)
                .HasDefaultValueSql("((1))");

            entity.HasOne(d => d.Client).WithMany(p => p.MyJournalClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MyJournal__Clien__0C85DE4D");
        });

        modelBuilder.Entity<Parent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Parents__3214EC079A36CA5C");

            entity.HasOne(d => d.Children).WithMany(p => p.Parents)
                .HasForeignKey(d => d.ChildrenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Parents__Childre__123EB7A3");

            entity.HasOne(d => d.User).WithMany(p => p.Parents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Parents__UserId__114A936A");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sessions__3214EC0745B41A87");

            entity.Property(e => e.Ip)
                .HasMaxLength(15)
                .HasColumnName("IP");
            
            entity.HasOne(d => d.MyJournalClient).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.MyJournalClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sessions__MyJour__0A9D95DB");

            entity.HasOne(d => d.SessionActivityStatus).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.SessionActivityStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sessions__Sessio__0B91BA14");

            entity.HasOne(d => d.User).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Sessions__UserId__09A971A2");
        });

        modelBuilder.Entity<SessionActivityStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SessionA__3214EC076C2E58E3");

            entity.Property(e => e.ActivityStatus).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<SessionActivityStatuses>(v)
            );

            entity.HasIndex(e => e.ActivityStatus, "UQ__SessionA__121B440BA261028D").IsUnique();

            entity.Property(e => e.ActivityStatus).HasMaxLength(7);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Students__3214EC0706D65229");

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Students__ClassI__0E6E26BF");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Students__UserId__0D7A0286");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tasks__3214EC07F7CD1E23");

            entity.Property(e => e.ReleasedAt).HasColumnType("datetime");
            entity.Property(e => e.Text).HasMaxLength(255);

            entity.HasOne(d => d.Class).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__ClassId__208CD6FA");

            entity.HasOne(d => d.Creator).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__CreatorId__2180FB33");

            entity.HasOne(d => d.Lesson).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__LessonId__1F98B2C1");

            entity.HasMany(d => d.Attachments).WithMany(p => p.Tasks)
                .UsingEntity<Dictionary<string, object>>(
                    "TasksAttachment",
                    r => r.HasOne<Attachment>().WithMany()
                        .HasForeignKey("AttachmentsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Tasks_Att__Attac__4B7734FF"),
                    l => l.HasOne<Task>().WithMany()
                        .HasForeignKey("TasksId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Tasks_Att__Tasks__4A8310C6"),
                    j =>
                    {
                        j.HasKey("TasksId", "AttachmentsId").HasName("PK__Tasks_At__9C1A76C03856B87F");
                        j.ToTable("Tasks_Attachments");
                        j.IndexerProperty<int>("TasksId").HasColumnName("Tasks_Id");
                        j.IndexerProperty<int>("AttachmentsId").HasColumnName("Attachments_Id");
                    });
        });

        modelBuilder.Entity<TaskCompletionResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskComp__3214EC074CB6A2EB");

            entity.Property(e => e.TaskCompletionStatusId).HasDefaultValue(2);

            entity.HasOne(d => d.Student).WithMany(p => p.TaskCompletionResults)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaskCompl__Stude__236943A5");

            entity.HasOne(d => d.TaskCompletionStatus).WithMany(p => p.TaskCompletionResults)
                .HasForeignKey(d => d.TaskCompletionStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaskCompl__TaskC__245D67DE");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskCompletionResults)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaskCompl__TaskI__22751F6C");
        });

        modelBuilder.Entity<TaskCompletionStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TaskComp__3214EC07711216CA");

            entity.Property(e => e.CompletionStatus).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TaskCompletionStatuses>(v)
            );

            entity.HasIndex(e => e.CompletionStatus, "UQ__TaskComp__5808739C1A411176").IsUnique();

            entity.Property(e => e.CompletionStatus).HasMaxLength(11);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teachers__3214EC076B92B857");

            entity.HasOne(d => d.User).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Teachers__UserId__0F624AF8");
        });

        modelBuilder.Entity<TeachersLesson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teachers__3214EC071B96C9FF");

            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");

            entity.HasOne(d => d.Lesson).WithMany(p => p.TeachersLessons)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TeachersL__Lesso__1EA48E88");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeachersLessons)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TeachersL__Teach__1DB06A4F");

            entity.HasMany(d => d.Classes).WithMany(p => p.TeachersLessons)
                .UsingEntity<Dictionary<string, object>>(
                    "TeachersLessonsClass",
                    r => r.HasOne<Class>().WithMany()
                        .HasForeignKey("ClassesId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__TeachersL__Class__43D61337"),
                    l => l.HasOne<TeachersLesson>().WithMany()
                        .HasForeignKey("TeachersLessonsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__TeachersL__Teach__42E1EEFE"),
                    j =>
                    {
                        j.HasKey("TeachersLessonsId", "ClassesId").HasName("PK__Teachers__F098B412EA847552");
                        j.ToTable("TeachersLessons_Classes");
                        j.IndexerProperty<int>("TeachersLessonsId").HasColumnName("TeachersLessons_Id");
                        j.IndexerProperty<int>("ClassesId").HasColumnName("Classes_Id");
                    });
        });

        modelBuilder.Entity<TypeOfDay>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypesOfD__3214EC07AC2C430C");

            entity.Property(e => e.Type).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<TypesOfDay>(v)
            );

            entity.ToTable("TypesOfDay");

            entity.HasIndex(e => e.Type, "UQ__TypesOfD__B0485BAA31595AF9").IsUnique();

            entity.Property(e => e.Type).HasMaxLength(10);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07EC4A57DC");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Login).HasMaxLength(25);
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.OnlineAt).HasColumnType("datetime");
            entity.Property(e => e.Password).HasMaxLength(25);
            entity.Property(e => e.Patronymic).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.RegisteredAt).HasColumnType("datetime");
            entity.Property(e => e.RegistrationCode).HasMaxLength(7);
            entity.Property(e => e.Surname).HasMaxLength(20);

            entity.HasOne(d => d.UserActivityStatus).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserActivityStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__UserActiv__08B54D69");

            entity.HasOne(d => d.UserRole).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__UserRoleI__08B54D69");
        });

        modelBuilder.Entity<UserActivityStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserActi__3214EC079C27A5B8");

            entity.Property(e => e.ActivityStatus).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UserActivityStatuses>(v)
            );

            entity.HasIndex(e => e.ActivityStatus, "UQ__UserActi__121B440BD047BE26").IsUnique();

            entity.Property(e => e.ActivityStatus).HasMaxLength(7);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.Property(e => e.Role).HasMaxLength(13);

            entity.Property(e => e.Role).HasConversion(
                v => v.ToString(),
                v => Enum.Parse<UserRoles>(v)
            );
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
