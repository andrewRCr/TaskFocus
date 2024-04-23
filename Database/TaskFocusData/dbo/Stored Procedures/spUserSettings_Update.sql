CREATE PROCEDURE [dbo].[spUserSettings_Update]
	@Id nvarchar(128),
	@CleanUpImmediately bit,
	@CleanUpDelayDays int,
	@DeleteDelayDays int

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.UserSettings
	SET CleanUpImmediately = cast(@CleanUpImmediately as bit), 
		CleanUpDelayDays = @CleanUpDelayDays,
		DeleteDelayDays = @DeleteDelayDays
	WHERE Id = @Id;
END