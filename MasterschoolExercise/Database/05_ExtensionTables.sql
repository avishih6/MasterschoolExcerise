-- ============================================
-- AdmissionProcess Extension Tables
-- MS SQL Server
-- 
-- These tables support potential extensions
-- that may be discussed in interviews
-- ============================================

-- ============================================
-- EXTENSION 1: Country/Institution-Specific Flows
-- Allows customizing flow per country or university
-- ============================================
CREATE TABLE FlowOverrides (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OverrideLevel NVARCHAR(20) NOT NULL,      -- 'country', 'university', 'program'
    OverrideKey NVARCHAR(100) NOT NULL,       -- e.g., 'IL', 'university_123', 'program_456'
    NodeId INT NOT NULL,
    IsDisabled BIT NOT NULL DEFAULT 0,        -- Skip this node for this override
    NewOrder INT NULL,                        -- Override the display order
    NewPassConditionJson NVARCHAR(MAX) NULL,  -- Override pass condition
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_FlowOverrides_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id),
    CONSTRAINT UQ_FlowOverrides_Level_Key_Node UNIQUE (OverrideLevel, OverrideKey, NodeId)
);

CREATE NONCLUSTERED INDEX IX_FlowOverrides_Key ON FlowOverrides(OverrideLevel, OverrideKey);

GO

-- ============================================
-- EXTENSION 2: Step Deadlines/Expiration
-- Allows setting time limits on steps
-- ============================================
CREATE TABLE NodeDeadlines (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    DeadlineHours INT NOT NULL,               -- Hours allowed to complete from when step becomes active
    ExpirationAction NVARCHAR(20) NOT NULL,   -- 'reject', 'notify', 'extend'
    NotificationTemplateId INT NULL,          -- FK to notification templates
    GracePeriodHours INT NULL,                -- Extra time before hard rejection
    
    CONSTRAINT FK_NodeDeadlines_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id)
);

-- Track deadline status per user
CREATE TABLE UserDeadlineTracking (
    UserId NVARCHAR(36) NOT NULL,
    NodeId INT NOT NULL,
    StartedAt DATETIME2 NOT NULL,
    DeadlineAt DATETIME2 NOT NULL,
    ExtensionsGranted INT NOT NULL DEFAULT 0,
    NotificationSentAt DATETIME2 NULL,
    
    CONSTRAINT PK_UserDeadlineTracking PRIMARY KEY (UserId, NodeId),
    CONSTRAINT FK_UserDeadlineTracking_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserDeadlineTracking_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id)
);

GO

-- ============================================
-- EXTENSION 3: A/B Testing Different Flows
-- Allows running experiments with different flow configurations
-- ============================================
CREATE TABLE FlowExperiments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ExperimentName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    TargetPercentage DECIMAL(5,2) NOT NULL,   -- % of new users to include
    
    CONSTRAINT UQ_FlowExperiments_Name UNIQUE (ExperimentName)
);

CREATE TABLE FlowExperimentVariants (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ExperimentId INT NOT NULL,
    VariantName NVARCHAR(100) NOT NULL,       -- 'control', 'variant_a', 'variant_b'
    FlowConfigJson NVARCHAR(MAX) NOT NULL,    -- Modified flow configuration
    AllocationPercentage DECIMAL(5,2) NOT NULL,
    
    CONSTRAINT FK_FlowExperimentVariants_Experiment FOREIGN KEY (ExperimentId) REFERENCES FlowExperiments(Id)
);

CREATE TABLE UserExperimentAssignments (
    UserId NVARCHAR(36) NOT NULL,
    ExperimentId INT NOT NULL,
    VariantId INT NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_UserExperimentAssignments PRIMARY KEY (UserId, ExperimentId),
    CONSTRAINT FK_UserExperimentAssignments_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserExperimentAssignments_Experiment FOREIGN KEY (ExperimentId) REFERENCES FlowExperiments(Id),
    CONSTRAINT FK_UserExperimentAssignments_Variant FOREIGN KEY (VariantId) REFERENCES FlowExperimentVariants(Id)
);

GO

-- ============================================
-- EXTENSION 4: Step Dependencies (Parallel Steps)
-- Allows steps that can be done in any order
-- ============================================
CREATE TABLE NodeDependencies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    DependsOnNodeId INT NOT NULL,
    DependencyType NVARCHAR(20) NOT NULL,     -- 'must_complete', 'must_pass', 'any'
    
    CONSTRAINT FK_NodeDependencies_Node FOREIGN KEY (NodeId) REFERENCES FlowNodes(Id),
    CONSTRAINT FK_NodeDependencies_DependsOn FOREIGN KEY (DependsOnNodeId) REFERENCES FlowNodes(Id),
    CONSTRAINT UQ_NodeDependencies UNIQUE (NodeId, DependsOnNodeId)
);

GO

-- ============================================
-- EXTENSION 5: Flow Versioning
-- Tracks configuration versions so users continue with their version
-- ============================================
CREATE TABLE FlowVersions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VersionNumber INT NOT NULL,
    ConfigJson NVARCHAR(MAX) NOT NULL,        -- Complete flow configuration snapshot
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT UQ_FlowVersions_Number UNIQUE (VersionNumber)
);

-- Track which version each user is on
ALTER TABLE UserProgress 
ADD FlowVersionId INT NULL;

-- FK would be: CONSTRAINT FK_UserProgress_FlowVersion FOREIGN KEY (FlowVersionId) REFERENCES FlowVersions(Id)

GO

-- ============================================
-- EXTENSION 6: Notifications/Reminders
-- ============================================
CREATE TABLE NotificationTemplates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TemplateName NVARCHAR(100) NOT NULL,
    Channel NVARCHAR(20) NOT NULL,            -- 'email', 'sms', 'push'
    Subject NVARCHAR(200) NULL,
    BodyTemplate NVARCHAR(MAX) NOT NULL,      -- Supports {{placeholders}}
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE UserNotifications (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(36) NOT NULL,
    TemplateId INT NOT NULL,
    TriggerNodeId INT NULL,
    TriggerReason NVARCHAR(50) NOT NULL,      -- 'deadline_approaching', 'step_available', 'reminder'
    ScheduledAt DATETIME2 NOT NULL,
    SentAt DATETIME2 NULL,
    Status NVARCHAR(20) NOT NULL,             -- 'scheduled', 'sent', 'failed', 'cancelled'
    
    CONSTRAINT FK_UserNotifications_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_UserNotifications_Template FOREIGN KEY (TemplateId) REFERENCES NotificationTemplates(Id),
    INDEX IX_UserNotifications_Scheduled (ScheduledAt, Status)
);

GO

-- ============================================
-- EXTENSION 7: Multi-Tenancy Support
-- For SaaS model serving multiple organizations
-- ============================================
CREATE TABLE Tenants (
    Id NVARCHAR(36) PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Domain NVARCHAR(200) NULL,
    ConfigJson NVARCHAR(MAX) NULL,            -- Tenant-specific settings
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Add TenantId to Users (would need ALTER TABLE)
-- ALTER TABLE Users ADD TenantId NVARCHAR(36) NULL;
-- ALTER TABLE Users ADD CONSTRAINT FK_Users_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id);

GO

PRINT 'Extension tables created successfully.';
