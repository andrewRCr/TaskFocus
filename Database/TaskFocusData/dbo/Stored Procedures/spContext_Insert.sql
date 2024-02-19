CREATE PROCEDURE [dbo].[spContext_Insert]
	@Id int = NULL,
	@UserId nvarchar(128),
	@ContextName nvarchar(128) = NULL

AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Context(UserId, ContextName)
	VALUES(@UserId, @ContextName);
END