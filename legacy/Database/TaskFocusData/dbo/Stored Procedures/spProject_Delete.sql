CREATE PROCEDURE [dbo].[spProject_Delete]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Project
	WHERE Id = @Id;
END
