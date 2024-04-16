CREATE PROCEDURE [dbo].[spUser_GetById]
	@Id NVARCHAR(128)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Id, FirstName, LastName, Email, CreatedDate
	FROM [dbo].[User]
	WHERE Id = @Id;
END