CREATE PROCEDURE [dbo].[spProject_Update]
	@Id int,
	@UserId nvarchar(128),
	@ProjectName nvarchar(max),
	@Completed bit,
	@DateCompleted datetime2,
	@ContextId int,
	@ContextName nvarchar(128),
	@DueDate datetime2,
	@OrderIndex int,
	@ServerLastUpdated datetimeoffset(7),
	@ClientLastUpdated datetimeoffset(7)
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Project
	SET ProjectName = @ProjectName, 
		Completed = cast(@Completed as bit), 
		DateCompleted = @DateCompleted, 
		ContextId = @ContextId, 
		DueDate = @DueDate,
		OrderIndex = @OrderIndex,
		ServerLastUpdated = @ServerLastUpdated, 
		ClientLastUpdated = @ClientLastUpdated
	WHERE Id = @Id;
END