CREATE PROCEDURE [dbo].[spUser_UpdateName]
	@Id NVARCHAR(128),
	@FirstName NVARCHAR(50),
	@LastName NVARCHAR(50),
	@ServerLastUpdated datetimeoffset(7),
	@ClientLastUpdated datetimeoffset(7)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [dbo].[User]
	SET FirstName = @FirstName, LastName = @LastName,
	ServerLastUpdated = @ServerLastUpdated, ClientLastUpdated = @ClientLastUpdated
	WHERE Id = @Id;
	SELECT @@ROWCOUNT;
END
