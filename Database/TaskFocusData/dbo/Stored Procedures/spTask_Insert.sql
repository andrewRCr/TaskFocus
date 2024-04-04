CREATE PROCEDURE [dbo].[spTask_Insert]
	@Id int = NULL,
	@UserId nvarchar(128),
	@TaskName nvarchar(max),
	@Completed bit = 0,
	@DateCompleted datetime2 = NULL,
	@ContextId int = NULL,
	@ContextName nvarchar(128) = NULL,
	@ProjectId int = NULL,
	@ProjectName nvarchar(128) = NULL,
	@DueDate datetime2 = NULL,
	@InboxIndex int = NULL,
	@ProjectIndex int = NULL,
	@ContextIndex int = NULL,
	@Starred bit = 0,
	@TodayIndex int = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Task(UserId, TaskName, Completed, DateCompleted, ContextId, ProjectId, DueDate, InboxIndex, ProjectIndex, ContextIndex, Starred, TodayIndex)
	VALUES(@UserId, @TaskName, cast(@Completed as bit), @DateCompleted, @ContextId, @ProjectId, @DueDate, @InboxIndex, @ProjectIndex, @ContextIndex, @Starred, @TodayIndex);
END