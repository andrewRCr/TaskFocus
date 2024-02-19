CREATE PROCEDURE [dbo].[spTask_Delete]
	@Id int
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM dbo.Task
	WHERE Id = @Id;
END