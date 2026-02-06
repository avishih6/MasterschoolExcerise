-- ============================================
-- AdmissionProcess Audit/History Tables
-- MS SQL Server
-- Version: 1.0
-- ============================================

-- ============================================
-- PROGRESS AUDIT LOG TABLE
-- Tracks all progress changes for compliance, debugging, and analytics
-- ============================================
CREATE TABLE ProgressAuditLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(36) NOT NULL,
    NodeId INT NOT NULL,
    NodeName NVARCHAR(200) NULL,              -- Denormalized for easier querying
    Action NVARCHAR(50) NOT NULL,             -- 'completed', 'passed', 'failed', 'recovery_attempted'
    PayloadJson NVARCHAR(MAX) NULL,           -- Raw webhook payload for debugging
    PreviousStatus TINYINT NULL,
    NewStatus TINYINT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    -- No FK to Users - audit should work even if user is deleted
    INDEX IX_ProgressAuditLog_UserId_CreatedAt (UserId, CreatedAt DESC),
    INDEX IX_ProgressAuditLog_NodeId (NodeId),
    INDEX IX_ProgressAuditLog_CreatedAt (CreatedAt DESC)
);

GO

-- ============================================
-- WEBHOOK PROCESSING LOG TABLE
-- For idempotency and retry tracking
-- ============================================
CREATE TABLE WebhookProcessingLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdempotencyKey NVARCHAR(100) NOT NULL,    -- Unique key from webhook (e.g., hash of payload)
    UserId NVARCHAR(36) NOT NULL,
    StepName NVARCHAR(200) NOT NULL,
    PayloadHash NVARCHAR(64) NOT NULL,        -- SHA256 of payload for duplicate detection
    ProcessingStatus NVARCHAR(20) NOT NULL,   -- 'received', 'processing', 'completed', 'failed'
    ErrorMessage NVARCHAR(MAX) NULL,
    ReceivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ProcessedAt DATETIME2 NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    
    CONSTRAINT UQ_WebhookProcessingLog_IdempotencyKey UNIQUE (IdempotencyKey),
    INDEX IX_WebhookProcessingLog_UserId (UserId),
    INDEX IX_WebhookProcessingLog_ReceivedAt (ReceivedAt DESC)
);

GO

-- ============================================
-- FLOW CONFIG CHANGE LOG TABLE
-- Tracks changes to flow configuration for versioning
-- ============================================
CREATE TABLE FlowConfigChangeLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NodeId INT NOT NULL,
    ChangeType NVARCHAR(20) NOT NULL,         -- 'created', 'updated', 'deleted', 'reordered'
    PreviousConfigJson NVARCHAR(MAX) NULL,
    NewConfigJson NVARCHAR(MAX) NULL,
    ChangedBy NVARCHAR(100) NULL,             -- User/system that made the change
    ChangedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_FlowConfigChangeLog_NodeId (NodeId),
    INDEX IX_FlowConfigChangeLog_ChangedAt (ChangedAt DESC)
);

GO

PRINT 'Audit tables created successfully.';
