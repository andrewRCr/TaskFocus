CREATE PROCEDURE [dbo].[spContext_UpdateForUser]
	@Id int,
	@UserId nvarchar(128),
	@ContextName nvarchar(128),
	@OrderIndex int,
	@ServerLastUpdated datetimeoffset(7),
	@ClientLastUpdated datetimeoffset(7)

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Context
	SET ContextName = @ContextName,
		OrderIndex = @OrderIndex,
		ServerLastUpdated = @ServerLastUpdated,
		ClientLastUpdated = @ClientLastUpdated
	WHERE Id = @Id AND UserId = @UserId;

	SELECT @@ROWCOUNT;
END
