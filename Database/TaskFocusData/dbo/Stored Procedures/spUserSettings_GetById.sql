CREATE PROCEDURE [dbo].[spUserSettings_GetById]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, CleanUpImmediately, CleanUpDelayDays
	FROM [dbo].UserSettings
	WHERE Id = @Id;
END
