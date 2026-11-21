CREATE PROCEDURE [dbo].[spTask_InsertForUser]
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
	@TodayIndex int = NULL,
	@CleanedUp bit = 0,
	@ClientLastUpdated datetimeoffset(7),
	@ServerLastUpdated datetimeoffset(7)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Task(UserId, TaskName, Completed, DateCompleted, ContextId, ProjectId, DueDate, InboxIndex, ProjectIndex, ContextIndex, Starred, TodayIndex, CleanedUp, ClientLastUpdated, ServerLastUpdated)
	OUTPUT inserted.*
	SELECT @UserId, @TaskName, cast(@Completed as bit), @DateCompleted, @ContextId, @ProjectId, @DueDate, @InboxIndex, @ProjectIndex, @ContextIndex, @Starred, @TodayIndex, @CleanedUp, @ClientLastUpdated, @ServerLastUpdated
	WHERE (@ProjectId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Project WITH (HOLDLOCK) WHERE Id = @ProjectId AND UserId = @UserId
		))
		AND (@ContextId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Context WITH (HOLDLOCK) WHERE Id = @ContextId AND UserId = @UserId
		));
END
