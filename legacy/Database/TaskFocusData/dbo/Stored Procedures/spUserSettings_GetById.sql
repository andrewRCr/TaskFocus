CREATE PROCEDURE [dbo].[spUserSettings_GetById]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, CleanUpImmediately, CleanUpDelayDays, DeleteDelayDays, ServerLastUpdated, ClientLastUpdated
	FROM [dbo].UserSettings
	WHERE Id = @Id;
END
