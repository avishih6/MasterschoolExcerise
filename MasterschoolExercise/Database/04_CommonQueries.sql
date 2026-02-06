-- ============================================
-- AdmissionProcess Common Queries
-- MS SQL Server
-- 
-- Useful queries for development, debugging,
-- and analytics
-- ============================================

-- ============================================
-- USER PROGRESS QUERIES
-- ============================================

-- Get user's current progress with step details
-- Used by: GET /api/flow/GetCurrentStepAndTaskForUser
CREATE OR ALTER PROCEDURE sp_GetUserCurrentProgress
    @UserId NVARCHAR(36)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.Id AS UserId,
        u.Email,
        up.CachedOverallStatus,
        CASE up.CachedOverallStatus
            WHEN 0 THEN 'InProgress'
            WHEN 1 THEN 'Accepted'
            WHEN 2 THEN 'Rejected'
        END AS StatusName,
        s.Name AS CurrentStepName,
        s.[Order] AS CurrentStepOrder,
        t.Name AS CurrentTaskName,
        up.CacheUpdatedAt
    FROM Users u
    LEFT JOIN UserProgress up ON u.Id = up.UserId
    LEFT JOIN FlowNodes s ON up.CurrentStepId = s.Id
    LEFT JOIN FlowNodes t ON up.CurrentTaskId = t.Id
    WHERE u.Id = @UserId;
END;
GO

-- Get all step statuses for a user
-- Used by: GET /api/flow/GetEntireFlowForUser
CREATE OR ALTER PROCEDURE sp_GetUserFlowStatuses
    @UserId NVARCHAR(36)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        fn.Id AS NodeId,
        fn.Name,
        fn.Role,
        CASE fn.Role WHEN 1 THEN 'Step' WHEN 2 THEN 'Task' END AS RoleName,
        fn.ParentId,
        fn.[Order],
        COALESCE(uns.Status, 0) AS Status,
        CASE COALESCE(uns.Status, 0)
            WHEN 0 THEN 'NotStarted'
            WHEN 1 THEN 'Accepted'
            WHEN 2 THEN 'Rejected'
        END AS StatusName,
        uns.UpdatedAt
    FROM FlowNodes fn
    LEFT JOIN UserNodeStatuses uns ON fn.Id = uns.NodeId AND uns.UserId = @UserId
    WHERE fn.IsActive = 1
    ORDER BY fn.Role, fn.[Order];
END;
GO

-- Get user's derived facts
CREATE OR ALTER PROCEDURE sp_GetUserDerivedFacts
    @UserId NVARCHAR(36)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        FactName,
        FactValue,
        FactValueType,
        CreatedAt
    FROM UserDerivedFacts
    WHERE UserId = @UserId
    ORDER BY CreatedAt;
END;
GO

-- ============================================
-- ANALYTICS QUERIES
-- ============================================

-- Funnel drop-off analysis
-- Shows how many users reached each step and their pass/fail rates
CREATE OR ALTER VIEW vw_FunnelAnalysis
AS
SELECT 
    fn.Id AS StepId,
    fn.Name AS StepName,
    fn.[Order] AS StepOrder,
    COUNT(DISTINCT uns.UserId) AS UsersReached,
    SUM(CASE WHEN uns.Status = 2 THEN 1 ELSE 0 END) AS Rejected,
    SUM(CASE WHEN uns.Status = 1 THEN 1 ELSE 0 END) AS Passed,
    CAST(SUM(CASE WHEN uns.Status = 1 THEN 1.0 ELSE 0 END) / NULLIF(COUNT(DISTINCT uns.UserId), 0) * 100 AS DECIMAL(5,2)) AS PassRate
FROM FlowNodes fn
LEFT JOIN UserNodeStatuses uns ON fn.Id = uns.NodeId
WHERE fn.Role = 1  -- Steps only
GROUP BY fn.Id, fn.Name, fn.[Order];
GO

-- IQ Test second chance usage
-- How many users needed the second chance and how many passed
CREATE OR ALTER VIEW vw_SecondChanceAnalysis
AS
SELECT 
    'First Attempt' AS TestAttempt,
    COUNT(DISTINCT uns.UserId) AS TotalUsers,
    SUM(CASE WHEN uns.Status = 1 THEN 1 ELSE 0 END) AS Passed,
    SUM(CASE WHEN uns.Status = 2 THEN 1 ELSE 0 END) AS Failed
