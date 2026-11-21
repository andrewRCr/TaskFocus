CREATE PROCEDURE [dbo].[spContext_GetByIdForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UserId, ContextName, OrderIndex, ServerLastUpdated, ClientLastUpdated
	FROM dbo.Context
	WHERE Id = @Id AND UserId = @UserId
END
