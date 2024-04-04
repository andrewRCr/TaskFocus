CREATE PROCEDURE [dbo].spTask_GetInboxForUser
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, 
		   TaskName, 
		   Task.Completed, 
		   Task.DateCompleted, 
		   Task.ProjectId, 
		   Task.ContextId, 
		   Task.DueDate, 
		   Project.ProjectName, 
		   Context.ContextName, 
		   Task.Starred, 
		   Task.TodayIndex,
		   Task.InboxIndex,
		   Task.ProjectIndex,
		   Task.ContextIndex
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id
	LEFT JOIN dbo.Context ON dbo.Task.ContextId = dbo.Context.Id
	WHERE Task.UserId = @Id AND (Task.ProjectId IS NULL OR Task.ContextId IS NULL)
	ORDER BY InboxIndex
END
