CREATE PROCEDURE [dbo].[spProject_UpdateForUser]
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
	WHERE Id = @Id AND UserId = @UserId
		AND (@ContextId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Context WITH (HOLDLOCK) WHERE Id = @ContextId AND UserId = @UserId
		));

	IF @@ROWCOUNT = 1
		SELECT 1;
	ELSE
		SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Project WHERE Id = @Id AND UserId = @UserId) THEN -1 ELSE 0 END;
END
