CREATE TABLE [dbo].[Project]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [UserId] NVARCHAR(128) NOT NULL, 
    [ProjectName] NVARCHAR(128) NOT NULL,
    [ContextId] INT NULL, 
    [DueDate] DATETIME2 NULL, 
    [ProjectTasks] NVARCHAR(MAX) NULL
)
