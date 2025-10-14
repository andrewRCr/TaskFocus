CREATE PROCEDURE [dbo].[spUserSettings_Insert]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [dbo].[UserSettings](Id)
	VALUES(@Id);
END
