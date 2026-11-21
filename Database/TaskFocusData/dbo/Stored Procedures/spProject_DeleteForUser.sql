CREATE PROCEDURE [dbo].[spProject_DeleteForUser]
	@Id int,
	@UserId nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Project
	WHERE Id = @Id AND UserId = @UserId;

	SELECT @@ROWCOUNT;
END
