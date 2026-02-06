# Interview Preparation: AdmissionProcess System

## Table of Contents
1. [System Overview](#system-overview)
2. [Database Design Decisions](#database-design-decisions)
3. [Scaling Strategies](#scaling-strategies)
4. [Potential Interview Questions](#potential-interview-questions)
5. [Extension Ideas](#extension-ideas)
6. [Code Design Highlights](#code-design-highlights)

---

## System Overview

### What This System Does
A flexible admission flow management system that:
- Tracks users through a configurable multi-step admission process
- Supports conditional tasks (e.g., second-chance IQ test only visible to users who scored 60-75)
- Evaluates pass/fail conditions from webhook payloads
- Provides APIs for frontend to display progress and submit completions

### Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **Configuration-driven flow** | PMs can modify flows without code deployments |
| **Adjacency list for steps/tasks** | Simple parent-child relationship, easy to query |
| **Derived facts pattern** | Enables runtime visibility conditions without complex joins |
| **Cached overall status** | Avoids expensive recalculation on every status check |
| **Repository pattern with interfaces** | Easy to swap mock/real implementations |

---

## Database Design Decisions

### Core Design: Separation of Definition vs Structure

```
Nodes                    → Pure definition (what the node IS)
NodeHierarchy            → Structure (where it sits, parent-child, order)
Users                    → Core user entity
UserProgress             → Cached current position and status
UserNodeStatuses         → Per-node completion tracking
UserDerivedFacts         → Stored facts for visibility evaluation
```

### Why Separate Nodes from NodeHierarchy?

| Aspect | Benefit |
|--------|---------|
| **Same node, different positions** | Country A can have different order than Country B |
| **Role derived from hierarchy** | ParentNodeId IS NULL = Step, otherwise Task |
| **Base + Override conditions** | Base conditions on Node, overrides in Hierarchy |
| **Future-proof** | Add ScopeLevel/ScopeEntityId columns for country/university flows |

### Index Strategy

| Index | Purpose | Query Pattern |
|-------|---------|---------------|
| `IX_Users_Email` | Email lookups, duplicate check | `WHERE Email = @email` |
| `IX_NodeHierarchy_ParentNodeId` | Get tasks under a step | `WHERE ParentNodeId = @stepId` |
| `IX_NodeHierarchy_RootSteps` | Get root steps | `WHERE ParentNodeId IS NULL` |
| `IX_UserNodeStatuses_UserId` | Get all statuses for user | `WHERE UserId = @userId` |

### Why NVARCHAR(36) for IDs?
- GUIDs provide natural uniqueness across distributed systems
- No central ID generation bottleneck
- Safe for future microservices split

### Why JSON for Conditions?
- Flexible schema for different condition types
- Base conditions on Nodes, overrides in NodeHierarchy
- Easy to extend without schema migrations
- COALESCE(Override, Base) pattern for effective condition

---

## Scaling Strategies

### Database Scaling

#### Scenario 1: 10M Users
```
Problem: UserNodeStatuses grows to 10M * 15 nodes = 150M rows

Solutions:
1. Partition by UserId hash (16-64 partitions)
2. Archive completed users to cold storage
3. Read replicas for GET endpoints
```

#### Scenario 2: High Webhook Throughput
```
Problem: 1000 webhooks/second during peak

Solutions:
1. Message queue (RabbitMQ/Kafka) for async processing
2. Return 202 Accepted immediately
3. Horizontal scaling of workers
4. Idempotency keys prevent duplicates
```

### Application Scaling (Microservices)

```
┌─────────────────┐
│   API Gateway   │
└────────┬────────┘
         │
    ┌────┼────┬────────────┐
    ▼    ▼    ▼            ▼
┌──────┐┌──────┐┌────────┐┌─────────┐
│ User ││ Flow ││Progress││ Webhook │
│ Svc  ││ Svc  ││  Svc   ││Processor│
└──┬───┘└──┬───┘└───┬────┘└────┬────┘
   │       │        │          │
   ▼       ▼        ▼          ▼
┌──────┐┌──────┐┌────────┐┌─────────┐
│Users ││Redis ││Progress││ Message │
│  DB  ││Cache ││   DB   ││  Queue  │
└──────┘└──────┘└────────┘└─────────┘
```

**Service Boundaries:**
- **User Service**: Auth, user CRUD
- **Flow Service**: Configuration (cacheable, rarely changes)
- **Progress Service**: High write throughput, user progress
- **Webhook Processor**: Async, horizontally scalable

---

## Potential Interview Questions

### Architecture Questions

**Q: Why configuration-driven instead of code?**
> "PMs change flows frequently. With JSON config, we can deploy changes without code releases. The config is validated on load with fallback to hardcoded defaults if corrupted."

**Q: How does the visibility condition work for second-chance IQ test?**
> "When Task 20 (IQ test) completes, we store the score as a derived fact `iq_score`. Task 21 has a visibility condition checking if `iq_score` is between 60-75. The `RequiresPreviousTaskFailedId` ensures Task 21 only appears after Task 20 failed."

**Q: How do you handle concurrent requests for same user?**
> "Current in-memory implementation uses ConcurrentDictionary. For SQL, we'd add a RowVersion column for optimistic concurrency, or use `UPDLOCK` for pessimistic locking on critical sections."

**Q: What if flow config changes while users are mid-flow?**
> "Version the configuration. When users start, they're assigned a version. They continue with that version even if we deploy new configs. Stored in `FlowVersions` table."

### Performance Questions

**Q: Why cache overall status?**
> "Calculating status requires traversing all steps, checking all visible tasks, evaluating recovery paths. Caching avoids this O(steps × tasks) calculation on every status check. Cache is invalidated/updated when progress changes."

**Q: What's the complexity of GetCurrentStep?**
> "O(steps × tasks) in worst case. Could optimize by storing CurrentStepId/CurrentTaskId in UserProgress, updated on each progress change. We already do this."

**Q: How would you make flow retrieval faster?**
> "Flow structure rarely changes. Cache entire flow in Redis with long TTL. Invalidate only on config changes. Could even use memory cache since it's shared across all users."

### Resilience Questions

**Q: What if webhook arrives twice?**
> "Check if NodeStatus already exists before processing. Store idempotency key from webhook in `WebhookProcessingLog`. Return success without reprocessing if duplicate detected."

**Q: What if DB write fails after evaluation?**
> "Wrap evaluation and persistence in transaction. If commit fails, retry with exponential backoff. For async webhooks, use outbox pattern: write to pending table, background job commits to main tables."

**Q: What if flow config file is corrupted?**
> "We have fallback configuration in code (`GetFallbackNodeDefinitions`, `GetFallbackStepConfigurations`). Log error, use fallback, alert operations team."

### Design Questions

**Q: How would you add country-specific flows?**
> "Add `FlowOverrides` table with (OverrideLevel, OverrideKey, NodeId, IsDisabled, NewOrder). When building flow for user, check for overrides based on user's country/institution. Apply overrides to base flow."

**Q: How would you add step deadlines?**
> "Add `NodeDeadlines` table with DeadlineHours per node. `UserDeadlineTracking` tracks when each user started a step. Background job checks for expired deadlines, triggers rejection or notification."

**Q: How would you allow parallel steps?**
> "Add `NodeDependencies` table to express 'this step requires these other steps completed first'. Steps with satisfied dependencies are all available. Current order-based logic becomes dependency-based."

---

## Extension Ideas

### High-Priority Extensions

| Extension | Implementation Effort | Business Value |
|-----------|----------------------|----------------|
| Webhook retry with idempotency | Low | High - reliability |
| Step deadlines | Medium | High - conversion |
| Email notifications | Medium | High - engagement |
| Analytics dashboard | Medium | High - insights |

### Medium-Priority Extensions

| Extension | Implementation Effort | Business Value |
|-----------|----------------------|----------------|
| A/B testing flows | High | Medium - optimization |
| Multi-tenancy | High | High - SaaS model |
| Country-specific flows | Medium | Medium - localization |
| Admin UI for config | High | Medium - operations |

### API Extension Ideas

```
POST /api/admin/flow/reload     → Hot-reload configuration
GET  /api/analytics/funnel      → Funnel drop-off analysis
POST /api/users/{id}/extend     → Grant deadline extension
GET  /api/flow/preview?config=  → Preview flow with temp config
```

---

## Code Design Highlights

### Clean Architecture
```
API Layer (Controllers)
    ↓
Business Logic (IFlowLogic, IProgressLogic, etc.)
    ↓
Data Access (IFlowRepository, IProgressRepository, etc.)
    ↓
Models (User, FlowNode, UserProgress, etc.)
```

### Key Design Patterns

| Pattern | Where Used | Benefit |
|---------|------------|---------|
| Repository | `IFlowRepository`, `MockFlowRepository` | Swap implementations |
| Strategy | `Condition.EvaluateVisibility/EvaluatePass` | Extensible evaluation |
| Result | `LogicResult<T>` | Clean error handling |
| Configuration | JSON files | Declarative flow definition |

### SOLID Principles Applied

- **S**: `UserLogic`, `StatusLogic`, `ProgressLogic` each have single responsibility
- **O**: New condition types added without modifying existing code
- **L**: Mock repositories substitutable for real ones
- **I**: Small, focused interfaces (`IUserLogic`, `IStatusLogic`)
- **D**: Business logic depends on abstractions (`IFlowRepository`)

### Testability

```csharp
// Easy to test with mocks
var mockFlowRepo = new MockFlowRepository(logger);
var mockProgressRepo = new MockProgressRepository();
var logic = new ProgressLogic(mockFlowRepo, mockProgressRepo, evaluator, logger);

// Test specific scenarios
await logic.CompleteStepAsync("user1", "IQ Test", new Dictionary<string, object> { ["score"] = 80 });
```

---

## Quick Reference: API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/users/CreateUser` | Create new user |
| GET | `/api/flow/GetEntireFlowForUser?userId=` | Get flow with progress |
| GET | `/api/flow/GetCurrentStepAndTaskForUser?userId=` | Get current position |
| PUT | `/api/flow/CompleteStep` | Mark step completed |
| GET | `/api/users/{userId}/GetUserStatus` | Get acceptance status |

---

## Common SQL Queries for Interview

```sql
-- User's current position
SELECT u.Email, s.Name AS CurrentStep, t.Name AS CurrentTask
FROM Users u
JOIN UserProgress up ON u.Id = up.UserId
LEFT JOIN FlowNodes s ON up.CurrentStepId = s.Id
LEFT JOIN FlowNodes t ON up.CurrentTaskId = t.Id
WHERE u.Id = @UserId;

-- Funnel analysis
SELECT fn.Name, COUNT(DISTINCT uns.UserId) AS Reached,
       SUM(CASE WHEN uns.Status = 1 THEN 1 ELSE 0 END) AS Passed
FROM FlowNodes fn
LEFT JOIN UserNodeStatuses uns ON fn.Id = uns.NodeId
WHERE fn.Role = 1
GROUP BY fn.Id, fn.Name, fn.[Order]
ORDER BY fn.[Order];
```

---

## Final Tips for Tomorrow

1. **Lead with architecture**: Explain the configuration-driven approach first
2. **Mention trade-offs**: Every decision has pros and cons - show you considered them
3. **Be ready for "what if"**: Scale, failure, extension questions will come
4. **Code walkthrough**: Be ready to explain `ProgressLogic.CompleteStepAsync` flow
5. **Don't over-engineer**: Start simple, explain how you'd extend when asked
