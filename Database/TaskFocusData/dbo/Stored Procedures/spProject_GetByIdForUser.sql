CREATE PROCEDURE [dbo].[spProject_GetByIdForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UserId, ProjectName, ContextId, DueDate, OrderIndex, ServerLastUpdated, ClientLastUpdated
	FROM dbo.Project
	WHERE Id = @Id AND UserId = @UserId
END
