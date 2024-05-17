namespace MyJournal.Core.Utilities.EventArgs;

#region Email
public delegate void UpdatedEmailHandler(UpdatedEmailEventArgs e);
#endregion

#region Profile photo
public delegate void UpdatedProfilePhotoHandler(UpdatedProfilePhotoEventArgs e);
#endregion

#region Phone
public delegate void UpdatedPhoneHandler(UpdatedPhoneEventArgs e);
#endregion

#region Tasks
public delegate void CompletedTaskHandler(CompletedTaskEventArgs e);
public delegate void UncompletedTaskHandler(UncompletedTaskEventArgs e);
public delegate void CreatedTaskHandler(CreatedTaskEventArgs e);
#endregion

#region Messages
public delegate void ReceivedMessageHandler(ReceivedMessageEventArgs e);
public delegate void ReadMessageHandler(ReadMessageEventArgs e);
public delegate void ReadMessagesHandler();
#endregion

#region Chats
public delegate void JoinedInChatHandler(JoinedInChatEventArgs e);
public delegate void ReadChatHandler(ReadChatEventArgs e);
#endregion

#region Interlocutor
public delegate void InterlocutorAppearedOnlineHandler(InterlocutorAppearedOnlineEventArgs e);
public delegate void InterlocutorAppearedOfflineHandler(InterlocutorAppearedOfflineEventArgs e);
public delegate void InterlocutorUpdatedPhotoHandler(InterlocutorUpdatedPhotoEventArgs e);
public delegate void InterlocutorDeletedPhotoHandler(InterlocutorDeletedPhotoEventArgs e);
#endregion

#region Sessions
public delegate void CreatedSessionHandler(CreatedSessionEventArgs e);
public delegate void ClosedSessionHandler(ClosedSessionEventArgs e);
#endregion

#region Assessments
public delegate void CreatedFinalAssessmentHandler(CreatedFinalAssessmentEventArgs e);
public delegate void CreatedAssessmentHandler(CreatedAssessmentEventArgs e);
public delegate void ChangedAssessmentHandler(ChangedAssessmentEventArgs e);
public delegate void DeletedAssessmentHandler(DeletedAssessmentEventArgs e);
#endregion

#region Timetable
public delegate void ChangedTimetableHandler(ChangedTimetableEventArgs e);
#endregion