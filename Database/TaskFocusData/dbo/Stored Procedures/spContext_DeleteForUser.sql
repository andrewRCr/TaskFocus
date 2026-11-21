CREATE PROCEDURE [dbo].[spContext_DeleteForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Context
	WHERE Id = @Id AND UserId = @UserId;

	SELECT @@ROWCOUNT;
END
