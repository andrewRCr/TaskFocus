CREATE PROCEDURE [dbo].[spProject_GetAllForUser]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Project.Id, ProjectName, ContextId, DueDate, ContextName
	FROM dbo.Project
	LEFT JOIN dbo.Context ON dbo.Project.ContextId = dbo.Context.Id
	WHERE Project.UserId = @Id
END