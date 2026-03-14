IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Comments] (
    [Id] int NOT NULL IDENTITY,
    [IssueId] int NOT NULL,
    [UserId] int NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Comments] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [IssueHistories] (
    [Id] int NOT NULL IDENTITY,
    [IssueId] int NOT NULL,
    [OldStatus] nvarchar(max) NOT NULL,
    [NewStatus] nvarchar(max) NOT NULL,
    [ChangedByUserId] int NOT NULL,
    [ChangedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_IssueHistories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Issues] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Priority] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ProjectId] int NOT NULL,
    [AssignedUserId] int NULL,
    CONSTRAINT [PK_Issues] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Projects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260220033611_InitialCreate', N'8.0.8');
GO

COMMIT;
GO

