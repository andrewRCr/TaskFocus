CREATE PROCEDURE [dbo].spTask_GetAllForUser
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, TaskName, Task.Completed, Task.DateCompleted, Task.ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName, Context.ContextName
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id
	LEFT JOIN dbo.Context ON dbo.Task.ContextId = dbo.Context.Id
	WHERE Task.UserId = @Id
	ORDER BY CreatedDate
END
