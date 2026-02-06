-- ============================================
-- AdmissionProcess Database Schema
-- MS SQL Server
-- Version: 1.0
-- ============================================

-- ============================================
-- USERS TABLE
-- Core user entity
-- ============================================
CREATE TABLE Users (
    Id NVARCHAR(36) PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

-- Index for email lookups (login, duplicate check)
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);

GO

-- ============================================
-- FLOW NODES TABLE
-- Steps and Tasks configuration (adjacency list pattern)
-- ============================================
CREATE TABLE FlowNodes (
    Id INT PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Role TINYINT NOT NULL,              -- 1=Step, 2=Task (maps to NodeRole enum)
    ParentId INT NULL,                  -- FK to parent step (NULL for root steps)
    [Order] INT NOT NULL,               -- Display/processing order within parent
    IsActive BIT NOT NULL DEFAULT 1,    -- Soft delete / feature flag
    
    CONSTRAINT FK_FlowNodes_Parent FOREIGN KEY (ParentId) REFERENCES FlowNodes(Id),
    CONSTRAINT UQ_FlowNodes_Name UNIQUE (Name)
);

-- Index for parent lookups (get children of a step)
CREATE NONCLUSTERED INDEX IX_FlowNodes_ParentId 
    ON FlowNodes(ParentId) 
    INCLUDE ([Order], Role);

-- Filtered index for root steps lookup (Role=1, ParentId IS NULL)
CREATE NONCLUSTERED INDEX IX_FlowNodes_RootSteps 
    ON FlowNodes(Role, [Order]) 
    WHERE Role = 1 AND ParentId IS NULL;

GO

-- ============================================
-- NODE CONDITIONS TABLE
-- Pass and Visibility conditions for nodes
-- ============================================
CREATE TABLE NodeConditions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    ConditionType NVARCHAR(50) NOT NULL,      -- 'visibility' or 'pass'
    EvaluationType NVARCHAR(50) NOT NULL,     -- 'score_threshold', 'score_range', 'decision_equals', etc.
    Field NVARCHAR(100) NULL,                 -- Field to evaluate (e.g., 'score', 'decision', 'iq_score')
    MinValue DECIMAL(18,4) NULL,              -- For range conditions
    MaxValue DECIMAL(18,4) NULL,              -- For range conditions
    ThresholdValue DECIMAL(18,4) NULL,        -- For threshold conditions (e.g., score > 75)
    ExpectedValue NVARCHAR(200) NULL,         -- For equality conditions (e.g., decision = 'passed_interview')
    RequiresPreviousTaskFailedId INT NULL,    -- Recovery task pattern: task only available if this task failed
    
    CONSTRAINT FK_NodeConditions_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id),
    CONSTRAINT FK_NodeConditions_RequiresPrevious FOREIGN KEY (RequiresPreviousTaskFailedId) REFERENCES FlowNodes(Id),
    CONSTRAINT CK_NodeConditions_ConditionType CHECK (ConditionType IN ('visibility', 'pass'))
);

CREATE NONCLUSTERED INDEX IX_NodeConditions_NodeId ON NodeConditions(NodeId);

GO

-- ============================================
-- NODE PAYLOAD IDENTIFIERS TABLE
-- Which fields each task expects in webhook payload
-- ============================================
CREATE TABLE NodePayloadIdentifiers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    IdentifierName NVARCHAR(100) NOT NULL,
    
    CONSTRAINT FK_NodePayloadIdentifiers_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id)
);

CREATE NONCLUSTERED INDEX IX_NodePayloadIdentifiers_NodeId ON NodePayloadIdentifiers(NodeId);

GO

-- ============================================
-- DERIVED FACT MAPPINGS TABLE
-- Maps payload fields to derived facts stored for visibility conditions
-- ============================================
CREATE TABLE DerivedFactMappings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    SourceField NVARCHAR(100) NOT NULL,       -- Field name in payload (e.g., 'score')
    TargetFactName NVARCHAR(100) NOT NULL,    -- Fact name to store (e.g., 'iq_score')
    
    CONSTRAINT FK_DerivedFactMappings_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id)
);

CREATE NONCLUSTERED INDEX IX_DerivedFactMappings_NodeId ON DerivedFactMappings(NodeId);

GO

-- ============================================
-- USER PROGRESS TABLE
-- Main progress tracking with cached current position
-- ============================================
CREATE TABLE UserProgress (
    UserId NVARCHAR(36) NOT NULL,
    CurrentStepId INT NULL,
    CurrentTaskId INT NULL,
    CachedOverallStatus TINYINT NOT NULL DEFAULT 0,  -- 0=InProgress, 1=Accepted, 2=Rejected (UserStatus enum)
    CacheUpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,                   -- Optimistic concurrency control
    
    CONSTRAINT PK_UserProgress PRIMARY KEY (UserId),
    CONSTRAINT FK_UserProgress_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserProgress_CurrentStep FOREIGN KEY (CurrentStepId) REFERENCES FlowNodes(Id),
    CONSTRAINT FK_UserProgress_CurrentTask FOREIGN KEY (CurrentTaskId) REFERENCES FlowNodes(Id)
);

GO

-- ============================================
-- USER NODE STATUSES TABLE
-- Per-user, per-node status tracking
-- ============================================
CREATE TABLE UserNodeStatuses (
    UserId NVARCHAR(36) NOT NULL,
    NodeId INT NOT NULL,
    Status TINYINT NOT NULL,                  -- 0=NotStarted, 1=Accepted, 2=Rejected (ProgressStatus enum)
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_UserNodeStatuses PRIMARY KEY (UserId, NodeId),
    CONSTRAINT FK_UserNodeStatuses_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserNodeStatuses_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id),
    CONSTRAINT CK_UserNodeStatuses_Status CHECK (Status IN (0, 1, 2))
);

-- Covering index for getting all statuses for a user
CREATE NONCLUSTERED INDEX IX_UserNodeStatuses_UserId 
    ON UserNodeStatuses(UserId) 
    INCLUDE (NodeId, Status, UpdatedAt);

GO

-- ============================================
-- USER DERIVED FACTS TABLE
-- Stored facts per user (e.g., iq_score for visibility conditions)
-- ============================================
CREATE TABLE UserDerivedFacts (
    UserId NVARCHAR(36) NOT NULL,
    FactName NVARCHAR(100) NOT NULL,
    FactValue NVARCHAR(MAX) NOT NULL,
    FactValueType NVARCHAR(20) NOT NULL,      -- 'number', 'string', 'boolean'
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_UserDerivedFacts PRIMARY KEY (UserId, FactName),
    CONSTRAINT FK_UserDerivedFacts_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT CK_UserDerivedFacts_ValueType CHECK (FactValueType IN ('number', 'string', 'boolean'))
);

GO

PRINT 'Core tables created successfully.';
