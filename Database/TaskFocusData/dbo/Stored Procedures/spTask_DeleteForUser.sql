CREATE PROCEDURE [dbo].[spTask_DeleteForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Task
	WHERE Id = @Id AND UserId = @UserId;

	SELECT @@ROWCOUNT;
END
