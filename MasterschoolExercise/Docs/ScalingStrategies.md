# Scaling Strategies: AdmissionProcess System

## Table of Contents
1. [Current Architecture Analysis](#current-architecture-analysis)
2. [Database Scaling](#database-scaling)
3. [Application Scaling](#application-scaling)
4. [Microservices Architecture](#microservices-architecture)
5. [Caching Strategies](#caching-strategies)
6. [Async Processing](#async-processing)
7. [Monitoring and Observability](#monitoring-and-observability)

---

## Current Architecture Analysis

### What We Have Now

```
┌─────────────────────────────────────────────────────┐
│                   Single Process                     │
│  ┌─────────┐  ┌────────┐  ┌───────────────────────┐ │
│  │   API   │─▶│   BL   │─▶│   In-Memory Repos     │ │
│  │ Layer   │  │ Layer  │  │ (ConcurrentDictionary)│ │
│  └─────────┘  └────────┘  └───────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### Current Limitations

| Limitation | Impact | Priority to Fix |
|------------|--------|-----------------|
| In-memory storage | Data lost on restart | Critical |
| Single process | No horizontal scaling | High |
| Synchronous webhooks | Potential timeouts | High |
| No caching layer | Repeated DB queries | Medium |
| No rate limiting | Vulnerable to abuse | Medium |

---

## Database Scaling

### Phase 1: Basic Production Setup

```
┌─────────────┐     ┌─────────────┐
│   App       │────▶│  SQL Server │
│  Servers    │     │   Primary   │
└─────────────┘     └──────┬──────┘
                           │ Replication
                    ┌──────▼──────┐
                    │  Read       │
                    │  Replica    │
                    └─────────────┘
```

**Implementation:**
- Primary DB for writes
- Read replica(s) for GET endpoints
- Connection string routing in code

### Phase 2: Partitioning (10M+ Users)

```sql
-- Partition UserNodeStatuses by UserId hash
CREATE PARTITION FUNCTION pf_UserIdHash (INT)
AS RANGE RIGHT FOR VALUES (0, 16, 32, 48, 64, 80, 96, 112);

CREATE PARTITION SCHEME ps_UserIdHash
AS PARTITION pf_UserIdHash ALL TO ([PRIMARY]);

-- Partition key: CHECKSUM(UserId) % 128
```

**When to Partition:**
- UserNodeStatuses > 100M rows
- Query performance degradation observed
- Write contention on indexes

### Phase 3: Sharding (100M+ Users)

```
┌──────────────────────────────────────────────────┐
│                  Shard Router                     │
│         shard_key = hash(UserId) % 4            │
└────────┬──────────┬──────────┬──────────┬───────┘
         │          │          │          │
    ┌────▼───┐ ┌────▼───┐ ┌────▼───┐ ┌────▼───┐
    │Shard 0 │ │Shard 1 │ │Shard 2 │ │Shard 3 │
    │Users   │ │Users   │ │Users   │ │Users   │
    │A-F     │ │G-L     │ │M-R     │ │S-Z     │
    └────────┘ └────────┘ └────────┘ └────────┘
```

**Sharding Strategy:**
- Shard by UserId (consistent hashing)
- Each shard holds complete user data (users + progress + facts)
- Cross-shard queries needed for analytics only

---

## Application Scaling

### Horizontal Scaling (Current Architecture)

```
                    ┌─────────────┐
                    │   Load      │
                    │  Balancer   │
                    └──────┬──────┘
           ┌───────────────┼───────────────┐
           │               │               │
      ┌────▼───┐      ┌────▼───┐      ┌────▼───┐
      │ App 1  │      │ App 2  │      │ App 3  │
      └────┬───┘      └────┬───┘      └────┬───┘
           │               │               │
           └───────────────┼───────────────┘
                    ┌──────▼──────┐
                    │    SQL      │
                    │   Server    │
                    └─────────────┘
```

**Requirements for Horizontal Scaling:**
1. ✅ Stateless application (no in-memory session state)
2. ⚠️ Need to move to database storage
3. ⚠️ Need distributed caching for flow config
4. ✅ Repository pattern allows easy swap

### Auto-Scaling Triggers

| Metric | Scale Up | Scale Down |
|--------|----------|------------|
| CPU | > 70% for 5 min | < 30% for 10 min |
| Memory | > 80% for 5 min | < 40% for 10 min |
| Request latency | p95 > 500ms | p95 < 100ms |
| Queue depth | > 1000 messages | < 100 messages |

---

## Microservices Architecture

### Service Decomposition

```
┌────────────────────────────────────────────────────────────────┐
│                        API Gateway                              │
│              (Authentication, Rate Limiting, Routing)           │
└────────────┬──────────────┬──────────────┬─────────────────────┘
             │              │              │
      ┌──────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
      │    User     │ │   Flow    │ │  Progress   │
      │   Service   │ │  Service  │ │   Service   │
      └──────┬──────┘ └─────┬─────┘ └──────┬──────┘
             │              │              │
      ┌──────▼──────┐ ┌─────▼─────┐ ┌──────▼──────┐
      │   Users     │ │   Redis   │ │  Progress   │
      │     DB      │ │   Cache   │ │     DB      │
      └─────────────┘ └─────┬─────┘ └─────────────┘
                            │
                     ┌──────▼──────┐
                     │  Flow DB    │
                     │ (read-only) │
                     └─────────────┘
```

### Service Responsibilities

| Service | Responsibilities | Data Owned |
|---------|-----------------|------------|
| **User Service** | User CRUD, authentication | Users table |
| **Flow Service** | Flow configuration, caching | FlowNodes, Conditions |
| **Progress Service** | Step completion, status | UserProgress, NodeStatuses |
| **Webhook Service** | Async webhook processing | WebhookProcessingLog |
| **Notification Service** | Email/SMS/Push | Notification templates, queue |
| **Analytics Service** | Funnel analysis, reporting | Aggregated metrics |

### Service Communication

```
Synchronous (REST/gRPC):
  - User Service ←→ Progress Service (user validation)
  - Progress Service ←→ Flow Service (flow structure)

Asynchronous (Message Queue):
  - Webhook Service → Progress Service (step completion)
  - Progress Service → Notification Service (status changes)
  - Progress Service → Analytics Service (events)
```

### API Gateway Configuration

```yaml
routes:
  - path: /api/users/**
    service: user-service
    rate_limit: 100/min
    
  - path: /api/flow/**
    service: flow-service
    cache_ttl: 300s  # Flow rarely changes
    
  - path: /api/progress/**
    service: progress-service
    rate_limit: 500/min
    
  - path: /webhook/**
    service: webhook-service
    timeout: 5s
    retry: 3
```

---

## Caching Strategies

### Cache Layers

```
┌─────────────────────────────────────────────────────┐
│                   Request Flow                       │
└───────────────────────┬─────────────────────────────┘
                        │
                 ┌──────▼──────┐
                 │   L1 Cache   │  In-process memory
                 │ (IMemoryCache)│  TTL: 60s
                 └──────┬──────┘
                        │ miss
                 ┌──────▼──────┐
                 │   L2 Cache   │  Distributed (Redis)
                 │    (Redis)   │  TTL: 5min
                 └──────┬──────┘
                        │ miss
                 ┌──────▼──────┐
                 │   Database   │
                 └─────────────┘
```

### What to Cache

| Data | Cache Location | TTL | Invalidation |
|------|---------------|-----|--------------|
| Flow structure | Redis + Memory | 5 min | On config change |
| User progress | Redis | 30 sec | On step complete |
| User status | Redis | 30 sec | On step complete |
| Node conditions | Memory | 5 min | On config change |

### Cache Implementation

```csharp
public class CachedFlowRepository : IFlowRepository
{
    private readonly IFlowRepository _inner;
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache _memoryCache;
    
    public async Task<List<FlowNode>> GetRootStepsAsync()
    {
        const string key = "flow:root_steps";
        
        // L1: Memory cache
        if (_memoryCache.TryGetValue(key, out List<FlowNode> steps))
            return steps;
        
        // L2: Redis
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
        {
            steps = JsonSerializer.Deserialize<List<FlowNode>>(cached);
            _memoryCache.Set(key, steps, TimeSpan.FromSeconds(60));
            return steps;
        }
        
        // Database
        steps = await _inner.GetRootStepsAsync();
        
        // Populate both caches
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(steps), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        _memoryCache.Set(key, steps, TimeSpan.FromSeconds(60));
        
        return steps;
    }
}
```

### Cache Invalidation

```csharp
public class CacheInvalidator
{
    public async Task InvalidateFlowCacheAsync()
    {
        // Redis pub/sub for distributed invalidation
        await _redis.PublishAsync("cache:invalidate", "flow:*");
    }
    
    public async Task InvalidateUserCacheAsync(string userId)
    {
        await _cache.RemoveAsync($"user:{userId}:progress");
        await _cache.RemoveAsync($"user:{userId}:status");
    }
}
```

---

## Async Processing

### Webhook Processing Flow

```
┌──────────┐    ┌───────────┐    ┌─────────┐    ┌────────┐
│ External │───▶│  Webhook  │───▶│ Message │───▶│ Worker │
│  System  │    │  Endpoint │    │  Queue  │    │  Pool  │
└──────────┘    └───────────┘    └─────────┘    └───┬────┘
                     │                              │
                     │ 202 Accepted                 ▼
                     │                         ┌────────┐
                     └─────────────────────────│Database│
                                               └────────┘
```

### Message Queue Implementation

```csharp
// Webhook endpoint - fast response
[HttpPost("step-complete")]
public async Task<IActionResult> HandleWebhook([FromBody] WebhookPayload payload)
{
    // Validate idempotency key
    var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault() 
                         ?? ComputeHash(payload);
    
    // Check for duplicate
    if (await _webhookLog.ExistsAsync(idempotencyKey))
        return Ok(new { status = "already_processed" });
    
    // Enqueue for async processing
    await _messageQueue.PublishAsync("webhooks.step-complete", new
    {
        IdempotencyKey = idempotencyKey,
        Payload = payload,
        ReceivedAt = DateTime.UtcNow
    });
    
    return Accepted(new { status = "processing" });
}

// Worker - processes queue
public class WebhookWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var message in _queue.ConsumeAsync("webhooks.step-complete", ct))
        {
            try
            {
                await ProcessWebhookAsync(message);
                await message.AckAsync();
            }
            catch (Exception ex)
            {
                await message.NackAsync(requeue: message.RetryCount < 3);
            }
        }
    }
}
```

### Retry Strategy

| Attempt | Delay | Action on Failure |
|---------|-------|-------------------|
| 1 | Immediate | Retry |
| 2 | 5 seconds | Retry |
| 3 | 30 seconds | Retry |
| 4 | 5 minutes | Dead letter queue |

---

## Monitoring and Observability

### Key Metrics to Track

```
Application Metrics:
├── request_duration_seconds (histogram)
├── request_total (counter, by endpoint, status)
├── active_connections (gauge)
└── error_rate (counter)

Business Metrics:
├── users_created_total
├── steps_completed_total (by step)
├── funnel_conversion_rate (gauge)
└── admission_decisions_total (by outcome)

Infrastructure Metrics:
├── db_connection_pool_size
├── cache_hit_rate
├── queue_depth
└── worker_processing_rate
```

### Distributed Tracing

```csharp
// Trace webhook through entire flow
public async Task<LogicResult> CompleteStepAsync(string userId, string stepName, Dictionary<string, object> payload)
{
    using var activity = _activitySource.StartActivity("CompleteStep");
    activity?.SetTag("user.id", userId);
    activity?.SetTag("step.name", stepName);
    
    try
    {
        // ... business logic
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddRedis(redisConnection, name: "cache")
    .AddRabbitMQ(rabbitConnection, name: "messagequeue")
    .AddCheck<FlowConfigHealthCheck>("flow-config");

// Custom health check
public class FlowConfigHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        var nodes = await _flowRepo.GetAllNodesAsync();
        if (nodes.Count == 0)
            return HealthCheckResult.Unhealthy("No flow nodes loaded");
        
        return HealthCheckResult.Healthy($"{nodes.Count} nodes loaded");
    }
}
```

### Alerting Rules

| Alert | Condition | Severity |
|-------|-----------|----------|
| High Error Rate | error_rate > 1% for 5 min | Critical |
| Slow Responses | p99 latency > 2s for 5 min | Warning |
| Queue Backlog | queue_depth > 10000 | Warning |
| DB Connection Exhaustion | pool_available < 5 | Critical |
| Cache Miss Spike | cache_hit_rate < 50% | Warning |

---

## Migration Path

### Phase 1: Database Migration (Week 1-2)
1. Create SQL Server database with schema
2. Implement SqlFlowRepository, SqlProgressRepository
3. Data migration tool for existing users
4. Feature flag to switch repositories

### Phase 2: Caching Layer (Week 3)
1. Deploy Redis cluster
2. Implement CachedFlowRepository
3. Add cache invalidation on config changes

### Phase 3: Async Webhooks (Week 4)
1. Deploy RabbitMQ/Azure Service Bus
2. Implement webhook queue producer
3. Deploy worker pool
4. Add idempotency tracking

### Phase 4: Microservices (Month 2-3)
1. Extract User Service
2. Extract Flow Service with dedicated cache
3. Extract Progress Service
4. Deploy API Gateway
5. Implement service mesh

---

## Interview Discussion Points

**Q: "How would you handle a spike of 10,000 webhooks in 1 minute?"**
> "Queue-based processing. Webhook endpoint validates and enqueues immediately, returning 202 Accepted. Worker pool scales horizontally. With 10 workers processing 50 messages/second each, we clear the spike in ~3 minutes with no data loss."

**Q: "What if Redis goes down?"**
> "Circuit breaker pattern. If Redis is unavailable, fall back to database directly. Flow config is also cached in-memory with longer TTL as L1 cache. Alerts fire, but system remains functional with degraded performance."

**Q: "How do you ensure consistency in microservices?"**
> "Eventual consistency with saga pattern for distributed transactions. Progress Service is source of truth for user progress. Other services subscribe to events. Compensating transactions for rollback scenarios."
