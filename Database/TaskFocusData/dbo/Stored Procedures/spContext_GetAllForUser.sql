CREATE PROCEDURE [dbo].[spContext_GetAllForUser]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, ContextName
	FROM dbo.Context
	WHERE UserId = @Id
END
