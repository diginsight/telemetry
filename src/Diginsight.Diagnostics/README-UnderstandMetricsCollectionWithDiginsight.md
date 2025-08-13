# Understanding Metrics Collection with Diginsight

This document provides a comprehensive explanation of how metrics recording works in Diginsight, particularly focusing on span duration metrics and the underlying architecture.

## 📑 Table of Contents

- [Overview](#overview)
- [The Three Pillars of Observability](#the-three-pillars-of-observability)
  - [Understanding Logs, Traces, and Metrics](#understanding-logs-traces-and-metrics)
    - [1. **Logs** - Discrete Event Records](#1-logs---discrete-event-records)
    - [2. **Traces** - Request Journey Tracking](#2-traces---request-journey-tracking)
    - [3. **Metrics** - Quantitative Measurements](#3-metrics---quantitative-measurements)
  - [.NET Core vs. OpenTelemetry Implementation](#net-core-vs-opentelemetry-implementation)
    - [Native .NET Core Approach](#native-net-core-approach)
    - [Unified Approach with OpenTelemetry](#unified-approach-with-opentelemetry)
    - [What AddOpenTelemetry() Does vs What You Still Need](#what-addopentelemetry-does-vs-what-you-still-need)
    - [Recommended Approach: Hybrid Configuration](#recommended-approach-hybrid-configuration)
    - [Key Takeaway](#key-takeaway)
    - [Integration Benefits](#integration-benefits)
    - [Comparison Summary](#comparison-summary)
    - [Best Practice: Hybrid Approach](#best-practice-hybrid-approach)
  - [Types of Metrics in Diginsight](#types-of-metrics-in-diginsight)
    - [1. **span_duration Metrics** (Diginsight)](#1-span_duration-metrics-diginsight)
    - [2. **request_size/response_size Metrics** (Diginsight.Components)](#2-request_sizeresponse_size-metrics-diginsightcomponents)
    - [3. **query_cost Metric** (Diginsight.Components)](#3-query_cost-metric-diginsightcomponents)
  - [Why Are These Metrics Essential?](#why-are-these-metrics-essential)
  - [Practical Examples](#practical-examples)
    - [E-commerce Application Metrics](#e-commerce-application-metrics)
    - [Database Performance Monitoring](#database-performance-monitoring)
- [How Metrics Are Gathered ?](#how-metrics-are-gathered-)
  - [The Collection Pipeline](#the-collection-pipeline)
  - [Step-by-Step Collection Process](#step-by-step-collection-process)
    - [1. **Activity Creation and Execution**](#1-activity-creation-and-execution)
    - [2. **Diginsight Activity Listener Registration**](#2-diginsight-activity-listener-registration)
    - [3. **Automatic Metric Recording**](#3-automatic-metric-recording)
    - [4. **OpenTelemetry Processing and Export**](#4-opentelemetry-processing-and-export)
  - [Real-World Example: E-commerce Order Processing](#real-world-example-e-commerce-order-processing)
    - [Application Code](#application-code)
    - [Metrics Generated](#metrics-generated)
    - [Configuration for Collection](#configuration-for-collection)
  - [Benefits of This Integration](#benefits-of-this-integration)
  - [Monitoring Dashboard Example](#monitoring-dashboard-example)
  - [Detailed Flow Explanation](#detailed-flow-explanation)
- [Key Components](#key-components)
  - [Core Classes](#core-classes)
  - [Interface Hierarchy](#interface-hierarchy)
  - [Metrics Infrastructure](#metrics-infrastructure)
- [Registration Process](#registration-process)
  - [Service Registration](#service-registration)
  - [Startup Registration](#startup-registration)
  - [Configuration Binding](#configuration-binding)
- [Filtering and Configuration](#filtering-and-configuration)
  - [Multi-Level Filtering](#multi-level-filtering)
    - [1. Global Control](#1-global-control)
    - [2. ActivitySource Level](#2-activitysource-level)
    - [3. Activity Name Level](#3-activity-name-level)
  - [Configuration Examples](#configuration-examples)
- [Metrics Output](#metrics-output)
  - [OpenTelemetry Integration](#opentelemetry-integration)
  - [Application Insights Output](#application-insights-output)
  - [Prometheus Output](#prometheus-output)
- [Performance Considerations](#performance-considerations)
  - [Optimization Strategies](#optimization-strategies)
  - [Memory and CPU Impact](#memory-and-cpu-impact)
- [Advanced Configuration](#advanced-configuration)
  - [Custom Metric Names](#custom-metric-names)
  - [Tag Customization](#tag-customization)
  - [Conditional Recording](#conditional-recording)
- [Troubleshooting](#troubleshooting)
  - [Common Issues](#common-issues)
    - [1. No Metrics Appearing](#1-no-metrics-appearing)
    - [2. Missing Activity Sources](#2-missing-activity-sources)
    - [3. Performance Issues](#3-performance-issues)
  - [Debugging Tools](#debugging-tools)
    - [Enable Detailed Logging](#enable-detailed-logging)
    - [Custom Diagnostics](#custom-diagnostics)
- [Integration with OpenTelemetry](#integration-with-opentelemetry)
  - [Complete Setup Example](#complete-setup-example)
  - [Correlation with Tracing](#correlation-with-tracing)
  - [Best Practices](#best-practices)

## Overview

Diginsight seamlessly **extends standard .NET Activities** to provide automatic observability for applications.
In particular, it **captures various types of metrics** during application execution, providing **quantitative insights** into system behavior and **performance**.

## The Three Pillars of Observability

Modern application observability is built on **three fundamental data types** that work together to provide comprehensive insights into system behavior. 

We'll see that all can be written with standard .NET code.
**Diginsight** implements small extensions to **gather automatically information about the application flow**.
**OpenTelemetry** implements small startup sequence configuration to **allow sending telemetry to remote listerners by means of standard protocols**.

### Understanding Logs, Traces, and Metrics

#### 1. **Logs** - Discrete Event Records

**What are Logs?**
Logs are **time-stamped records of discrete events** that occur within an application. 
They capture what happened, when it happened, and contextual information about the event.

**Characteristics:**
- **Structured or unstructured text** containing event details
- **Point-in-time snapshots** of application state
- **Variable cardinality** - can contain any number of fields
- **Human-readable** and searchable

**.NET Core Logs:**
with .NET Core's built-in logging framework logs are normally written using the **`ILogger`** methods: **LogInformation**, **LogDebug**, **LogError**, etc.
```csharp
// Microsoft.Extensions.Logging
public class OrderService
{
    private readonly ILogger<OrderService> logger;
    
    public async Task ProcessOrderAsync(int orderId)
    {
        logger.LogInformation("Starting order processing for {OrderId}", orderId);
        
        try
        {
            // Business logic
            logger.LogDebug("Inventory validated for order {OrderId}", orderId);
            logger.LogInformation("Order {OrderId} processed successfully", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order {OrderId}", orderId);
            throw;
        }
    }
}
```

**OpenTelemetry Logs:**
With OpenTelemetry, if Exporters are configured during the startup sequence, then .NET Core logging will be sent to registered OpenTelemetry listeners.

```csharp
// OpenTelemetry Logging API - Configured during startup
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging
            .AddConsoleExporter()
            .AddOtlpExporter();
    });

// Usage in service classes
public class OrderService
{
    private readonly ILogger<OrderService> logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        this.logger = logger;
    }
    
    public async Task ProcessOrderAsync(int orderId, int customerId)
    {
        logger.LogInformation("Processing order {OrderId} for customer {CustomerId}", 
            orderId, customerId);
    }
}
```


**Use Cases:**
- **Debugging**: Finding the root cause of specific errors
- **Auditing**: Recording business events and compliance data
- **Analysis**: Understanding application behavior patterns

#### 2. **Traces** - Request Journey Tracking

**What are Traces?**
Traces track the **journey of a request** as it flows through various services and components. They show the complete call graph and timing relationships between operations.

**Characteristics:**
- **Hierarchical structure** with parent-child relationships
- **Distributed context** across service boundaries
- **Timing information** for each operation (span)
- **Causal relationships** between operations

**.NET Core Activities (Distributed Tracing):**
With .NET Core, distributed tracing is implemented using the **`Activity`** class, which represents a single operation in a trace.

**Activities can be nested**, and **attributes can be set on them** as shown below, to provide rich context about the operation.

```csharp
// System.Diagnostics.Activity
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("OrderService");
    
    public async Task<Order> ProcessOrderAsync(int orderId)
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId.ToString());
        activity?.SetTag("service.name", "OrderService");
        
        try
        {
            // Child activity for sub-operation
            using var childActivity = ActivitySource.StartActivity("ValidateInventory");
            childActivity?.SetTag("operation", "inventory_check");
            await ValidateInventoryAsync(orderId);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return order;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

**OpenTelemetry Tracing:**
With OpenTelemetry, traces are collected using the **traceProvider.GetTracer()** API.
Every operation in the application can be instrumented by means of a **span** instance obtained by **tracer.StartActiveSpan()**, as shown below.

> **OpenTelemetry span is an alternative to .NET Core's Activity**.
If OpenTelemetry is enabled, **Diginsight manages automatically the span creation** during the activities lifecycle.

```csharp
// OpenTelemetry Tracing API
using var tracerProvider = TracerProviderBuilder.Create()
    .AddSource("OrderService")
    .AddJaegerExporter()
    .Build();

using var tracer = tracerProvider.GetTracer("OrderService");

// Create a span (part of a trace)
using var span = tracer.StartActiveSpan("ProcessOrder");
span.SetAttribute("order.id", orderId);
span.SetAttribute("customer.id", customerId);

try
{
    // Child span for sub-operation
    using var childSpan = tracer.StartActiveSpan("ValidateInventory");
    await ValidateInventoryAsync(orderId);
    
    span.SetStatus(SpanStatusCode.Ok);
}
catch (Exception ex)
{
    span.SetStatus(SpanStatusCode.Error, ex.Message);
    span.RecordException(ex);
    throw;
}
```



**Trace Structure Example:**
```
Trace ID: 1234567890abcdef
└── Span: HTTP POST /api/orders (200ms)
    ├── Span: ProcessOrder (180ms)
    │   ├── Span: ValidateInventory (45ms)
    │   ├── Span: ProcessPayment (120ms)
    │   └── Span: CreateShipment (15ms)
    └── Span: HTTP Response (20ms)
```

When spans are nested, they form a **parent-child hierarchy** **within the same trace**. 
Each span maintains its own attributes, but they share trace context and can inherit certain properties.

**Same Tracer vs Different Tracers:**

```csharp
public class OrderService
{
    // Same tracer instance across methods
    private static readonly ActivitySource OrderTracer = new("OrderService");
    private static readonly ActivitySource PaymentTracer = new("PaymentService");  // Different tracer
    
    public async Task ProcessOrderAsync(int orderId)
    {
        // Parent span from OrderService tracer
        using var parentSpan = OrderTracer.StartActivity("ProcessOrder");
        parentSpan?.SetTag("order.id", orderId.ToString());
        parentSpan?.SetTag("customer.tier", "premium");
        parentSpan?.SetTag("region", "us-east");
        
        // Child span from SAME tracer
        using var inventorySpan = OrderTracer.StartActivity("ValidateInventory");
        inventorySpan?.SetTag("inventory.warehouse", "warehouse-1");
        // inventorySpan does NOT automatically inherit parent attributes
        // You must explicitly access parent or set attributes again
        
        // Child span from DIFFERENT tracer
        using var paymentSpan = PaymentTracer.StartActivity("ProcessPayment");
        paymentSpan?.SetTag("payment.method", "credit_card");
        // paymentSpan is still a child in the same trace, but from different tracer
        
        await ProcessPaymentLogic();
    }
}
```

**Key Principles:**

1. **Span Isolation**: Each span has its **own independent attributes** - child spans do NOT automatically inherit parent attributes
2. **Trace Context Sharing**: All spans in the hierarchy share the same **TraceId** and propagate **TraceContext**
3. **Tracer Independence**: Whether spans use the same tracer or different tracers doesn't affect attribute inheritance
4. **Manual Propagation**: If you need parent attributes in child spans, you must pass them explicitly

**Attribute Accessibility Patterns:**

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("OrderService");
    
    public async Task ProcessOrderAsync(int orderId, string customerTier)
    {
        using var parentActivity = ActivitySource.StartActivity("ProcessOrder");
        parentActivity?.SetTag("order.id", orderId.ToString());
        parentActivity?.SetTag("customer.tier", customerTier);
        parentActivity?.SetTag("service.version", "1.2.3");
        
        // Pattern 1: Access parent attributes (read-only)
        var parentOrderId = parentActivity?.GetTagItem("order.id")?.ToString();
        var parentCustomerTier = parentActivity?.GetTagItem("customer.tier")?.ToString();
        
        // Pattern 2: Pass context explicitly to child operations
        await ValidateInventoryAsync(orderId, customerTier, parentActivity);
        await ProcessPaymentAsync(orderId, customerTier, parentActivity);
    }
    
    private async Task ValidateInventoryAsync(int orderId, string customerTier, Activity? parentActivity)
    {
        using var childActivity = ActivitySource.StartActivity("ValidateInventory");
        
        // Child span attributes are independent
        childActivity?.SetTag("inventory.warehouse", "warehouse-1");
        childActivity?.SetTag("inventory.method", "realtime");
        
        // Must explicitly set parent context if needed
        childActivity?.SetTag("order.id", orderId.ToString());           // Explicit
        childActivity?.SetTag("customer.tier", customerTier);           // Explicit
        
        // Or reference parent attributes
        var serviceVersion = parentActivity?.GetTagItem("service.version")?.ToString();
        childActivity?.SetTag("service.version", serviceVersion);
        
        // Baggage: Cross-cutting concerns that propagate automatically
        Baggage.SetBaggage("correlation.id", Guid.NewGuid().ToString());
        Baggage.SetBaggage("user.session", "session-12345");
        
        await InventoryLogic();
    }
    
    private async Task ProcessPaymentAsync(int orderId, string customerTier, Activity? parentActivity)
    {
        // Different tracer, but still forms parent-child relationship
        using var paymentActivity = PaymentTracer.StartActivity("ProcessPayment");
        
        // Child attributes are independent regardless of tracer
        paymentActivity?.SetTag("payment.processor", "stripe");
        paymentActivity?.SetTag("payment.method", "credit_card");
        
        // Must explicitly propagate needed context
        paymentActivity?.SetTag("order.id", orderId.ToString());
        paymentActivity?.SetTag("customer.tier", customerTier);
        
        // Baggage is automatically available across all spans in trace
        var correlationId = Baggage.GetBaggage("correlation.id");
        var userSession = Baggage.GetBaggage("user.session");
        
        await PaymentLogic();
    }
}
```

**Advanced Context Propagation with Diginsight:**

```csharp
public class OrderService
{
    private static readonly ActivitySource ActivitySource = new("OrderService");
    
    public async Task ProcessOrderAsync(int orderId)
    {
        // Diginsight automatically captures input parameters for LOGGING (not as tags)
        using var activity = ActivitySource.StartMethodActivity(new 
        { 
            orderId, 
            customerTier = "premium", 
            region = "us-east",
            requestId = Guid.NewGuid()
        });
        
        // If you need tags for metrics/tracing, set them explicitly
        activity?.SetTag("order.id", orderId.ToString());
        activity?.SetTag("customer.tier", "premium");
        activity?.SetTag("region", "us-east");
        activity?.SetTag("request.id", Guid.NewGuid().ToString());
        
        try
        {
            // All child operations can access current activity context
            var result = await ProcessOrderSteps(orderId);
            
            // Diginsight automatically captures outputs for LOGGING
            activity?.SetOutput(result);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
    
    private async Task ProcessOrderSteps(int orderId)
    {
        using var childActivity = ActivitySource.StartMethodActivity(new { orderId, step = "validation" });
        
        // Access parent activity through the child's Parent property
        var parentFromChild = childActivity?.Parent;
        var parentOrderId = parentFromChild?.GetTagItem("order.id")?.ToString();
        var parentCustomerTier = parentFromChild?.GetTagItem("customer.tier")?.ToString();
        
        // If useful, you can copy parent tags to child activity for metrics/tracing
        if (parentFromChild != null)
        {
            // Copy essential tags from parent to child
            childActivity?.SetTag("order.id", parentFromChild.GetTagItem("order.id")?.ToString());
            childActivity?.SetTag("customer.tier", parentFromChild.GetTagItem("customer.tier")?.ToString());
            childActivity?.SetTag("region", parentFromChild.GetTagItem("region")?.ToString());
        }
        
        await InventoryValidationLogic();
    }
}
```


**Baggage vs Span Attributes:**

| Aspect | Span Attributes | Baggage |
|--------|----------------|---------|
| **Scope** | Local to individual span | Propagates across entire trace |
| **Inheritance** | Not inherited by child spans | Automatically available in all spans |
| **Performance** | Low overhead | Higher overhead (transmitted across services) |
| **Use Case** | Span-specific metadata | Cross-cutting concerns (user ID, correlation ID) |
| **Cardinality** | High cardinality allowed | Should be low cardinality |

**Best Practices for Nested Spans:**

1. **Explicit Context Passing**: Don't rely on automatic attribute inheritance
2. **Minimal Baggage**: Use baggage sparingly for truly cross-cutting data
3. **Consistent Naming**: Use consistent attribute names across parent and child spans
4. **Activity.Current**: Leverage `Activity.Current` to access the active span context
5. **Diginsight Helpers**: Use Diginsight extensions for automatic parameter capture

**Real-World Example:**

```csharp
public class ECommerceOrderProcessor
{
    private static readonly ActivitySource OrderSource = new("ECommerce.Orders");
    private static readonly ActivitySource PaymentSource = new("ECommerce.Payments");
    private static readonly ActivitySource InventorySource = new("ECommerce.Inventory");
    
    public async Task<Order> ProcessOrderAsync(CreateOrderRequest request)
    {
        // Set cross-cutting baggage that all child spans can access
        Baggage.SetBaggage("user.id", request.UserId.ToString());
        Baggage.SetBaggage("correlation.id", request.CorrelationId);
        Baggage.SetBaggage("tenant.id", request.TenantId);
        
        using var orderActivity = OrderSource.StartRichActivity("ProcessOrder", new
        {
            orderId = request.OrderId,
            customerId = request.CustomerId,
            totalAmount = request.TotalAmount,
            itemCount = request.Items.Count
        });
        
        try
        {
            // Each service creates its own spans but can access baggage
            var inventoryResult = await ValidateInventoryAsync(request.Items);
            var paymentResult = await ProcessPaymentAsync(request.Payment);
            var shipmentResult = await CreateShipmentAsync(request.Shipping);
            
            var order = new Order { /* ... */ };
            orderActivity?.SetOutput(new { order.Id, order.Status });
            return order;
        }
        catch (Exception ex)
        {
            orderActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
    
    private async Task<InventoryResult> ValidateInventoryAsync(List<OrderItem> items)
    {
        // Different tracer, but baggage is automatically available
        using var activity = InventorySource.StartActivity("ValidateInventory");
        
        // Baggage automatically propagated
        var userId = Baggage.GetBaggage("user.id");
        var correlationId = Baggage.GetBaggage("correlation.id");
        var tenantId = Baggage.GetBaggage("tenant.id");
        
        // Span-specific attributes
        activity?.SetTag("inventory.items.count", items.Count);
        activity?.SetTag("inventory.warehouse", "primary");
        activity?.SetTag("user.id", userId);  // Explicitly set from baggage
        
        return await InventoryService.ValidateAsync(items);
    }
    
    private async Task<PaymentResult> ProcessPaymentAsync(PaymentInfo payment)
    {
        // Yet another tracer, still part of same trace
        using var activity = PaymentSource.StartActivity("ProcessPayment");
        
        // Access baggage for cross-cutting concerns
        var userId = Baggage.GetBaggage("user.id");
        var correlationId = Baggage.GetBaggage("correlation.id");
        
        // Payment-specific attributes
        activity?.SetTag("payment.method", payment.Method);
        activity?.SetTag("payment.processor", "stripe");
        activity?.SetTag("user.id", userId);
        
        return await PaymentService.ProcessAsync(payment);
    }
}
```

This comprehensive approach ensures proper context propagation while maintaining span independence and performance.

**Use Cases:**
- **Performance Analysis**: Identifying slow operations in request flows
- **Dependency Mapping**: Understanding service interactions
- **Root Cause Analysis**: Tracing errors across service boundaries

#### 3. **Metrics** - Quantitative Measurements

**What are Metrics?**
Metrics are **numerical measurements** such as **latencies** **costs** or **event counts** that collected over time that provide aggregated insights into system behavior and performance.

**Characteristics:**
- **Time-series data** with timestamps
- **Aggregatable** (sum, average, percentiles)
- **Low cardinality** - limited number of dimensions
- **Efficient storage** and querying


**.NET Core Metrics:**
.NET Core provides built-in support for metrics through the **`System.Diagnostics.Metrics`** namespace, which allows you to define and record various types of metrics.

```csharp
// System.Diagnostics.Metrics
public class OrderService
{
    private static readonly Meter Meter = new("OrderService");
    private static readonly Counter<long> OrderCounter = Meter.CreateCounter<long>("orders_processed");
    private static readonly Histogram<double> OrderDuration = Meter.CreateHistogram<double>("order_duration_ms");
    
    public async Task ProcessOrderAsync(int orderId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await ProcessOrderLogic(orderId);
            
            // Record metrics
            OrderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
            OrderDuration.Record(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            OrderCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
    }
}
```

**OpenTelemetry Metrics:**
when using `.AddPrometheusExporter()`, OpenTelemetry exports metrics across different platforms and languages.

```csharp
// OpenTelemetry Metrics API
using var meterProvider = MeterProviderBuilder.Create()
    .AddMeter("OrderService")
    .AddPrometheusExporter()
    .Build();

var meter = new Meter("OrderService");

// Counter - monotonically increasing value
var orderCounter = meter.CreateCounter<long>("orders_processed_total");

// Histogram - distribution of values
var orderDuration = meter.CreateHistogram<double>("order_processing_duration");

// Gauge - current value
var activeOrders = meter.CreateObservableGauge("active_orders_count", 
    () => GetActiveOrderCount());

// Record metrics
public async Task ProcessOrderAsync(int orderId)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        await ProcessOrderLogic(orderId);
        
        // Record successful processing
        orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
        orderDuration.Record(stopwatch.ElapsedMilliseconds, 
            new KeyValuePair<string, object?>("operation", "process_order"));
    }
    catch (Exception)
    {
        orderCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
        throw;
    }
}
```

**Metric Types:**
- **Counter**: Monotonically increasing values (requests processed, errors occurred)
- **Gauge**: Current state values (active connections, memory usage)
- **Histogram**: Distribution of values (request durations, costs, payload sizes)

**Use Cases:**
- **Monitoring**: Real-time dashboards and health indicators
- **Alerting**: Automated notifications when thresholds are exceeded
- **Capacity Planning**: Understanding resource utilization trends

### .NET Core vs. OpenTelemetry Implementation

#### Native .NET Core Approach

**.NET Core** provides built-in observability primitives that are **OpenTelemetry-compatible**:

```csharp
// .NET Core native approach - Manual registration
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Logging (Microsoft.Extensions.Logging)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddApplicationInsights();
        });
        
        // Metrics (System.Diagnostics.Metrics)
        services.AddMetrics();
        
        // Activity/Tracing (System.Diagnostics.Activity)
        // Automatically integrated with OpenTelemetry
    }
}
```

#### Unified Approach with OpenTelemetry

**OpenTelemetry** provides a **vendor-neutral, standardized approach** to observability that works across different languages and platforms:

```csharp
// Unified OpenTelemetry setup - INCLUDES underlying services automatically
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MyApp.*")
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter()           // Export traces to Jaeger
            .AddZipkinExporter();          // Export traces to Zipkin
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("MyApp.*")
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter()       // Export metrics to Prometheus
            .AddApplicationInsightsExporter(); // Export to Azure
    })
    .WithLogging(logging =>
    {
        logging
            .AddConsoleExporter()
            .AddOtlpExporter();           // Export logs via OTLP protocol
    });

// ❓ DO YOU STILL NEED .NET Core services when using AddOpenTelemetry()?
```

#### **What AddOpenTelemetry() Does vs What You Still Need**

**✅ What AddOpenTelemetry() INCLUDES automatically:**
- **Metrics**: `services.AddMetrics()` is called internally
- **Logging Bridge**: Creates bridge to Microsoft.Extensions.Logging
- **Activity Integration**: Automatic integration with System.Diagnostics.Activity

**❌ What AddOpenTelemetry() does NOT include:**
- **Base Logging Infrastructure**: You still need `services.AddLogging()` for ILogger<T> injection
- **Console/File Logging**: Native .NET logging providers for local development
- **Application Insights Logging**: Direct Application Insights logging (separate from telemetry export)

#### **Recommended Approach: Hybrid Configuration**

```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ STILL NEEDED: Base logging infrastructure for ILogger<T> injection
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();              // Local development console output
    logging.AddDebug();                // Debug output
    logging.AddApplicationInsights();  // Direct App Insights logging (if needed)
});

// ✅ OpenTelemetry: Handles observability export and standardization
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MyApp.*")
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddApplicationInsightsExporter();  // Traces to App Insights
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("MyApp.*")
            .AddRuntimeInstrumentation()
            .AddApplicationInsightsExporter();  // Metrics to App Insights
    })
    .WithLogging(logging =>
    {
        logging
            .AddApplicationInsightsExporter();  // Logs to App Insights via OTEL
    });

// ❌ NOT NEEDED: services.AddMetrics() - included in AddOpenTelemetry()
```

If you want **only** OpenTelemetry-exported observability:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Minimal setup - OpenTelemetry handles everything
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("MyApp.*").AddApplicationInsightsExporter())
    .WithMetrics(metrics => metrics.AddMeter("MyApp.*").AddApplicationInsightsExporter())
    .WithLogging(logging => logging.AddApplicationInsightsExporter());

```

You'll lose local console logging during development
ILogger<T> injection still works, but logs only go to OpenTelemetry exporters.


### Types of Metrics in Diginsight

Diginsight automatically collects several types of metrics from the application execution flow.

> **Diginsight telemetry** implements method activities with `StartMethodActivity`, `StartRichActivity`.
**span_duration** metric can be gathered for every method according to the startup sequence and application configuration.

> **Diginsight components** implements implement Observable extensions for Database access and HTTP client, which automatically gather metrics such as **payload_size**, **query_cost** and **span_duration** for those operations.


#### 1. **span_duration Metrics** (Diginsight)
Measure the execution time of operations (activities/spans) in your application.

**Example**: An API endpoint that processes orders
```csharp
using var activity = ActivitySource.StartRichActivity("ProcessOrder", new { orderId = 123, customerId = 456 });
// ... business logic takes 150ms
// Automatically records: diginsight.span_duration = 150ms with tags like span_name="ProcessOrder"
```

**Real-world scenarios**:
- Database query execution times
- HTTP request processing durations  
- Business operation completion times
- External service call latencies

#### 2. **request_size/response_size Metrics** (Diginsight.Components)
Track the payload sizes of incoming and outgoing requests.

**Example**: HTTP API responses
```csharp
// Automatically captured when using Diginsight with HTTP instrumentation
// Metrics: request_size=1024 bytes, response_size=4096 bytes
```

#### 3. **query_cost Metric** (Diginsight.Components)
Application-specific measurements using Diginsight's timer utilities.

**Example**: Processing throughput
```csharp
var timer = meter.CreateTimer("order_processing_duration");
using var lap = timer.StartLap(new { customer_tier = "premium", region = "us-east" });
// ... process order
// Records: order_processing_duration with customer_tier and region dimensions
```

### Why Are These Metrics Essential?

Span duration metrics measure the execution time of operations (activities/spans) in your application. These metrics are essential for:

- **Performance Monitoring**: Track how long operations take across different components
- **Bottleneck Identification**: Find slow operations causing user experience issues
- **Trend Analysis**: Monitor performance changes over time and detect regressions
- **SLA Monitoring**: Ensure operations meet performance requirements and service agreements
- **Capacity Planning**: Understand resource utilization patterns for scaling decisions

### Practical Examples

#### E-commerce Application Metrics
```csharp
// Order processing pipeline
using var orderActivity = ActivitySource.StartRichActivity("ProcessOrder", 
    new { customer_id = customerId, order_value = orderTotal });

// Each step automatically generates metrics:
using var inventoryCheck = ActivitySource.StartRichActivity("CheckInventory");
// Metric: diginsight.span_duration with span_name="CheckInventory"

using var paymentProcess = ActivitySource.StartRichActivity("ProcessPayment");  
// Metric: diginsight.span_duration with span_name="ProcessPayment"

using var shipmentCreate = ActivitySource.StartRichActivity("CreateShipment");
// Metric: diginsight.span_duration with span_name="CreateShipment"
```

**Resulting metrics in monitoring system**:
- `diginsight.span_duration{span_name="ProcessOrder"}` - Overall order processing time
- `diginsight.span_duration{span_name="CheckInventory"}` - Inventory validation time  
- `diginsight.span_duration{span_name="ProcessPayment"}` - Payment processing time
- `diginsight.span_duration{span_name="CreateShipment"}` - Shipment creation time

#### Database Performance Monitoring
```csharp
public async Task<Customer> GetCustomerAsync(int customerId)
{
    using var activity = ActivitySource.StartMethodActivity(new { customerId, database = "CustomerDB" });
    
    // Database query execution
    var customer = await dbContext.Customers
        .Where(c => c.Id == customerId)
        .FirstOrDefaultAsync();
    
    activity?.SetOutput(new { customer_found = customer != null, records_returned = customer != null ? 1 : 0 });
    return customer;
    
    // Automatically generates:
    // diginsight.span_duration{span_name="GetCustomerAsync", customer_id="123", database="CustomerDB"}
}
```

## How Metrics Are Gathered ?

### The Collection Pipeline

Diginsight integrates deeply with .NET's Activity system and OpenTelemetry to create a seamless metrics collection pipeline:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐    ┌──────────────────┐
│ .NET Activities │    │ Diginsight       │    │ OpenTelemetry       │    │ Monitoring       │
│ (Your Code)     │───▶│ Metric Recorders │───▶│ Metrics Pipeline    │───▶│ Backends         │
│                 │    │                  │    │                     │    │                  │
│ StartActivity() │    │ ActivityListener │    │ Meter → Exporter    │    │ • App Insights   │
│ using { ... }   │    │ → Record()       │    │ → Aggregation       │    │ • Prometheus     │
│ activity.Stop() │    │ → Tags           │    │ → Export            │    │ • Grafana        │
└─────────────────┘    └──────────────────┘    └─────────────────────┘    └──────────────────┘
```

### Step-by-Step Collection Process

#### 1. **Activity Creation and Execution**

When you create activities in your application code using Diginsight extensions:

```csharp
public async Task<Order> ProcessOrderAsync(int orderId)
{
    // Step 1: Activity is created and started using Diginsight extensions
    using var activity = ActivitySource.StartMethodActivity(new { orderId, service = "OrderService" });
    
    try
    {
        // Step 2: Your business logic executes
        var order = await GetOrderFromDatabase(orderId);
        await ValidateInventory(order);
        await ProcessPayment(order);
        await CreateShipment(order);
        
        // Step 3: Set outputs for rich observability
        activity?.SetOutput(new { order.Id, order.Status, totalAmount = order.TotalAmount });
        
        // Step 4: Activity automatically stops when using block ends
        return order;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
    // Step 5: Activity.Dispose() is called, triggering ActivityStopped event
}
```

#### 2. **Diginsight Activity Listener Registration**

During application startup, Diginsight registers ActivityListeners:

```csharp
// In your Program.cs or Startup.cs
builder.Services.AddDiginsightCore();                    // Core activity logging
builder.Services.AddSpanDurationMetricRecorder();       // Metrics collection

// Behind the scenes, this registers:
// - SpanDurationMetricRecorder as an IActivityListenerLogic
// - ActivityListener that responds to ActivityStopped events
// - Filtering based on ActivitySource names and configuration
```

#### 3. **Automatic Metric Recording**

When activities stop, Diginsight automatically captures metrics:

```csharp
// This happens automatically in SpanDurationMetricRecorder
public void ActivityStopped(Activity activity)
{
    // Check if we should record this activity
    if (!ShouldRecordActivity(activity)) return;
    
    // Extract duration and tags
    var duration = activity.Duration.TotalMilliseconds;
    var tags = BuildTagsFromActivity(activity);
    
    // Record to OpenTelemetry histogram
    spanDurationHistogram.Record(duration, tags);
    
    // Tags include:
    // - span_name: "ProcessOrderAsync"  
    // - status: "Ok" or "Error"
    // - orderId: "12345"
    // - service: "OrderService"
    // - Custom tags from activity.Tags
}
```

#### 4. **OpenTelemetry Processing and Export**

OpenTelemetry handles the metrics pipeline:

```csharp
// Configure OpenTelemetry metrics pipeline
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Diginsight.Diagnostics")      // Listen to Diginsight metrics
            .AddMeter("MyApp.*")                     // Your custom meters
            .AddRuntimeInstrumentation()             // .NET runtime metrics
            .AddHttpClientInstrumentation()         // HTTP metrics
            .AddAspNetCoreInstrumentation()         // ASP.NET Core metrics
            .AddView("diginsight.span_duration",     // Custom histogram buckets
                new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = new[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 }
                })
            .AddApplicationInsightsExporter()        // Export to Azure
            .AddPrometheusExporter();               // Export to Prometheus
    });
```

### Real-World Example: E-commerce Order Processing

Let's trace a complete order through the metrics collection system:

#### Application Code
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("ECommerce.OrderService");
    private readonly IOrderService orderService;
    private readonly ILogger<OrderController> logger;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // HTTP request automatically creates activity: "POST /api/order"
        using var orderActivity = ActivitySource.StartRichActivity("CreateOrder", 
            new { customer_id = request.CustomerId, item_count = request.Items.Count, order_value = request.TotalValue });

        try
        {
            var order = await orderService.ProcessOrderAsync(request);
            orderActivity?.SetOutput(new { order.Id, order.Status });
            return Ok(order);
        }
        catch (Exception ex)
        {
            orderActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return StatusCode(500, "Order processing failed");
        }
    }
}

public class OrderService : IOrderService
{
    private static readonly ActivitySource ActivitySource = new("ECommerce.OrderService");
    private readonly ILogger<OrderService> logger;

    public async Task<Order> ProcessOrderAsync(CreateOrderRequest request)
    {
        using var activity = ActivitySource.StartMethodActivity(new { request.CustomerId, request.TotalValue });
        
        // Step 1: Validate inventory
        using var inventoryActivity = ActivitySource.StartRichActivity("ValidateInventory", 
            new { items_to_check = request.Items.Count });
        await ValidateInventoryAsync(request.Items);
        inventoryActivity?.SetOutput(new { validation_passed = true });
        // Metric recorded: diginsight.span_duration{span_name="ValidateInventory", items_to_check="3"}

        // Step 2: Process payment  
        using var paymentActivity = ActivitySource.StartRichActivity("ProcessPayment", 
            new { payment_method = request.PaymentMethod, amount = request.TotalValue });
        await ProcessPaymentAsync(request.Payment);
        paymentActivity?.SetOutput(new { transaction_id = "txn_123", success = true });
        // Metric recorded: diginsight.span_duration{span_name="ProcessPayment", payment_method="credit_card", amount="99.99"}

        // Step 3: Create shipment
        using var shipmentActivity = ActivitySource.StartRichActivity("CreateShipment", 
            new { shipping_method = request.ShippingMethod });
        var shipment = await CreateShipmentAsync(order);
        shipmentActivity?.SetOutput(new { shipment.TrackingNumber, estimated_delivery = shipment.EstimatedDelivery });
        // Metric recorded: diginsight.span_duration{span_name="CreateShipment", shipping_method="express"}

        activity?.SetOutput(new { order.Id, order.Status, processing_time_ms = activity.Duration.TotalMilliseconds });
        return order;
        // Metric recorded: diginsight.span_duration{span_name="ProcessOrderAsync", customer_id="12345", total_value="99.99"}
    }
}
```

#### Metrics Generated

For a single order processing request, the following metrics are automatically collected:

```
# HTTP Request (from ASP.NET Core instrumentation)
http_request_duration{method="POST", route="/api/order", status_code="200"} = 1250ms

# Order Processing Steps (from Diginsight)
diginsight.span_duration{span_name="CreateOrder", customer_id="12345", item_count="3", order_value="99.99", status="Ok"} = 1200ms
diginsight.span_duration{span_name="ProcessOrderAsync", customer_id="12345", total_value="99.99", status="Ok"} = 1180ms
diginsight.span_duration{span_name="ValidateInventory", items_to_check="3", status="Ok"} = 45ms
diginsight.span_duration{span_name="ProcessPayment", payment_method="credit_card", amount="99.99", status="Ok"} = 850ms
diginsight.span_duration{span_name="CreateShipment", shipping_method="express", status="Ok"} = 285ms
```

#### Configuration for Collection

```json
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDurations": true,
      "ActivitySources": {
        "ECommerce.*": true,           // Record all ECommerce activities
        "Microsoft.AspNetCore": true,  // Include HTTP request metrics
        "System.*": false              // Exclude system activities
      },
      "SpanMeasuredActivityNames": [   // Only record specific operations
        "CreateOrder",
        "ProcessOrderAsync", 
        "ValidateInventory",
        "ProcessPayment",
        "CreateShipment"
      ]
    }
  },
  "OpenTelemetry": {
    "Metrics": {
      "ExportIntervalMilliseconds": 5000,    // Export every 5 seconds
      "MaxExportBatchSize": 512              // Batch size for efficiency
    }
  }
}
```

### Benefits of This Integration

1. **Zero Code Changes**: Existing activities automatically generate metrics
2. **Rich Context**: All activity tags become metric dimensions
3. **Performance Optimized**: Filtering happens before expensive metric operations
4. **Standardized**: Uses OpenTelemetry standards for interoperability
5. **Scalable**: Built-in aggregation and efficient export mechanisms

### Monitoring Dashboard Example

With these metrics, you can create powerful dashboards:

```promql
# Average order processing time by customer tier
avg(diginsight_span_duration{span_name="ProcessOrderAsync"}) by (customer_tier)

# 95th percentile payment processing time  
histogram_quantile(0.95, rate(diginsight_span_duration_bucket{span_name="ProcessPayment"}[5m]))

# Error rate for inventory validation
rate(diginsight_span_duration{span_name="ValidateInventory", status="Error"}[5m]) / 
rate(diginsight_span_duration{span_name="ValidateInventory"}[5m])

# Order processing throughput
rate(diginsight_span_duration{span_name="CreateOrder"}[1m])
```

This comprehensive integration provides automatic, detailed metrics collection with minimal configuration while maintaining high performance and observability standards.

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐    ┌──────────────────┐
│ Activity        │    │ ActivityListener │    │ SpanDurationMetric  │    │ OpenTelemetry    │
│ Creation        │───▶│ Events           │───▶│ Recorder            │───▶│ Metrics Export   │
│                 │    │                  │    │                     │    │                  │
│ using(activity) │    │ ActivityStopped  │    │ Metric.Record()     │    │ App Insights/    │
│ { ... }         │    │ Event Fires      │    │ Duration + Tags     │    │ Prometheus/etc   │
└─────────────────┘    └──────────────────┘    └─────────────────────┘    └──────────────────┘
```

### Detailed Flow Explanation

1. **Activity Creation**: When an Activity is created using Diginsight's `StartMethodActivity()` or `StartRichActivity()` extensions
2. **Listener Registration**: `SpanDurationMetricRecorder` registers as an `ActivityListener` 
3. **Activity Execution**: Your application code runs within the activity scope
4. **Activity Disposal**: When the `using` block ends, `Activity.Dispose()` is called
5. **Event Firing**: The `ActivityStopped` event fires automatically
6. **Duration Calculation**: The recorder calculates `activity.Duration.TotalMilliseconds`
7. **Metric Recording**: Duration and tags are recorded via `Metric.Record()`
8. **Export**: OpenTelemetry exports the metrics to configured destinations

## Key Components

### Core Classes

| Component | Purpose | Implementation Details |
|-----------|---------|----------------------|
| `SpanDurationMetricRecorder` | Core metrics recorder implementing `IActivityListenerLogic` | Handles ActivityStopped events and records durations |
| `SpanDurationMetricRecorderRegistration` | Registration wrapper with ActivitySource filtering | Wraps the recorder with source-specific configuration |
| `ActivityListenerExtensions` | Creates native .NET `ActivityListener` from registrations | Bridges Diginsight abstractions to .NET ActivityListener API |
| `DiginsightActivitiesOptions` | Configuration for metrics behavior and filtering | Controls which activities are recorded and how |

### Interface Hierarchy

```csharp
IActivityListenerLogic
├── SpanDurationMetricRecorder
└── ActivityLifecycleLogEmitter

IActivityListenerRegistration
├── SpanDurationMetricRecorderRegistration  
└── ActivityLifecycleLogEmitterRegistration
```

### Metrics Infrastructure

```csharp
// Core metric creation
private readonly Histogram<double> spanDurationHistogram;

// In constructor
spanDurationHistogram = meter.CreateHistogram<double>(
    name: "diginsight.span_duration",
    unit: "ms", 
    description: "Duration of spans"
);

// Recording measurements
spanDurationHistogram.Record(
    activity.Duration.TotalMilliseconds,
    new KeyValuePair<string, object?>("span_name", activity.DisplayName),
    new KeyValuePair<string, object?>("status", activity.Status.ToString())
);
```

## Registration Process

### Service Registration

```csharp
// High-level registration
services.AddSpanDurationMetricRecorder();

// Equivalent to:
services.AddClassAwareOptions();           // Options pattern support
services.AddActivityListenersAdder();      // Listener infrastructure  
services.AddMetrics();                     // .NET metrics support
services.AddSingleton<IActivityListenerRegistration, SpanDurationMetricRecorderRegistration>();
```

### Startup Registration

During application startup, the system automatically:

```csharp
// In HostedService or similar
foreach (IActivityListenerRegistration registration in registrations)
{
    var activityListener = registration.ToActivityListener();
    ActivitySource.AddActivityListener(activityListener);
}
```

### Configuration Binding

```csharp
// appsettings.json
{
  "Diginsight": {
    "Activities": {
      "RecordSpanDurations": true,
      "SpanMeasuredActivityNames": ["MyApp.*", "BusinessLogic.*"],
      "ActivitySources": {
        "MyApp.*": true,
        "System.*": false
      }
    }
  }
}
```

## Filtering and Configuration

### Multi-Level Filtering

The system provides several levels of filtering for fine-grained control:

#### 1. Global Control
```csharp
public class DiginsightActivitiesOptions
{
    public bool RecordSpanDurations { get; set; } = true;  // Master switch
}
```

#### 2. ActivitySource Level
```csharp
public class DiginsightActivitiesOptions  
{
    public Dictionary<string, bool> ActivitySources { get; set; } = new();
}
```

#### 3. Activity Name Level
```csharp
public class DiginsightActivitiesOptions
{
    public string[] SpanMeasuredActivityNames { get; set; } = Array.Empty<string>();
}
```

### Configuration Examples

```csharp
// Record all activities from MyApp namespace
builder.Services.Configure<DiginsightActivitiesOptions>(options =>
{
    options.RecordSpanDurations = true;
    options.ActivitySources["MyApp.*"] = true;
    options.ActivitySources["System.*"] = false;
    options.ActivitySources["Microsoft.*"] = false;
});

// Record only specific operations
builder.Services.Configure<DiginsightActivitiesOptions>(options =>
{
    options.SpanMeasuredActivityNames = new[] 
    {
        "ProcessOrderAsync",
        "GetCustomerAsync", 
        "ExternalApiCall"
    };
});
```

## Metrics Output

### OpenTelemetry Integration

Metrics are exported through OpenTelemetry's standard pipeline:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Diginsight.Diagnostics")  // Diginsight metrics
            .AddPrometheusExporter()             // Export to Prometheus
            .AddApplicationInsightsExporter();   // Export to App Insights
    });
```

### Application Insights Output

In Application Insights, span duration metrics appear as:

- **Metric Name**: `diginsight.span_duration` (configurable via options)
- **Dimensions**: 
  - `span_name`: The activity's DisplayName
  - `status`: Activity status (Ok, Error, etc.)
  - Custom tags from activity tags
- **Value**: Duration in milliseconds
- **Aggregation**: Histogram supporting percentile calculations (P50, P95, P99)

### Prometheus Output

```prometheus
# HELP diginsight_span_duration_ms Duration of spans
# TYPE diginsight_span_duration_ms histogram
diginsight_span_duration_ms_bucket{span_name="ProcessOrderAsync",status="Ok",le="10"} 45
diginsight_span_duration_ms_bucket{span_name="ProcessOrderAsync",status="Ok",le="50"} 123
diginsight_span_duration_ms_bucket{span_name="ProcessOrderAsync",status="Ok",le="100"} 156
diginsight_span_duration_ms_bucket{span_name="ProcessOrderAsync",status="Ok",le="+Inf"} 167
diginsight_span_duration_ms_sum{span_name="ProcessOrderAsync",status="Ok"} 2847.3
diginsight_span_duration_ms_count{span_name="ProcessOrderAsync",status="Ok"} 167
```

## Performance Considerations

### Optimization Strategies

1. **Lazy Creation**: Metrics are only created when first needed
   ```csharp
   private Histogram<double>? spanDurationHistogram;
   
   private Histogram<double> GetOrCreateHistogram()
   {
       return spanDurationHistogram ??= meter.CreateHistogram<double>("diginsight.span_duration");
   }
   ```

2. **Fast Filtering**: Early exit when recording is disabled
   ```csharp
   public void ActivityStopped(Activity activity)
   {
       if (!options.RecordSpanDurations) return;  // Fast exit
       if (!ShouldRecord(activity)) return;       // Fast filtering
       
       RecordDuration(activity);
   }
   ```

3. **Exception Safety**: Metric failures don't affect application flow
   ```csharp
   try
   {
       histogram.Record(duration, tags);
   }
   catch (Exception ex)
   {
       // Log error but don't propagate
       logger.LogWarning(ex, "Failed to record span duration metric");
   }
   ```

4. **Efficient Tagging**: Minimal memory allocation for tag arrays
   ```csharp
   private static readonly KeyValuePair<string, object?>[] EmptyTags = Array.Empty<KeyValuePair<string, object?>>();
   
   private KeyValuePair<string, object?>[] BuildTags(Activity activity)
   {
       var tagCount = 2 + activity.Tags.Count();
       var tags = new KeyValuePair<string, object?>[tagCount];
       // ... populate efficiently
       return tags;
   }
   ```

### Memory and CPU Impact

- **Memory**: Minimal overhead per activity (~100 bytes for tag arrays)
- **CPU**: Microsecond-level overhead per activity stop event
- **Garbage Collection**: Pre-allocated tag arrays reduce GC pressure
- **Threading**: Thread-safe metric recording via OpenTelemetry's built-in synchronization

## Advanced Configuration

### Custom Metric Names

```csharp
builder.Services.Configure<SpanDurationMetricRecorderSettings>(options =>
{
    options.MetricName = "my_app.operation_duration";
    options.MetricDescription = "Duration of business operations";
    options.MetricUnit = "milliseconds";
});
```

### Tag Customization

```csharp
public class CustomSpanDurationMetricRecorder : SpanDurationMetricRecorder
{
    protected override KeyValuePair<string, object?>[] BuildTags(Activity activity)
    {
        var baseTags = base.BuildTags(activity);
        
        // Add custom tags
        return baseTags.Concat(new[]
        {
            new KeyValuePair<string, object?>("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")),
            new KeyValuePair<string, object?>("version", Assembly.GetExecutingAssembly().GetName().Version?.ToString())
        }).ToArray();
    }
}
```

### Conditional Recording

```csharp
public class ConditionalSpanDurationMetricRecorder : SpanDurationMetricRecorder
{
    protected override bool ShouldRecord(Activity activity)
    {
        // Only record activities longer than 100ms
        if (activity.Duration.TotalMilliseconds < 100) return false;
        
        // Only record business operations
        if (!activity.Source.Name.StartsWith("MyApp.Business")) return false;
        
        return base.ShouldRecord(activity);
    }
}
```

## Troubleshooting

### Common Issues

#### 1. No Metrics Appearing

**Symptoms**: No span duration metrics in your monitoring system

**Diagnostics**:
```csharp
// Check if recording is enabled
var options = serviceProvider.GetService<IOptions<DiginsightActivitiesOptions>>();
Console.WriteLine($"RecordSpanDurations: {options.Value.RecordSpanDurations}");

// Check if ActivitySource is registered
var sources = options.Value.ActivitySources;
foreach (var source in sources)
{
    Console.WriteLine($"Source: {source.Key} = {source.Value}");
}
```

**Solutions**:
- Ensure `RecordSpanDurations = true` in configuration
- Verify ActivitySource patterns match your activity sources
- Check OpenTelemetry exporter configuration

#### 2. Missing Activity Sources

**Symptoms**: Some activities not being recorded

**Diagnostics**:
```csharp
// Add debugging to see which activities are created
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = source => 
    {
        Console.WriteLine($"ActivitySource created: {source.Name}");
        return true;
    },
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
});
```

**Solutions**:
- Use broader patterns like `"MyApp.*"` instead of exact names
- Check activity source names in your code
- Verify the listener is registered before activities are created

#### 3. Performance Issues

**Symptoms**: High CPU usage or memory consumption

**Diagnostics**:
```csharp
// Monitor metric recording frequency
public class DiagnosticSpanDurationMetricRecorder : SpanDurationMetricRecorder
{
    private static long recordingCount = 0;
    
    public override void ActivityStopped(Activity activity)
    {
        Interlocked.Increment(ref recordingCount);
        if (recordingCount % 1000 == 0)
        {
            Console.WriteLine($"Recorded {recordingCount} span durations");
        }
        base.ActivityStopped(activity);
    }
}
```

**Solutions**:
- Implement more restrictive filtering
- Use sampling to reduce metric volume
- Monitor for metric creation hotspots

### Debugging Tools

#### Enable Detailed Logging

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddDiginsightConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddFilter("Diginsight.Diagnostics", LogLevel.Trace);
});
```

#### Custom Diagnostics

```csharp
public class DiagnosticActivityListener : IActivityListenerLogic
{
    public void ActivityStarted(Activity activity)
    {
        Console.WriteLine($"STARTED: {activity.Source.Name}.{activity.DisplayName}");
    }
    
    public void ActivityStopped(Activity activity)
    {
        Console.WriteLine($"STOPPED: {activity.Source.Name}.{activity.DisplayName} ({activity.Duration.TotalMilliseconds:F2}ms)");
    }
}
```

## Integration with OpenTelemetry

### Complete Setup Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Diginsight with metrics
builder.Services.AddDiginsightCore();
builder.Services.AddSpanDurationMetricRecorder();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Diginsight.Diagnostics")    // Diginsight metrics
            .AddMeter("MyApp.*")                   // Application metrics
            .AddRuntimeInstrumentation()           // .NET runtime metrics
            .AddHttpClientInstrumentation()       // HTTP client metrics
            .AddAspNetCoreInstrumentation()       // ASP.NET Core metrics
            .AddPrometheusExporter()              // Export to Prometheus
            .AddApplicationInsightsExporter();    // Export to Application Insights
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Diginsight.Diagnostics")
            .AddSource("MyApp.*")
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddApplicationInsightsExporter();
    });

var app = builder.Build();

// Configure Prometheus endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

### Correlation with Tracing

Span duration metrics automatically correlate with distributed tracing:

```csharp
// Same activity provides both trace spans and duration metrics
using var activity = ActivitySource.StartRichActivity("ProcessOrder", 
    new { customerId, orderValue = orderTotal });

// Trace: Creates a span in distributed trace
// Metrics: Records duration with customerId and orderValue as dimensions
await ProcessOrderAsync(order);

activity?.SetStatus(ActivityStatusCode.Ok);
// Both trace and metric will have the same status information
```

This dual instrumentation provides comprehensive observability:
- **Traces**: Show request flow and detailed operation context
- **Metrics**: Enable aggregated performance analysis and alerting

### Best Practices

1. **Consistent Naming**: Use the same activity names for both tracing and metrics
2. **Meaningful Tags**: Add business-relevant tags that are useful for both traces and metrics
3. **Sampling Coordination**: Configure trace and metric sampling consistently
4. **Resource Attributes**: Include deployment and service information in both signals
5. **Error Handling**: Ensure both traces and metrics capture error conditions

This comprehensive metrics collection system provides deep insights into application performance while maintaining minimal overhead and maximum flexibility for different deployment scenarios.
