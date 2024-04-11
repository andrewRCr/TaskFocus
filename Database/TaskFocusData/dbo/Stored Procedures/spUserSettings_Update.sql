CREATE PROCEDURE [dbo].[spUserSettings_Update]
	@Id nvarchar(128),
	@CleanUpImmediately bit,
	@CleanUpDelayDays int

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.UserSettings
	SET CleanUpImmediately = cast(@CleanUpImmediately as bit), 
		CleanUpDelayDays = @CleanUpDelayDays
	WHERE Id = @Id;
END