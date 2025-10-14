CREATE PROCEDURE [dbo].spTask_GetAllForUser
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, TaskName, Task.Completed, Task.DateCompleted, Task.ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName, Context.ContextName,
		   Task.InboxIndex, Task.ProjectIndex, Task.ContextIndex, Task.Starred, Task.TodayIndex, Task.CleanedUp, Task.ClientLastUpdated, Task.ServerLastUpdated
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id
	LEFT JOIN dbo.Context ON dbo.Task.ContextId = dbo.Context.Id
	WHERE Task.UserId = @Id
	ORDER BY CreatedDate
END
