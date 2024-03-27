CREATE PROCEDURE [dbo].[spContext_Update]
	@Id int,
	@UserId nvarchar(128),
	@ContextName nvarchar(128),
	@OrderIndex int

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Context
	SET ContextName = @ContextName, 
		OrderIndex = @OrderIndex
	WHERE Id = @Id;
END