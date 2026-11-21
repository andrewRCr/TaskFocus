CREATE PROCEDURE [dbo].[spTask_GetAllForProjectIdForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, TaskName, Task.Completed, Task.DateCompleted, Task.ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName, Context.ContextName,
		   Task.InboxIndex, Task.ProjectIndex, Task.ContextIndex, Task.Starred, Task.TodayIndex, Task.CleanedUp, Task.ClientLastUpdated, Task.ServerLastUpdated
	FROM dbo.Task
		INNER JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id AND Project.UserId = Task.UserId
		LEFT JOIN dbo.Context ON dbo.Task.ContextId = dbo.Context.Id AND Context.UserId = Task.UserId
	WHERE Task.ProjectId = @Id AND Task.UserId = @UserId
	ORDER BY ProjectIndex
END
