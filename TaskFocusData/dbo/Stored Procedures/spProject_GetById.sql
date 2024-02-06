CREATE PROCEDURE [dbo].[spProject_GetById]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UserId, ProjectName, ContextId, DueDate, ProjectTasks
	FROM dbo.Project
	WHERE Id = @Id
END
