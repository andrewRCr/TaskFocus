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
    [CreatedDate] DATETIME2 NOT NULL DEFAULT getutcdate(), 
    [InboxIndex] INT NULL, 
    [ProjectIndex] INT NULL, 
    [ContextIndex] INT NULL, 
    [Starred] BIT NOT NULL DEFAULT 0, 
    [TodayIndex] INT NULL, 
    [CleanedUp] BIT NOT NULL DEFAULT 0, 
    [ClientLastUpdated] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(), 
    [ServerLastUpdated] DATETIMEOFFSET NOT NULL DEFAULT getutcdate(), 
    CONSTRAINT [FK_Task_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id),
    CONSTRAINT [FK_Task_ToProject] FOREIGN KEY (ProjectId) REFERENCES Project(Id), 
    CONSTRAINT [FK_Task_ToContext] FOREIGN KEY (ContextId) REFERENCES Context(Id)

)
