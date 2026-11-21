CREATE PROCEDURE [dbo].[spTask_GetByIdForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, Task.UserId, TaskName, Task.Completed, Task.DateCompleted, ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName,
		   Task.InboxIndex, Task.ProjectIndex, Task.ContextIndex, Task.Starred, Task.TodayIndex, Task.CleanedUp,
		   Task.ClientLastUpdated, Task.ServerLastUpdated
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id AND Project.UserId = Task.UserId
	WHERE Task.Id = @Id AND Task.UserId = @UserId
END
