CREATE PROCEDURE [dbo].[spProject_InsertForUser]
	@Id int = NULL,
	@UserId nvarchar(128),
	@ProjectName nvarchar(max),
	@ContextId int = NULL,
	@ContextName nvarchar(128) = NULL,
	@DueDate datetime2 = NULL,
	@Completed bit = 0,
	@DateCompleted datetime2 = NULL,
	@OrderIndex int = NULL,
	@ClientLastUpdated datetimeoffset(7),
	@ServerLastUpdated datetimeoffset(7)
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Project(UserId, ProjectName, ContextId, DueDate, Completed, DateCompleted, OrderIndex, ClientLastUpdated, ServerLastUpdated)
	OUTPUT inserted.*
	SELECT @UserId, @ProjectName, @ContextId, @DueDate, cast(@Completed as bit), @DateCompleted, @OrderIndex, @ClientLastUpdated, @ServerLastUpdated
	WHERE (@ContextId IS NULL OR EXISTS (
			SELECT 1 FROM dbo.Context WITH (HOLDLOCK) WHERE Id = @ContextId AND UserId = @UserId
		));
END
