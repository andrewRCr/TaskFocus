CREATE PROCEDURE [dbo].[spContext_GetById]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, UserId, ContextName, OrderIndex
	FROM dbo.Context
	WHERE Id = @Id
END