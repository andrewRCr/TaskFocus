CREATE PROCEDURE [dbo].[spContext_Insert]
	@Id int = NULL,
	@UserId nvarchar(128),
	@ContextName nvarchar(128) = NULL,
	@OrderIndex int = NULL,
	@ClientLastUpdated datetimeoffset(7),
	@ServerLastUpdated datetimeoffset(7)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Context(UserId, ContextName, OrderIndex, ClientLastUpdated, ServerLastUpdated)
	OUTPUT inserted.*
	VALUES(@UserId, @ContextName, @OrderIndex, @ClientLastUpdated, @ServerLastUpdated);
END