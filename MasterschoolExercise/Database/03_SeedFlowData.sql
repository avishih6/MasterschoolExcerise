-- ============================================
-- AdmissionProcess Flow Configuration Seed Data
-- MS SQL Server
-- Version: 1.0
-- 
-- This script populates the flow configuration
-- matching the current JSON configuration
-- ============================================

-- ============================================
-- INSERT FLOW NODES (Steps and Tasks)
-- ============================================

-- Step 1: Personal Details Form
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (1, 'Personal Details Form', 1, NULL, 1, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (10, 'Complete personal details', 2, 1, 1, 1);

-- Step 2: IQ Test
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (2, 'IQ Test', 1, NULL, 2, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (20, 'Take IQ test', 2, 2, 1, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (21, 'Take second chance IQ test', 2, 2, 2, 1);

-- Step 3: Interview
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (3, 'Interview', 1, NULL, 3, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (30, 'Schedule interview', 2, 3, 1, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (31, 'Perform interview', 2, 3, 2, 1);

-- Step 4: Sign Contract
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (4, 'Sign Contract', 1, NULL, 4, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (40, 'Upload identification document', 2, 4, 1, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (41, 'Sign employment contract', 2, 4, 2, 1);

-- Step 5: Payment
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (5, 'Payment', 1, NULL, 5, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (50, 'Complete payment', 2, 5, 1, 1);

-- Step 6: Join Slack
INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (6, 'Join Slack', 1, NULL, 6, 1);

INSERT INTO FlowNodes (Id, Name, Role, ParentId, [Order], IsActive)
VALUES (60, 'Join Slack workspace', 2, 6, 1, 1);

GO

-- ============================================
-- INSERT NODE CONDITIONS
-- ============================================

-- Task 20 (IQ Test): Pass if score > 75
INSERT INTO NodeConditions (NodeId, ConditionType, EvaluationType, Field, ThresholdValue)
VALUES (20, 'pass', 'score_threshold', 'score', 75);

-- Task 21 (Second Chance IQ Test): 
-- Visibility: Only if iq_score between 60-75
-- Pass: if score > 75
-- Requires: Task 20 failed
INSERT INTO NodeConditions (NodeId, ConditionType, EvaluationType, Field, MinValue, MaxValue)
VALUES (21, 'visibility', 'score_range', 'iq_score', 60, 75);

INSERT INTO NodeConditions (NodeId, ConditionType, EvaluationType, Field, ThresholdValue, RequiresPreviousTaskFailedId)
VALUES (21, 'pass', 'score_threshold', 'score', 75, 20);

-- Task 31 (Perform Interview): Pass if decision = 'passed_interview'
INSERT INTO NodeConditions (NodeId, ConditionType, EvaluationType, Field, ExpectedValue)
VALUES (31, 'pass', 'decision_equals', 'decision', 'passed_interview');

GO

-- ============================================
-- INSERT PAYLOAD IDENTIFIERS
-- ============================================

-- Task 10: Personal Details
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (10, 'first_name');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (10, 'last_name');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (10, 'email');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (10, 'timestamp');

-- Task 20: IQ Test
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (20, 'test_id');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (20, 'score');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (20, 'timestamp');

-- Task 21: Second Chance IQ Test
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (21, 'score');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (21, 'timestamp');

-- Task 30: Schedule Interview
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (30, 'interview_date');

-- Task 31: Perform Interview
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (31, 'interview_date');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (31, 'interviewer_id');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (31, 'decision');

-- Task 40: Upload ID Document
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (40, 'passport_number');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (40, 'timestamp');

-- Task 41: Sign Contract
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (41, 'timestamp');

-- Task 50: Payment
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (50, 'payment_id');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (50, 'timestamp');

-- Task 60: Join Slack
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (60, 'email');
INSERT INTO NodePayloadIdentifiers (NodeId, IdentifierName) VALUES (60, 'timestamp');

GO

-- ============================================
-- INSERT DERIVED FACT MAPPINGS
-- ============================================

-- Task 20: Store score as iq_score
INSERT INTO DerivedFactMappings (NodeId, SourceField, TargetFactName)
VALUES (20, 'score', 'iq_score');

-- Task 21: Store score as iq_score (overwrite on second chance)
INSERT INTO DerivedFactMappings (NodeId, SourceField, TargetFactName)
VALUES (21, 'score', 'iq_score');

GO

PRINT 'Flow configuration seed data inserted successfully.';