FROM UserNodeStatuses uns
WHERE uns.NodeId = 20

UNION ALL

SELECT 
    'Second Chance' AS TestAttempt,
    COUNT(DISTINCT uns.UserId) AS TotalUsers,
    SUM(CASE WHEN uns.Status = 1 THEN 1 ELSE 0 END) AS Passed,
    SUM(CASE WHEN uns.Status = 2 THEN 1 ELSE 0 END) AS Failed
FROM UserNodeStatuses uns
WHERE uns.NodeId = 21;
GO

-- Daily admission completions
CREATE OR ALTER VIEW vw_DailyAdmissions
AS
SELECT 
    CAST(up.CacheUpdatedAt AS DATE) AS CompletionDate,
    COUNT(CASE WHEN up.CachedOverallStatus = 1 THEN 1 END) AS Accepted,
    COUNT(CASE WHEN up.CachedOverallStatus = 2 THEN 1 END) AS Rejected,
    COUNT(CASE WHEN up.CachedOverallStatus = 0 THEN 1 END) AS InProgress
FROM UserProgress up
GROUP BY CAST(up.CacheUpdatedAt AS DATE);
GO

-- ============================================
-- OPERATIONAL QUERIES
-- ============================================

-- Find users stuck at a specific step for more than N days
CREATE OR ALTER PROCEDURE sp_GetStuckUsers
    @StepId INT,
    @DaysStuck INT = 7
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.Id AS UserId,
        u.Email,
        fn.Name AS StuckAtStep,
        DATEDIFF(DAY, COALESCE(uns.UpdatedAt, u.CreatedAt), GETUTCDATE()) AS DaysAtStep
    FROM Users u
    JOIN UserProgress up ON u.Id = up.UserId
    JOIN FlowNodes fn ON up.CurrentStepId = fn.Id
    LEFT JOIN UserNodeStatuses uns ON u.Id = uns.UserId AND uns.NodeId = fn.Id
    WHERE up.CurrentStepId = @StepId
      AND up.CachedOverallStatus = 0  -- InProgress
      AND DATEDIFF(DAY, COALESCE(uns.UpdatedAt, u.CreatedAt), GETUTCDATE()) >= @DaysStuck
    ORDER BY DaysAtStep DESC;
END;
GO

-- Check for duplicate webhook processing
CREATE OR ALTER PROCEDURE sp_CheckDuplicateWebhooks
    @HoursBack INT = 24
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        PayloadHash,
        COUNT(*) AS DuplicateCount,
        MIN(ReceivedAt) AS FirstReceived,
        MAX(ReceivedAt) AS LastReceived
    FROM WebhookProcessingLog
    WHERE ReceivedAt >= DATEADD(HOUR, -@HoursBack, GETUTCDATE())
    GROUP BY PayloadHash
    HAVING COUNT(*) > 1
    ORDER BY DuplicateCount DESC;
END;
GO

-- ============================================
-- FLOW CONFIGURATION QUERIES
-- ============================================

-- Get complete flow structure (steps with tasks)
CREATE OR ALTER PROCEDURE sp_GetFlowStructure
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        s.Id AS StepId,
        s.Name AS StepName,
        s.[Order] AS StepOrder,
        t.Id AS TaskId,
        t.Name AS TaskName,
        t.[Order] AS TaskOrder,
        nc_vis.EvaluationType AS VisibilityCondition,
        nc_vis.Field AS VisibilityField,
        nc_pass.EvaluationType AS PassCondition,
        nc_pass.Field AS PassField,
        nc_pass.ThresholdValue,
        nc_pass.ExpectedValue
    FROM FlowNodes s
    LEFT JOIN FlowNodes t ON t.ParentId = s.Id AND t.Role = 2
    LEFT JOIN NodeConditions nc_vis ON t.Id = nc_vis.NodeId AND nc_vis.ConditionType = 'visibility'
    LEFT JOIN NodeConditions nc_pass ON t.Id = nc_pass.NodeId AND nc_pass.ConditionType = 'pass'
    WHERE s.Role = 1 AND s.ParentId IS NULL AND s.IsActive = 1
    ORDER BY s.[Order], t.[Order];
END;
GO

PRINT 'Common queries and procedures created successfully.';
