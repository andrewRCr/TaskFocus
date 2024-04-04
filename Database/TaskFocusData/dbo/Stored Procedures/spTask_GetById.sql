CREATE PROCEDURE [dbo].[spTask_GetById]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, Task.UserId, TaskName, Task.Completed, Task.DateCompleted, ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName,
		   Task.InboxIndex, Task.ProjectIndex, Task.ContextIndex, Task.Starred, Task.TodayIndex
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id
	WHERE Task.Id = @Id
END
