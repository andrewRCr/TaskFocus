CREATE PROCEDURE [dbo].[spContext_GetAllForUser]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, ContextName, OrderIndex
	FROM dbo.Context
	WHERE UserId = @Id
END
