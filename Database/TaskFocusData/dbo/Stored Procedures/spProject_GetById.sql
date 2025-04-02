CREATE PROCEDURE [dbo].[spProject_GetById]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UserId, ProjectName, ContextId, DueDate, OrderIndex, ServerLastUpdated, ClientLastUpdated
	FROM dbo.Project
	WHERE Id = @Id
END
