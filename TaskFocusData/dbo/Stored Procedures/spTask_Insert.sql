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
	@DueDate datetime2 = NULL
AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO dbo.Task(UserId, TaskName, Completed, DateCompleted, ContextId, ProjectId, DueDate)
	VALUES(@UserId, @TaskName, cast(@Completed as bit), @DateCompleted, @ContextId, @ProjectId, @DueDate);
END