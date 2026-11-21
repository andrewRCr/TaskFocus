CREATE PROCEDURE [dbo].[spTask_UpdateForUser]
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
	@ContextIndex int,
	@Starred bit,
	@TodayIndex int,
	@CleanedUp bit,
	@ClientLastUpdated datetimeoffset(7),
	@ServerLastUpdated datetimeoffset(7)
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
		ContextIndex = @ContextIndex,
		Starred = cast(@Starred as bit),
		TodayIndex = @TodayIndex,
		CleanedUp = cast(@CleanedUp as bit),
		ClientLastUpdated = @ClientLastUpdated,
		ServerLastUpdated = @ServerLastUpdated
	WHERE Id = @Id AND UserId = @UserId
		AND (@ProjectId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Project WITH (HOLDLOCK) WHERE Id = @ProjectId AND UserId = @UserId
		))
		AND (@ContextId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Context WITH (HOLDLOCK) WHERE Id = @ContextId AND UserId = @UserId
		));

	IF @@ROWCOUNT = 1
		SELECT 1;
	ELSE
		SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Task WHERE Id = @Id AND UserId = @UserId) THEN -1 ELSE 0 END;
END
