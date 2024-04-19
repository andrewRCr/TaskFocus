CREATE PROCEDURE [dbo].[spUser_Update]
	@Id NVARCHAR(128),
	@FirstName NVARCHAR(50),
	@LastName NVARCHAR(50),
	@Email NVARCHAR(256),
	@CreatedDate datetime2(7)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [dbo].[User]
	SET FirstName = @FirstName, LastName = @LastName, Email = @Email
	WHERE Id = @Id;
END
