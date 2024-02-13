CREATE PROCEDURE [dbo].spTask_GetInboxForUser
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Task.Id, TaskName, Completed, DateCompleted, ProjectId, Task.ContextId, Task.DueDate, Project.ProjectName
	FROM dbo.Task
	LEFT JOIN dbo.Project ON dbo.Task.ProjectId = dbo.Project.Id
	WHERE Task.UserId = @Id AND Task.ProjectId IS NULL
	ORDER BY CreatedDate
END
