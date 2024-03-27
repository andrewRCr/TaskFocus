CREATE PROCEDURE [dbo].[spProject_Insert]
	@Id int = NULL,
	@UserId nvarchar(128),
	@ProjectName nvarchar(max),
	@ContextId int = NULL,
	@ContextName nvarchar(128) = NULL,
	@DueDate datetime2 = NULL,
	@Completed bit = 0,
	@DateCompleted datetime2 = NULL,
	@OrderIndex int = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Project(UserId, ProjectName, ContextId, DueDate, Completed, DateCompleted, OrderIndex)
	VALUES(@UserId, @ProjectName, @ContextId, @DueDate, cast(@Completed as bit), @DateCompleted, @OrderIndex);
END
