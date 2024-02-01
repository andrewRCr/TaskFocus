CREATE PROCEDURE [dbo].spTask_GetAllForUser
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, TaskName, Completed, DateCompleted, ProjectId, ContextId, DueDate
	FROM dbo.Task
	WHERE UserId = @Id
	ORDER BY CreatedDate;
END
