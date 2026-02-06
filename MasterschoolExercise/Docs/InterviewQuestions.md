# Interview Questions & Answers: AdmissionProcess System

## Table of Contents
1. [Architecture & Design Questions](#architecture--design-questions)
2. [Database Questions](#database-questions)
3. [Scaling Questions](#scaling-questions)
4. [Code Quality Questions](#code-quality-questions)
5. [Extension Questions](#extension-questions)
6. [Tricky Scenario Questions](#tricky-scenario-questions)
7. [Behavioral Questions](#behavioral-questions)

---

## Architecture & Design Questions

### Q1: "Why did you choose a configuration-driven approach for the flow?"

**Strong Answer:**
> "Product managers frequently change admission flows - adding steps, reordering, modifying conditions. A configuration-driven approach has three key benefits:
> 
> 1. **No code deployments** for flow changes - just update JSON config
> 2. **Testability** - can test different configurations without code changes
> 3. **Separation of concerns** - business rules (config) vs execution logic (code)
>
> The trade-off is added complexity in the evaluation engine, but for a flow that changes monthly, this investment pays off quickly."

### Q2: "Walk me through what happens when a webhook arrives for step completion."

**Strong Answer:**
> "The flow is:
> 1. `FlowController.CompleteStepAsync` receives the request with userId, stepName, and payload
> 2. `ProgressLogic.CompleteStepAsync` finds the step by name, gets/creates user progress
> 3. If step has tasks, `DetermineTaskFromPayload` matches payload to the correct task
> 4. `PassEvaluator.EvaluateAsync` checks if the user passed (e.g., score > 75)
> 5. Derived facts are stored (e.g., `iq_score`) for future visibility conditions
> 6. Node status is updated to Accepted/Rejected
> 7. If all tasks complete, step status is updated
> 8. Cache is updated with new current position and overall status
> 9. Progress is persisted"

### Q3: "Explain the 'second chance IQ test' feature and how it works."

**Strong Answer:**
> "It demonstrates conditional visibility. Here's the flow:
>
> 1. User takes Task 20 (IQ Test), scores 65 - fails (threshold is 75)
> 2. Score is stored as derived fact `iq_score = 65`
> 3. Task 21 has visibility condition: `iq_score BETWEEN 60 AND 75`
> 4. Task 21 also has `RequiresPreviousTaskFailedId = 20`
> 5. Both conditions are met, so Task 21 becomes visible
> 6. If user scored < 60, Task 21 stays hidden - they're rejected
> 7. If user scored > 75, Task 21 stays hidden - they passed
>
> This pattern is extensible - we could add a 'third chance' or conditional retakes for any task."

### Q4: "How would you handle a PM request to add a completely new step type?"

**Strong Answer:**
> "The system is designed for this:
>
> 1. Add the new node to `nodes.json` with unique ID
> 2. Add step configuration to `flow-config.json` with tasks
> 3. If new condition type needed (e.g., 'date_before'), add to `ConditionTypes.cs` and handle in `Condition.EvaluatePass`
> 4. If new payload fields needed, add to `PayloadIdentifiers`
>
> No changes needed to controllers, repositories, or core logic. This is the power of configuration-driven design."

---

## Database Questions

### Q5: "Explain your table design decisions."

**Strong Answer:**
> "Key decisions:
>
> **Nodes vs NodeHierarchy separation** - Nodes define WHAT exists (name, base conditions, payload fields). NodeHierarchy defines WHERE it sits (parent, order, condition overrides). This separation enables:
> - Same node definition, different positions per country/university
> - Base conditions with scope-specific overrides
> - Role derived from hierarchy (no parent = Step, has parent = Task)
>
> **JSON for conditions** - Using JSON columns for conditions because:
> - Different condition types have different fields
> - COALESCE(Override, Base) pattern for effective condition
> - Easy to add new condition types without schema changes
>
> **UserNodeStatuses with composite PK** - (UserId, NodeId) is natural key. Clustered index on this supports 'get all statuses for user' efficiently.
>
> **DerivedFacts as key-value** - Flexible for unknown future facts. Alternative was typed columns, but that requires schema changes for new fact types."

### Q6: "Why separate Nodes from NodeHierarchy instead of one table?"

**Strong Answer:**
> "The separation follows the principle of separating DEFINITION from STRUCTURE:
>
> **Single table approach problems:**
> - To support Israel-specific ordering, you'd duplicate all node rows
> - Changing a node name requires updating multiple rows
> - Role (Step/Task) stored redundantly
>
> **Two table approach benefits:**
> - Node definition is single source of truth
> - Hierarchy can vary per scope (country, university) without duplicating definitions
> - Role is DERIVED from hierarchy (ParentNodeId IS NULL = Step)
> - Base conditions on Node, overrides in Hierarchy - clean fallback pattern
>
> **Future extensibility:**
> Just add `ScopeLevel` and `ScopeEntityId` to NodeHierarchy. Same node can appear in multiple hierarchies with different orders and condition overrides."

### Q7: "Why use NVARCHAR(36) for user IDs instead of INT IDENTITY?"

**Strong Answer:**
> "GUIDs (NEWID()) have advantages for distributed systems:
>
> 1. **No central sequence** - can generate IDs without DB round-trip
> 2. **Merge-friendly** - no conflicts when merging sharded data
> 3. **Microservices ready** - User Service can generate IDs independently
>
> Trade-offs:
> - 36 bytes vs 4 bytes (but storage is cheap)
> - Worse for clustered index locality (can use NEWSEQUENTIALID() if needed)
> - Slightly slower joins (but with proper indexing, negligible)
>
> For an admission system expecting millions of users potentially across regions, GUIDs are the safer choice."

### Q7: "How would you optimize the 'get flow for user' query?"

**Strong Answer:**
> "Current approach requires multiple queries. Optimization strategies:
>
> 1. **Denormalization**: Store user's visible task count per step in UserProgress
> 2. **Materialized view**: Pre-compute flow structure with JOIN
> 3. **Caching**: Flow structure rarely changes - cache in Redis for 5 min
>
> The query pattern:
> ```sql
> -- Instead of multiple round-trips
> SELECT s.*, t.*, c.*, uns.Status
> FROM FlowNodes s
> LEFT JOIN FlowNodes t ON t.ParentId = s.Id
> LEFT JOIN NodeConditions c ON c.NodeId = t.Id
> LEFT JOIN UserNodeStatuses uns ON uns.NodeId = t.Id AND uns.UserId = @UserId
> WHERE s.Role = 1 AND s.ParentId IS NULL
> ORDER BY s.[Order], t.[Order]
> ```
> Then process in memory to evaluate visibility conditions."

---

## Scaling Questions

### Q8: "How would you handle 1 million concurrent users?"

**Strong Answer:**
> "Layer by layer:
>
> **Database**:
> - Read replicas for GET endpoints (90% of traffic)
> - Partition UserNodeStatuses by UserId hash
> - Connection pooling (50 connections × 10 app servers)
>
> **Application**:
> - Horizontal scaling behind load balancer
> - Stateless design (already have this)
> - Auto-scaling based on CPU/request latency
>
> **Caching**:
> - Redis cluster for flow config (99% cache hit expected)
> - User progress cache with 30s TTL
>
> **Async processing**:
> - Webhook queue to handle bursts
> - Worker pool scales independently
>
> With this setup, 1M concurrent is achievable with ~20 app servers, 5 Redis nodes, and 1 primary + 3 read replicas."

### Q9: "What if webhooks arrive faster than you can process them?"

**Strong Answer:**
> "This is exactly why we need async processing:
>
> 1. **Immediate acknowledgment** - return 202 Accepted after enqueuing
> 2. **Message queue** - RabbitMQ/Kafka absorbs the burst
> 3. **Backpressure** - workers process at sustainable rate
> 4. **Auto-scaling** - scale worker pool based on queue depth
> 5. **Monitoring** - alert if queue depth > threshold
>
> Key metrics to watch:
> - Queue depth (should stabilize)
> - Processing time per message
> - Error rate in workers
>
> If sustained high throughput, might need to shard the progress database."

### Q10: "How do you ensure consistency when caching user progress?"

**Strong Answer:**
> "Cache consistency strategy:
>
> 1. **Write-through**: On step completion, update DB then invalidate cache
> 2. **Short TTL**: 30 seconds max for user-specific data
> 3. **Pub/sub invalidation**: Progress Service publishes invalidation events
>
> For eventual consistency scenarios:
> - User might see stale status for up to 30 seconds
> - Critical operations (step completion) always hit DB
> - Status check endpoint can have 'skip_cache' parameter for accuracy
>
> Trade-off: Slightly stale reads vs database load. For admission flow, 30s staleness is acceptable."

---

## Code Quality Questions

### Q11: "What design patterns did you use and why?"

**Strong Answer:**
> "Several patterns:
>
> **Repository Pattern** (`IFlowRepository`, `MockFlowRepository`)
> - Abstracts data access
> - Enables easy mocking for tests
> - Allows swapping implementations (Mock → SQL → Cosmos)
>
> **Strategy Pattern** (Condition evaluation)
> - Different evaluation strategies per condition type
> - New types added without modifying existing code
>
> **Result Pattern** (`LogicResult<T>`)
> - Explicit success/failure handling
> - Carries error details without exceptions
> - Clean API responses
>
> **Dependency Injection** (throughout)
> - Loose coupling between layers
> - Testability
> - Runtime flexibility"

### Q12: "How would you make this code more testable?"

**Strong Answer:**
> "Current state is already quite testable due to:
>
> 1. **Interface-based dependencies** - can mock everything
> 2. **Pure functions where possible** - `EvaluatePassCondition` takes payload, returns bool
> 3. **Small, focused classes** - single responsibility
>
> Improvements I'd make:
> - Extract `IDateTimeProvider` instead of `DateTime.UtcNow` for time-dependent tests
> - Add `IConditionEvaluatorFactory` for testing different condition types in isolation
> - Consider property-based testing for condition evaluation edge cases"

### Q13: "How do you handle errors in your application?"

**Strong Answer:**
> "Multi-layer approach:
>
> **Business Logic** - `LogicResult` pattern:
> ```csharp
> return LogicResult.Failure("Step not found", 404);
> ```
>
> **Controllers** - translate to HTTP:
> ```csharp
> if (!result.IsSuccess)
>     return StatusCode(result.HttpStatusCode ?? 500, new ErrorResponse { Error = result.ErrorMessage });
> ```
>
> **Global** - exception middleware for unhandled:
> - Log full exception
> - Return sanitized error to client
> - Different detail levels for dev/prod
>
> **Webhooks** - async processing:
> - Retry failed messages
> - Dead letter queue for persistent failures
> - Alert on DLQ growth"

---

## Extension Questions

### Q14: "How would you add email notifications when users complete steps?"

**Strong Answer:**
> "Event-driven approach:
>
> 1. **Publish event** after step completion:
> ```csharp
> await _eventBus.PublishAsync(new StepCompletedEvent(userId, stepName, status));
> ```
>
> 2. **Notification Service** subscribes:
> - Checks notification preferences
> - Renders email template with user/step data
> - Queues for sending
>
> 3. **Email worker** processes queue:
> - Rate limiting
> - Retry logic
> - Delivery tracking
>
> Database changes:
> - `NotificationTemplates` table
> - `UserNotificationPreferences` table
> - `NotificationLog` for tracking"

### Q15: "How would you implement A/B testing for different flows?"

**Strong Answer:**
> "Experiment framework:
>
> 1. **Experiment definition**:
> ```json
> {
>   \"name\": \"simplified_flow_v1\",
>   \"variants\": [
>     { \"name\": \"control\", \"weight\": 50, \"flow\": \"default\" },
>     { \"name\": \"treatment\", \"weight\": 50, \"flow\": \"simplified\" }
>   ]
> }
> ```
>
> 2. **Assignment on user creation**:
> - Hash userId + experimentId for deterministic assignment
> - Store in `UserExperimentAssignments`
>
> 3. **Flow retrieval**:
> - Check user's variant
> - Return appropriate flow config
>
> 4. **Analytics**:
> - Track conversion per variant
> - Statistical significance calculation
> - Auto-shutoff if treatment performs worse"

### Q16: "How would you support multiple organizations (multi-tenancy)?"

**Strong Answer:**
> "Two approaches:
>
> **Shared database** (simpler):
> - Add TenantId to all tables
> - Row-level security in SQL Server
> - Filter all queries by tenant
>
> **Database per tenant** (more isolated):
> - Each tenant gets own database
> - Connection string routing based on tenant
> - Better for compliance/data isolation
>
> For this system, I'd start with shared database:
> - Admission flows are similar across tenants
> - Easier to manage
> - Can migrate high-value tenants to dedicated later
>
> Implementation:
> - Tenant identified from JWT/subdomain
> - Tenant context middleware sets tenant ID
> - Repository base class adds tenant filter automatically"

---

## Tricky Scenario Questions

### Q17: "What happens if a user tries to complete step 3 but hasn't finished step 2?"

**Strong Answer:**
> "Current implementation doesn't explicitly prevent this - it's a design decision:
>
> **Option 1: Allow (current)**
> - External systems might complete out of order
> - Trust the webhook source
> - Flow calculation handles incomplete dependencies
>
> **Option 2: Strict ordering**
> - Add validation in `CompleteStepAsync`:
> ```csharp
> var previousStep = await GetPreviousStepAsync(stepNode);
> if (previousStep != null && !IsStepComplete(previousStep, progress))
>     return LogicResult.Failure(\"Previous step not complete\", 400);
> ```
>
> **Option 3: Flexible dependencies**
> - `NodeDependencies` table defines what must complete first
> - Some steps might be parallel
>
> I'd discuss with PM which behavior is expected."

### Q18: "How do you handle the scenario where someone calls CompleteStep twice for the same step?"

**Strong Answer:**
> "Idempotency is crucial:
>
> **Current behavior**:
> - If node status already exists, it gets overwritten
> - Could lead to incorrect state if second call has different payload
>
> **Improved handling**:
> ```csharp
> if (progress.NodeStatuses.TryGetValue(taskId, out var existing))
> {
>     if (existing.Status == ProgressStatus.Accepted)
>         return LogicResult.Success(); // Already completed successfully
>     // Only allow retry if previously failed
> }
> ```
>
> **With database**:
> - Store idempotency key (hash of userId + stepName + timestamp)
> - Check before processing
> - Return previous result if duplicate"

### Q19: "What if the flow configuration changes while a user is mid-flow?"

**Strong Answer:**
> "Critical question for production. Options:
>
> **Option 1: Continue with old config (recommended)**
> - Store flow version when user starts
> - User completes with their assigned version
> - New users get new version
>
> **Option 2: Force upgrade**
> - Map old progress to new flow
> - Complex if steps are removed/reordered
> - Risk of losing progress
>
> **Option 3: Soft transitions**
> - New steps are optional until user restarts
> - Removed steps are skipped
> - Changed conditions only apply to new attempts
>
> Implementation:
> - `FlowVersions` table with complete config snapshots
> - `UserProgress.FlowVersionId` tracks user's version
> - Flow retrieval respects user's version"

---

## Behavioral Questions

### Q20: "Tell me about a technical decision you made that you'd change in hindsight."

**Suggested Answer:**
> "The in-memory storage was fine for demonstrating the architecture, but I should have included a database implementation from the start. The repository pattern made swapping easy, but having a working SQL implementation would have demonstrated:
> 
> - Transaction handling for step completion
> - Optimistic concurrency with RowVersion
> - Query optimization with indexes
>
> In a real project, I'd start with the simplest working database implementation, even if we plan to scale later."

### Q21: "How would you prioritize features if you had one more week?"

**Suggested Answer:**
> "Priority order:
>
> 1. **SQL Server implementation** (2 days) - Critical for production
> 2. **Webhook idempotency** (1 day) - Prevents duplicate processing
> 3. **Basic caching for flow** (1 day) - Easy win for performance
> 4. **Integration tests** (1 day) - Confidence in system behavior
>
> What I'd skip:
> - A/B testing (nice to have)
> - Admin UI (can use SQL directly)
> - Advanced analytics (can add later)
>
> The goal is production-ready, not feature-complete."

---

## Quick Reference Card

### Key Numbers to Remember
- 6 steps, ~15 total nodes
- O(steps × tasks) for current position calculation
- 75 = IQ test pass threshold
- 60-75 = second chance range

### Key Classes to Know
- `ProgressLogic.CompleteStepAsync` - main business logic
- `Condition.EvaluateVisibility/EvaluatePass` - rule evaluation
- `FlowNode.IsVisibleForUser` - visibility check
- `MockFlowRepository.InitializeAsync` - config loading

### Key Config Files
- `nodes.json` - node definitions (ID, name, order)
- `flow-config.json` - flow structure (parent-child, conditions)

### Key Patterns Used
- Repository, Strategy, Result, DI

### Indexes You Should Know
- `IX_Users_Email` - email lookup
- `IX_NodeHierarchy_ParentNodeId` - get tasks under a step
- `IX_NodeHierarchy_RootSteps` - get root steps (filtered index)
- `IX_UserNodeStatuses_UserId` - user's progress

### DB Schema Key Point
- **Nodes** = definition (what), **NodeHierarchy** = structure (where)
- Role derived: `ParentNodeId IS NULL` = Step, otherwise Task
- Conditions: `COALESCE(Override, Base)` pattern
