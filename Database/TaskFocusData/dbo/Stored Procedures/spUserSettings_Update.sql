CREATE PROCEDURE [dbo].[spUserSettings_Update]
	@Id nvarchar(128),
	@CleanUpImmediately bit,
	@CleanUpDelayDays int,
	@DeleteDelayDays int,
	@ServerLastUpdated datetimeoffset(7),
	@ClientLastUpdated datetimeoffset(7)

AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.UserSettings
	SET CleanUpImmediately = cast(@CleanUpImmediately as bit), 
		CleanUpDelayDays = @CleanUpDelayDays,
		DeleteDelayDays = @DeleteDelayDays,
		ServerLastUpdated = @ServerLastUpdated, 
		ClientLastUpdated = @ClientLastUpdated
	WHERE Id = @Id;
END