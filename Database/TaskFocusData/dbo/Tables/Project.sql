CREATE TABLE [dbo].[Project]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [UserId] NVARCHAR(128) NOT NULL, 
    [ProjectName] NVARCHAR(128) NOT NULL,
    [ContextId] INT NULL, 
    [DueDate] DATETIME2 NULL, 
    [Completed] BIT NOT NULL DEFAULT 0, 
    [DateCompleted] DATETIME2 NULL, 
    [ProjectTasks] NVARCHAR(MAX) NULL, 
    CONSTRAINT [FK_Project_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id), 
    CONSTRAINT [FK_Project_ToContext] FOREIGN KEY (ContextId) REFERENCES Context(Id)
)
