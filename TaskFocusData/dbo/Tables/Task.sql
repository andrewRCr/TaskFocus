CREATE TABLE [dbo].[Task]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [UserId] NVARCHAR(128) NOT NULL, 
    [TaskName] NVARCHAR(MAX) NOT NULL, 
    [Completed] BIT NOT NULL DEFAULT 0, 
    [DateCompleted] DATETIME2 NULL, 
    [ContextId] INT NULL, 
    [ProjectId] INT NULL, 
    [DueDate] DATETIME2 NULL, 
    [CreatedDate] DATETIME2 NOT NULL DEFAULT getutcdate()
)
