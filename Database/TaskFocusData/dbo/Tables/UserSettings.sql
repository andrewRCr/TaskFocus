CREATE TABLE [dbo].[UserSettings]
(
	[Id] NCHAR(128) NOT NULL PRIMARY KEY, 
    [CleanUpImmediately] BIT NOT NULL DEFAULT 0, 
    [CleanUpDelayDays] INT NOT NULL DEFAULT 7, 
    [DeleteDelayDays] INT NOT NULL DEFAULT 30
)
