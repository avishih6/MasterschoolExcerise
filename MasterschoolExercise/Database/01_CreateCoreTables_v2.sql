-- ============================================
-- AdmissionProcess Database Schema v2
-- MS SQL Server
-- 
-- Key Design Decisions:
-- 1. Nodes table = pure definition (what exists)
-- 2. NodeHierarchy table = structure (where it sits)
-- 3. Role derived from hierarchy (has parent = task)
-- 4. Base conditions on Node, overridable in Hierarchy
-- ============================================

-- ============================================
-- USERS TABLE
-- ============================================
CREATE TABLE Users (
    Id NVARCHAR(36) PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);

GO

-- ============================================
-- NODES TABLE
-- Pure definition - what the node IS, not where it sits
-- ============================================
CREATE TABLE Nodes (
    Id INT PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    
    -- Base conditions (can be overridden in hierarchy)
    BasePassConditionJson NVARCHAR(MAX) NULL,        -- e.g., {"type":"score_threshold","field":"score","threshold":75}
    BaseVisibilityConditionJson NVARCHAR(MAX) NULL,  -- e.g., {"type":"score_range","field":"iq_score","min":60,"max":75}
    
    -- Payload this node expects (doesn't change per hierarchy)
    PayloadIdentifiersJson NVARCHAR(MAX) NULL,       -- e.g., ["score","timestamp","test_id"]
    
    -- Derived facts to store when this node completes
    DerivedFactMappingsJson NVARCHAR(MAX) NULL,      -- e.g., [{"sourceField":"score","targetFactName":"iq_score"}]
    
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT UQ_Nodes_Name UNIQUE (Name)
);

GO

-- ============================================
-- NODE HIERARCHY TABLE
-- Defines structure: parent-child relationships and order
-- Role is DERIVED: ParentNodeId IS NULL = Step, otherwise = Task
-- 
-- FUTURE: Add columns for scoped hierarchies:
--   ScopeLevel NVARCHAR(20) NULL,     -- 'country', 'university', 'program'
--   ScopeEntityId NVARCHAR(100) NULL  -- 'IL', 'tel_aviv_uni', etc.
--   (NULL = global/default hierarchy)
-- ============================================
CREATE TABLE NodeHierarchy (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    ParentNodeId INT NULL,              -- NULL = root step, NOT NULL = task under that step
    [Order] INT NOT NULL,               -- Order within parent (or among root steps)
    
    -- Condition overrides (if NULL, use Node's base condition)
    PassConditionOverrideJson NVARCHAR(MAX) NULL,
    VisibilityConditionOverrideJson NVARCHAR(MAX) NULL,
    
    -- Recovery task pattern: this hierarchy entry only available if another node failed
    RequiresPreviousNodeFailedId INT NULL,
    
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT FK_NodeHierarchy_Node FOREIGN KEY (NodeId) REFERENCES Nodes(Id),
    CONSTRAINT FK_NodeHierarchy_Parent FOREIGN KEY (ParentNodeId) REFERENCES Nodes(Id),
    CONSTRAINT FK_NodeHierarchy_RequiresPrevious FOREIGN KEY (RequiresPreviousNodeFailedId) REFERENCES Nodes(Id),
    
    -- Each node appears once in the hierarchy (for now - will change with scoped hierarchies)
    CONSTRAINT UQ_NodeHierarchy_NodeId UNIQUE (NodeId)
);

-- Get children of a step (tasks under a step)
CREATE NONCLUSTERED INDEX IX_NodeHierarchy_ParentNodeId 
    ON NodeHierarchy(ParentNodeId) 
    INCLUDE ([Order], NodeId);

-- Get root steps (ParentNodeId IS NULL)
CREATE NONCLUSTERED INDEX IX_NodeHierarchy_RootSteps 
    ON NodeHierarchy([Order]) 
    WHERE ParentNodeId IS NULL;

GO

-- ============================================
-- USER PROGRESS TABLE
-- ============================================
CREATE TABLE UserProgress (
    UserId NVARCHAR(36) NOT NULL,
    CurrentNodeId INT NULL,             -- Current step or task node
    CachedOverallStatus TINYINT NOT NULL DEFAULT 0,  -- 0=InProgress, 1=Accepted, 2=Rejected
    CacheUpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION NOT NULL,
    
    CONSTRAINT PK_UserProgress PRIMARY KEY (UserId),
    CONSTRAINT FK_UserProgress_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserProgress_CurrentNode FOREIGN KEY (CurrentNodeId) REFERENCES Nodes(Id)
);

GO

-- ============================================
-- USER NODE STATUSES TABLE
-- Per-user, per-node completion tracking
-- ============================================
CREATE TABLE UserNodeStatuses (
    UserId NVARCHAR(36) NOT NULL,
    NodeId INT NOT NULL,
    Status TINYINT NOT NULL,            -- 0=NotStarted, 1=Accepted, 2=Rejected
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_UserNodeStatuses PRIMARY KEY (UserId, NodeId),
    CONSTRAINT FK_UserNodeStatuses_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserNodeStatuses_Node FOREIGN KEY (NodeId) REFERENCES Nodes(Id),
    CONSTRAINT CK_UserNodeStatuses_Status CHECK (Status IN (0, 1, 2))
);

CREATE NONCLUSTERED INDEX IX_UserNodeStatuses_UserId 
    ON UserNodeStatuses(UserId) 
    INCLUDE (NodeId, Status, UpdatedAt);

GO

-- ============================================
-- USER DERIVED FACTS TABLE
-- ============================================
CREATE TABLE UserDerivedFacts (
    UserId NVARCHAR(36) NOT NULL,
    FactName NVARCHAR(100) NOT NULL,
    FactValue NVARCHAR(MAX) NOT NULL,
    FactValueType NVARCHAR(20) NOT NULL,  -- 'number', 'string', 'boolean'
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_UserDerivedFacts PRIMARY KEY (UserId, FactName),
    CONSTRAINT FK_UserDerivedFacts_User FOREIGN KEY (UserId) REFERENCES Users(Id)
);

GO

-- ============================================
-- HELPER VIEW: Flattened node with hierarchy info
-- Joins Node definition with its hierarchy position
-- ============================================
CREATE OR ALTER VIEW vw_FlowNodes AS
SELECT 
    n.Id,
    n.Name,
    CASE WHEN nh.ParentNodeId IS NULL THEN 'Step' ELSE 'Task' END AS Role,
    nh.ParentNodeId,
    nh.[Order],
    -- Effective conditions (override wins if set)
    COALESCE(nh.PassConditionOverrideJson, n.BasePassConditionJson) AS EffectivePassConditionJson,
    COALESCE(nh.VisibilityConditionOverrideJson, n.BaseVisibilityConditionJson) AS EffectiveVisibilityConditionJson,
    n.PayloadIdentifiersJson,
    n.DerivedFactMappingsJson,
    nh.RequiresPreviousNodeFailedId,
    n.IsActive AND nh.IsActive AS IsActive
FROM Nodes n
INNER JOIN NodeHierarchy nh ON n.Id = nh.NodeId;

GO

PRINT 'Core tables v2 created successfully.';
PRINT 'Key changes from v1:';
PRINT '  - Nodes table: pure definition (no Role, no ParentId)';
PRINT '  - NodeHierarchy: defines structure and order';
PRINT '  - Role derived from hierarchy (ParentNodeId NULL = Step)';
PRINT '  - Base conditions on Node, overridable in Hierarchy';
