-- ============================================
-- AdmissionProcess Flow Configuration Seed Data v2
-- MS SQL Server
-- 
-- Matches the current JSON configuration
-- ============================================

-- ============================================
-- INSERT NODES (Pure definitions)
-- ============================================

-- Steps (will be root nodes in hierarchy)
INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (1, 'Personal Details Form', NULL);

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (2, 'IQ Test', NULL);

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (3, 'Interview', NULL);

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (4, 'Sign Contract', NULL);

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (5, 'Payment', NULL);

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (6, 'Join Slack', NULL);

-- Tasks (will have parents in hierarchy)
INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (10, 'Complete personal details', '["first_name","last_name","email","timestamp"]');

INSERT INTO Nodes (Id, Name, 
    BasePassConditionJson, 
    PayloadIdentifiersJson,
    DerivedFactMappingsJson)
VALUES (20, 'Take IQ test', 
    '{"type":"score_threshold","field":"score","threshold":75}',
    '["test_id","score","timestamp"]',
    '[{"sourceField":"score","targetFactName":"iq_score"}]');

INSERT INTO Nodes (Id, Name, 
    BaseVisibilityConditionJson,
    BasePassConditionJson, 
    PayloadIdentifiersJson,
    DerivedFactMappingsJson)
VALUES (21, 'Take second chance IQ test',
    '{"type":"score_range","field":"iq_score","min":60,"max":75}',
    '{"type":"score_threshold","field":"score","threshold":75}',
    '["score","timestamp"]',
    '[{"sourceField":"score","targetFactName":"iq_score"}]');

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (30, 'Schedule interview', '["interview_date"]');

INSERT INTO Nodes (Id, Name, 
    BasePassConditionJson,
    PayloadIdentifiersJson)
VALUES (31, 'Perform interview',
    '{"type":"decision_equals","field":"decision","expectedValue":"passed_interview"}',
    '["interview_date","interviewer_id","decision"]');

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (40, 'Upload identification document', '["passport_number","timestamp"]');

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (41, 'Sign employment contract', '["timestamp"]');

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (50, 'Complete payment', '["payment_id","timestamp"]');

INSERT INTO Nodes (Id, Name, PayloadIdentifiersJson)
VALUES (60, 'Join Slack workspace', '["email","timestamp"]');

GO

-- ============================================
-- INSERT NODE HIERARCHY (Structure and order)
-- ParentNodeId NULL = Step (root node)
-- ParentNodeId NOT NULL = Task (child of that step)
-- ============================================

-- Step 1: Personal Details Form (Order 1, no parent = Step)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (1, NULL, 1);

-- Task 10: Complete personal details (under Step 1)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (10, 1, 1);

-- Step 2: IQ Test (Order 2)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (2, NULL, 2);

-- Task 20: Take IQ test (under Step 2)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (20, 2, 1);

-- Task 21: Second chance IQ test (under Step 2, requires Task 20 failed)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order], RequiresPreviousNodeFailedId)
VALUES (21, 2, 2, 20);

-- Step 3: Interview (Order 3)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (3, NULL, 3);

-- Task 30: Schedule interview (under Step 3)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (30, 3, 1);

-- Task 31: Perform interview (under Step 3)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (31, 3, 2);

-- Step 4: Sign Contract (Order 4)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (4, NULL, 4);

-- Task 40: Upload ID document (under Step 4)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (40, 4, 1);

-- Task 41: Sign contract (under Step 4)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (41, 4, 2);

-- Step 5: Payment (Order 5)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (5, NULL, 5);

-- Task 50: Complete payment (under Step 5)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (50, 5, 1);

-- Step 6: Join Slack (Order 6)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (6, NULL, 6);

-- Task 60: Join Slack workspace (under Step 6)
INSERT INTO NodeHierarchy (NodeId, ParentNodeId, [Order])
VALUES (60, 6, 1);

GO

-- ============================================
-- VERIFY: Show the flow structure
-- ============================================
SELECT 
    n.Id,
    n.Name,
    CASE WHEN nh.ParentNodeId IS NULL THEN 'Step' ELSE 'Task' END AS Role,
    nh.ParentNodeId,
    pn.Name AS ParentName,
    nh.[Order],
    n.BasePassConditionJson,
    n.BaseVisibilityConditionJson,
    nh.RequiresPreviousNodeFailedId
FROM Nodes n
INNER JOIN NodeHierarchy nh ON n.Id = nh.NodeId
LEFT JOIN Nodes pn ON nh.ParentNodeId = pn.Id
ORDER BY 
    CASE WHEN nh.ParentNodeId IS NULL THEN nh.[Order] ELSE 999 END,
    COALESCE(nh.ParentNodeId, n.Id),
    nh.[Order];

GO

PRINT 'Flow configuration seed data v2 inserted successfully.';
