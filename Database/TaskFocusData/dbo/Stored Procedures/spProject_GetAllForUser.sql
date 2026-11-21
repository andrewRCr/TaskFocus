CREATE PROCEDURE [dbo].[spProject_GetAllForUser]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Project.Id, ProjectName, ContextId, DueDate, ContextName, Project.OrderIndex, Project.ClientLastUpdated, Project.ServerLastUpdated
	FROM dbo.Project
	LEFT JOIN dbo.Context ON dbo.Project.ContextId = dbo.Context.Id AND Context.UserId = Project.UserId
	WHERE Project.UserId = @Id
END