CREATE PROCEDURE [dbo].[spContext_Insert]
	@Id int = NULL,
	@UserId nvarchar(128),
	@ContextName nvarchar(128) = NULL,
	@OrderIndex int = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Context(UserId, ContextName, OrderIndex)
	OUTPUT inserted.*
	VALUES(@UserId, @ContextName, @OrderIndex);
END