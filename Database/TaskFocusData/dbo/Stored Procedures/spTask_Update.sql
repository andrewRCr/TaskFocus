CREATE PROCEDURE [dbo].[spTask_Update]
	@Id int,
	@UserId nvarchar(128),
	@TaskName nvarchar(max),
	@Completed bit,
	@DateCompleted datetime2,
	@ContextId int,
	@ContextName nvarchar(128),
	@ProjectId int,
	@ProjectName nvarchar(128),
	@DueDate datetime2,
	@InboxIndex int,
	@ProjectIndex int,
	@ContextIndex int
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE dbo.Task
	SET TaskName = @TaskName, 
		Completed = cast(@Completed as bit), 
		DateCompleted = @DateCompleted, 
		ContextId = @ContextId, 
		ProjectId = @ProjectId, 
		DueDate = @DueDate,
		InboxIndex = @InboxIndex,
		ProjectIndex = @ProjectIndex,
		ContextIndex = @ContextIndex
	WHERE Id = @Id;
END
