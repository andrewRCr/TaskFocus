CREATE PROCEDURE [dbo].[spContext_Update]
	@Id int,
	@UserId nvarchar(128),
	@ContextName nvarchar(128)

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Context
	SET ContextName = ContextName
	WHERE Id = @Id;
END