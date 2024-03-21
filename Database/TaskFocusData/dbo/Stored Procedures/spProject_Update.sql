CREATE PROCEDURE [dbo].[spProject_Update]
	@Id int,
	@UserId nvarchar(128),
	@ProjectName nvarchar(max),
	@Completed bit,
	@DateCompleted datetime2,
	@ContextId int,
	@ContextName nvarchar(128),
	@DueDate datetime2
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Project
	SET ProjectName = @ProjectName, 
		Completed = cast(@Completed as bit), 
		DateCompleted = @DateCompleted, 
		ContextId = @ContextId, 
		DueDate = @DueDate
	WHERE Id = @Id;
END