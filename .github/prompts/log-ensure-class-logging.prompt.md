---
name: log-ensure-class-logging
description: implements diginsight observability into current class
agent: agent
model: claude-opus-4.6
tools: ['codebase', 'fetch']
argument-hint: 'class="ClassName"'
---

# 🔍 Log-EnsureClassLogging

## 📝 **Prompt Overview**

This prompt implements methods observability in C# classes using **diginsight ActivitySource.StartMethodActivity()**, **diginsight ActivitySource.StartRichActivity()** and **activity?.SetOutput();**.<br>

It reviews and understands diginsight logic for adding log to methods by means of  **diginsight ActivitySource.StartMethodActivity()**, **diginsight ActivitySource.StartRichActivity()** and **activity?.SetOutput();**.

Then it adds structured logging and monitoring to public methods that call external services, databases, adapters, or implement business logic, enabling better debugging, performance monitoring, and distributed tracing.

⚠️ **CRITICAL REQUIREMENT:**
. methods start is instrumented with `using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { ... });`
. methods return value is instrumented with `activity?.SetOutput(result);`


## 🎯 **Prompt Goal**

### **Core Objectives:**
- **📊 Add Activity Tracking:** Implement `using var activity = Observability.ActivitySource.StartMethodActivity` in appropriate public methods
- **📝 Parameter Logging:** Include relevant method parameters for debugging and monitoring
- **📈 Result Tracking:** Add `activity?.SetOutput(result)` to track method return values
- **⚡ Selective Implementation:** Apply observability only where it adds value, avoiding performance overhead in high-frequency or simple methods
- **🔒 Business Logic Preservation:** Maintain all existing exception handling and return patterns exactly as they were

### **Implementation Criteria:**

**✅ ADD activity tracking if:**
- The method is calling databases or external services
- The method is calling adapters, repository classes, or services with business logic
- The method implements relevant business logic
- The method is public and has significant logic worth monitoring

**❌ AVOID adding it if:**
- The method is a constructor with medium or small logic or fully in-memory logic
- The method is private with medium or small logic or fully in-memory logic
- The method is public with small logic or fully in-memory logic
- The method is likely to be used in a tight loop
- The method is a simple property getter/setter or basic validation

## 📤 **Expected Output**

### **Code Pattern:**
**If original method returns errors (don't throw) - PRESERVE THIS PATTERN:**
```csharp
public async Task<ServiceResult<Data>> GetData(Guid plantId, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });

    ServiceResult<Data> result;
    try
    {
        result = await ProcessBusinessLogic(plantId, context).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while getting data for plant {PlantId}", plantId);
        result = ServiceResult<Data>.CreateError(ex); // ✅ Keep original pattern - return error result
    }
    
    activity?.SetOutput(result); // Log final result
    return result;
}
```

**If original method throws exceptions - PRESERVE THIS PATTERN:**
```csharp
public async Task<ServiceResult<Data>> GetData(Guid plantId, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });

    try
    {
        ServiceResult<Data> result = await ProcessBusinessLogic(plantId, context).ConfigureAwait(false);
        
        activity?.SetOutput(result); // Log success result
        return result;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while getting data for plant {PlantId}", plantId);
        throw; // ✅ Keep original pattern - rethrow exception
    }
}
```

**If original method swallows exceptions and returns null/default - PRESERVE THIS PATTERN:**
```csharp
public async Task<ServiceResult<Data>> GetData(Guid plantId, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });

    try
    {
        ServiceResult<Data> result = await ProcessBusinessLogic(plantId, context).ConfigureAwait(false);
        
        activity?.SetOutput(result);
        return result;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while getting data for plant {PlantId}", plantId);
        ServiceResult<Data> errorResult = ServiceResult<Data>.CreateSuccess(null); // ✅ Keep original pattern - return null on error
        activity?.SetOutput(errorResult);
        return errorResult;
    }
}
```

### **Required Using Directives:**
```csharp
using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger in static methods
```

### **Infrastructure Requirements:**
**STEP 1:** Always verify if `Observability` class exists by looking for `Observability.ActivitySource` usage in existing code.

**STEP 2:** If `Observability` class is missing, add it at the project root level:
```csharp
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Diginsight.Diagnostics;

namespace YourProject.Namespace;

internal static class Observability
{
    public static readonly ActivitySource ActivitySource = new(Assembly.GetExecutingAssembly().GetName().Name!);
    public static ILoggerFactory LoggerFactory { get; set; } = null!;
    static Observability() => ObservabilityRegistry.RegisterComponent(factory => LoggerFactory = factory);
}
```

**Key Points:**
- The static constructor registers this assembly's LoggerFactory setter with `ObservabilityRegistry`
- This allows centralized initialization from the application startup
- Each assembly registers itself independently

**STEP 3: Create Observability Class (If Missing)**

If STEP 2 finds nothing, create at project root:

````````
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Diginsight.Diagnostics;

namespace YourProject.Namespace;

internal static class Observability
{
    public static readonly ActivitySource ActivitySource = new(Assembly.GetExecutingAssembly().GetName().Name!);
    public static ILoggerFactory LoggerFactory { get; set; } = null!;
    static Observability() => ObservabilityRegistry.RegisterComponent(factory => LoggerFactory = factory);
}
````````

**Key Points:**
- The static constructor registers this assembly's LoggerFactory setter with `ObservabilityRegistry`
- This allows centralized initialization from the application startup
- Each assembly registers itself independently

**STEP 3:** Ensure `ObservabilityRegistry.RegisterLoggerFactory()` is called once in your application startup:
```csharp
// In Program.cs or Startup.cs - ONLY ONCE for the entire application
ObservabilityRegistry.RegisterLoggerFactory(observabilityManager.LoggerFactory);

// This single call initializes LoggerFactory for ALL registered assemblies
```

**Benefits of this approach:**
- ✅ Each assembly is self-contained and registers itself
- ✅ Application startup only needs one line of code
- ✅ All assemblies get their LoggerFactory initialized automatically
- ✅ No need to manually initialize each assembly's Observability class

## 📚 **Implementation Guidelines**

### **StartMethodActivity Pattern:**
```csharp
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { ... });
```
**Required Components:**
1. **Logger Parameter:** Always include `logger` as the first parameter
2. **Lambda Expression:** Use `() => new { ... }` pattern for deferred parameter evaluation
3. **Parameter Selection:** Include only relevant, simple parameters

### **Logger Creation for Static Methods:**

**✅ CORRECT Pattern - Create logger from Observability.LoggerFactory:**
```csharp
public static RSSFeedChannel ParseRSS(string xmlContent)
{
    var logger = Observability.LoggerFactory?.CreateLogger<RSSFeedParser>() ?? NullLogger<RSSFeedParser>.Instance;
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { });
    
    // Business logic...
    
    activity?.SetOutput(rssFeed);
    return rssFeed;
}
```

**❌ WRONG Pattern - Don't pass logger as parameter:**
```csharp
// ❌ DON'T DO THIS
public static RSSFeedChannel ParseRSS(string xmlContent, ILogger logger = null!)
{
    // This pollutes the method signature
}

// ✅ DO THIS INSTEAD
public static RSSFeedChannel ParseRSS(string xmlContent)
{
    var logger = Observability.LoggerFactory?.CreateLogger<RSSFeedParser>() ?? NullLogger<RSSFeedParser>.Instance;
    // ...
}
```

### **Parameter Logging Rules:**

**✅ Include parameters - Concrete Examples:**
```csharp
// ✅ Basic parameters
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType });

// ✅ With business objects
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, assetId, plantType, assetEditDto });

// ✅ With collections (materialized)
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType, sortFilterParameters });

// ✅ For methods with large content (like XML), use empty anonymous object
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { }); // Don't log large xmlContent
```

**❌ Exclude parameters - Concrete Examples:**
```csharp
// ❌ DON'T include context, tokens, or sensitive data
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { 
    plantId, 
    plantType,
    // context,           // ❌ Never include - contains sensitive data
    // cancellationToken, // ❌ Never include - not useful for debugging  
    // file               // ❌ Never include - large objects
    // xmlContent         // ❌ Never include - large content
});

// ✅ DO - Clean parameter list
using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType });
```

### **SetOutput Implementation - Single Point of Exit Pattern:**

**✅ PREFERRED: Single point of exit with consolidated SetOutput:**
```csharp
public async Task<ServiceResult<Data>> ProcessData(string plantId, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });

    // Early validation returns - can exit directly without SetOutput
    if (string.IsNullOrWhiteSpace(plantId))
    {
        return ServiceResult<Data>.CreateErrorResourceNotFound(); // No SetOutput needed - parameter already logged
    }

    ServiceResult<Data> result; // Single result variable
    try
    {
        result = await ProcessBusinessLogic(plantId, context).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while processing data for plant {PlantId}", plantId);
        result = ServiceResult<Data>.CreateError(ex); // Keep original behavior
    }

    activity?.SetOutput(result); // ✅ Single point of exit - log final result
    return result;
}
```

**✅ Add `activity?.SetOutput(result)` guidelines:**
- **PREFERRED:** Create a single point of exit where one `activity?.SetOutput(result)` statement logs the final result
- **Limit method exit points** as much as possible - use a single result variable and one return statement
- The method returns a value (not void)
- Right before the final return statement
- After all business logic is complete
- For BOTH success and error results (to track the final outcome)

**✅ Exception for early validation returns:**
```csharp
// ✅ Early parameter validation - can return directly without SetOutput
if (string.IsNullOrWhiteSpace(plantId))
{
    return ServiceResult<Data>.CreateErrorResourceNotFound(); // No SetOutput - parameter already logged
}

if (assetId == Guid.Empty)
{
    return ServiceResult<Data>.CreateErrorBadRequest(); // No SetOutput - avoid excessive nesting
}
```

**❌ AVOID multiple exit points with SetOutput:**
```csharp
// ❌ Avoid this pattern - multiple SetOutput calls
public async Task<ServiceResult<Data>> ProcessData(string plantId, string plantType, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });

    try
    {
        ServiceResult<Data> successResult = await ProcessBusinessLogic(plantId, context).ConfigureAwait(false);
        activity?.SetOutput(successResult); // ❌ Multiple SetOutput calls make debugging harder
        return successResult;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred");
        ServiceResult<Data> errorResult = ServiceResult<Data>.CreateError(ex);
        activity?.SetOutput(errorResult); // ❌ Multiple SetOutput calls
        return errorResult;
    }
}
```

### **Exception Handling - PRESERVE ORIGINAL BEHAVIOR:**

**Pattern 1: Original method throws exceptions - Keep throwing:**
```csharp
catch (Exception ex)
{
    this.logger.LogError(ex, "Exception occurred while [action] for {Parameter1} of type {Parameter2}", param1, param2);
    throw; // ✅ Keep original behavior - rethrow
}
```

**Pattern 2: Original method swallows exceptions and returns error results - Keep swallowing:**
```csharp
catch (Exception ex)
{
    this.logger.LogError(ex, "Exception occurred while [action] for {Parameter1} of type {Parameter2}", param1, param2);
    result = ServiceResult<T>.CreateError(ex); // ✅ Keep original behavior - assign to result variable
}
// Single SetOutput and return outside catch block
activity?.SetOutput(result);
return result;
```

**Pattern 3: Original method swallows exceptions and returns null/default - Keep that pattern:**
```csharp
catch (Exception ex)
{
    this.logger.LogError(ex, "Exception occurred while [action] for {Parameter1} of type {Parameter2}", param1, param2);
    result = ServiceResult<T>.CreateSuccess(null); // ✅ Keep original behavior - assign to result variable
}
// Single SetOutput and return outside catch block  
activity?.SetOutput(result);
return result;
```

**❌ AVOID changing the original exception handling pattern:**
```csharp
// ❌ DON'T change from swallowing to throwing
// ❌ DON'T change from throwing to swallowing  
// ❌ DON'T change return types or error codes
```

## ⚠️ **CRITICAL REQUIREMENT: PRESERVE ORIGINAL BUSINESS LOGIC**

**🚨 ALWAYS PRESERVE the original method's business logic and exception handling behavior:**

- **✅ If the original method swallows exceptions and returns success/error results → KEEP that pattern**
- **✅ If the original method throws exceptions → KEEP throwing exceptions**
- **✅ If the original method returns specific error codes → PRESERVE those patterns**
- **✅ If the original method has early validation returns → KEEP them exactly as-is**
- **❌ NEVER change the fundamental exception handling or return behavior of existing methods**

**The observability enhancement must be transparent to the existing application behavior.**

## 🔧 **Implementation Steps**

### **Step 1: Infrastructure Verification**
1. Search for existing `Observability.ActivitySource` usage in the project
2. If found, note the namespace where `Observability` is defined  
3. If not found, create the `Observability` class in the main project namespace
4. Verify `using Diginsight.Diagnostics;` is present

### **Step 2: Analyze Original Method Behavior**
Before modifying any method, identify:
- **Exception handling pattern:** Does it throw or swallow exceptions?
- **Return patterns:** What does it return on success/failure?
- **Early exits:** Are there validation returns that bypass main logic?
- **Error handling:** How are errors currently communicated?

### **Step 3: Identify Target Methods**
Apply these rules strictly:
- ✅ Public methods that call external services/adapters
- ✅ Public methods with complex business logic
- ✅ Public methods that return `ServiceResult<T>` or similar
- ❌ Constructors (even complex ones)
- ❌ Simple property mappers or validators
- ❌ Private helper methods

### **Step 4: Add Activity Tracking (Preserve Original Behavior)**
For each target method:
1. Add `using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { ... });` at method start
2. Include only business-relevant parameters (exclude context, tokens, files)
3. **Preserve the original exception handling pattern exactly**
4. **Use single point of exit pattern:** Create one result variable and one `activity?.SetOutput(result)` statement
5. Allow early validation returns without SetOutput to avoid excessive nesting
6. Ensure no changes to method behavior or return values

### **Step 5: Validation**
1. Compile code to check for errors
2. Verify original exception handling behavior is preserved
3. Verify no context/token parameters are logged
4. Ensure early validation returns don't have SetOutput calls
5. **Verify the method behaves exactly the same as before (critical)**
6. Confirm single point of exit pattern with one SetOutput call

## 🛠️ **Method-Specific Patterns - Preserving Original Behavior**

### **Static Methods Pattern - Using Observability.LoggerFactory:**
```csharp
public static AtomFeedChannel ParseAtom(string xmlContent)
{
    var logger = Observability.LoggerFactory?.CreateLogger<AtomFeedParser>() ?? NullLogger<AtomFeedParser>.Instance;
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { });

    var doc = XDocument.Parse(xmlContent);
    var ns = doc.Root.GetDefaultNamespace();
    var feed = doc.Root;

    if (feed.Name.LocalName != "feed")
        throw new InvalidOperationException("Invalid Atom feed: <feed> element not found");

    var atomFeed = new AtomFeedChannel
    {
        Id = feed.Element(ns + "id")?.Value,
        Title = feed.Element(ns + "title")?.Value,
        // ... populate feed properties
    };

    // ... parse feed logic

    activity?.SetOutput(atomFeed);
    return atomFeed;
}
```

**Key Requirements for Static Methods:**
1. **Logger Creation:** Use `Observability.LoggerFactory?.CreateLogger<TClass>() ?? NullLogger<TClass>.Instance`
2. **Class Type Parameter:** Use the class containing the method (e.g., `CreateLogger<AtomFeedParser>()`)
3. **Null Safety:** Always provide fallback to `NullLogger<TClass>.Instance`
4. **No Logger Parameters:** Never add logger as a method parameter
5. **Single SetOutput:** Follow same single-point-of-exit pattern as instance methods
6. **Required Using:** Add `using Microsoft.Extensions.Logging.Abstractions;` for `NullLogger`

### **CRUD Operations Pattern - Single Point of Exit:**
```csharp
public async Task<ServiceResult<string>> Create(string plantId, string plantType, AssetEditDto assetEditDto, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType, assetEditDto });
    
    // Early validation - can return directly (preserve original pattern)
    if (string.IsNullOrWhiteSpace(plantId))
    {
        return ServiceResult<string>.CreateErrorResourceNotFound(); // No SetOutput needed
    }
    
    ServiceResult<string> result; // Single result variable
    try
    {
        result = await ProcessCreate(plantId, plantType, assetEditDto, context).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while creating asset for plant {PlantId} of type {PlantType}", plantId, plantType);
        
        // If original throws - rethrow, if original swallows - assign result
        if (originalMethodThrows)
        {
            throw; // ✅ Keep original behavior - rethrow
        }
        else
        {
            result = ServiceResult<string>.CreateSuccess(null); // ✅ Keep original behavior - return null on error
        }
    }
    
    activity?.SetOutput(result); // ✅ Single point of exit
    return result;
}
```

### **CRUD Operations Pattern - Original Swallows Exceptions:**
```csharp
public async Task<ServiceResult<string>> Create(string plantId, string plantType, AssetEditDto assetEditDto, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType, assetEditDto });
    
    try
    {
        // Business logic
        ServiceResult<string> result = await ProcessCreate(plantId, plantType, assetEditDto, context).ConfigureAwait(false);
        
        activity?.SetOutput(result);
        return result;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while creating asset for plant {PlantId} of type {PlantType}", plantId, plantType);
        ServiceResult<string> errorResult = ServiceResult<string>.CreateSuccess(null); // ✅ Keep original behavior - return null on error
        activity?.SetOutput(errorResult);
        return errorResult;
    }
}
```

### **Query Operations Pattern - Original Returns Null on Exception:**
```csharp
public async Task<ServiceResult<IEnumerable<T>>> GetItems(string plantId, string plantType, SortFilterParameters filters, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType, filters });
    
    ServiceResult<IEnumerable<T>> result; // Single result variable
    try
    {
        result = await QueryItems(plantId, plantType, filters, context).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while getting items for plant {PlantId} of type {PlantType}", plantId, plantType);
        result = null; // ✅ Keep original behavior - return null
    }
    
    activity?.SetOutput(result); // ✅ Single point of exit
    return result;
}
```

## 🚫 **Anti-Patterns to Avoid**

### **❌ DON'T Use Multiple SetOutput Calls:**
```csharp
// ❌ WRONG - Multiple exit points with SetOutput
public async Task<ServiceResult<T>> BadExample()
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { });
    
    try
    {
        ServiceResult<T> result = await ProcessLogic().ConfigureAwait(false);
        activity?.SetOutput(result); // ❌ First SetOutput
        return result;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred");
        ServiceResult<T> errorResult = ServiceResult<T>.CreateSuccess(null);
        activity?.SetOutput(errorResult); // ❌ Second SetOutput - hard to debug
        return errorResult;
    }
}

// ✅ CORRECT - Single exit point with SetOutput
public async Task<ServiceResult<T>> GoodExample()
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { });
    
    ServiceResult<T> result; // Single result variable
    try
    {
        result = await ProcessLogic().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred");
        result = ServiceResult<T>.CreateSuccess(null); // ✅ Keep original behavior
    }
    
    activity?.SetOutput(result); // ✅ Single SetOutput call
    return result;
}
```

### **❌ DON'T Change Original Exception Handling:**
```csharp
// ❌ WRONG - Original method swallowed exceptions, but now we're throwing
public async Task<ServiceResult<T>> OriginalMethod()
{
    // Original code: catch(Exception) { return ServiceResult<T>.CreateSuccess(null); }
    
    try
    {
        // logic
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred");
        throw; // ❌ WRONG - Original didn't throw!
    }
}

// ✅ CORRECT - Preserve original exception swallowing
public async Task<ServiceResult<T>> OriginalMethod()
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { });
    
    try
    {
        ServiceResult<T> result = await ProcessLogic().ConfigureAwait(false);
        activity?.SetOutput(result);
        return result;
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred");
        ServiceResult<T> nullResult = ServiceResult<T>.CreateSuccess(null); // ✅ Keep original behavior
        activity?.SetOutput(nullResult);
        return nullResult;
    }
}
```

### **❌ DON'T Add SetOutput to Early Validation Returns:**
```csharp
// ❌ WRONG - Adding SetOutput to parameter validation
public async Task<ServiceResult<T>> BadValidation(string plantId)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });
    
    if (string.IsNullOrWhiteSpace(plantId))
    {
        ServiceResult<T> validationResult = ServiceResult<T>.CreateErrorResourceNotFound();
        activity?.SetOutput(validationResult); // ❌ Unnecessary - parameter already logged
        return validationResult;
    }
    
    // ... rest of method
}

// ✅ CORRECT - Early validation without SetOutput
public async Task<ServiceResult<T>> GoodValidation(string plantId)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId });
    
    if (string.IsNullOrWhiteSpace(plantId))
    {
        return ServiceResult<T>.CreateErrorResourceNotFound(); // ✅ No SetOutput needed
    }
    
    // ... rest of method with single SetOutput at end
}
```

## ✅ **Success Checklist**

### **Infrastructure:**
- [ ] `using Diginsight.Diagnostics;` is added
- [ ] `Observability` class exists and is accessible
- [ ] Logger field/parameter is available in target class

### **Method Selection:**
- [ ] Only public methods with external calls have activity tracking
- [ ] Constructors are excluded
- [ ] Simple helper/mapping methods are excluded
- [ ] CRUD and query operations are included

### **Parameter Logging:**
- [ ] Logger is first parameter in StartMethodActivity
- [ ] Lambda expression pattern used: `() => new { ... }`
- [ ] No context, tokens, or sensitive data in parameters
- [ ] Only business-relevant parameters included

### **SetOutput Usage - Single Point of Exit:**
- [ ] **Single `activity?.SetOutput(result)` call per method (preferred)**
- [ ] Added before final return statement
- [ ] **Single result variable used throughout method**
- [ ] Not used in early validation returns (parameter already logged)
- [ ] Used only for methods that return values
- [ ] **Method has minimal exit points for better maintainability**

### **Exception Handling - CRITICAL:**
- [ ] **Original exception handling pattern is preserved exactly**
- [ ] **Methods that threw exceptions still throw exceptions**
- [ ] **Methods that swallowed exceptions still swallow exceptions**
- [ ] **Methods that returned null on error still return null on error**
- [ ] **No changes to application behavior or error handling**
- [ ] Meaningful error messages with relevant parameters

### **Code Quality:**
- [ ] Code compiles without errors
- [ ] **Method behavior is identical to original (critical)**
- [ ] No performance impact on simple methods
- [ ] Consistent patterns across all methods
- [ ] **Clean, maintainable code with single point of exit**

## 🎯 **Final Verification**

After implementation, verify these patterns:

### **✅ Good Example - Single Point of Exit with Preserved Exception Swallowing:**
```csharp
// Original method swallowed exceptions and returned null
public async Task<ServiceResult<string>> ProcessData(string plantId, string plantType, DataDto input, ContextBase context)
{
    using var activity = Observability.ActivitySource.StartMethodActivity(logger, () => new { plantId, plantType, input });
    
    // Early validation - no SetOutput needed
    if (string.IsNullOrWhiteSpace(plantId))
    {
        return ServiceResult<string>.CreateErrorResourceNotFound(); 
    }
    
    ServiceResult<string> result; // Single result variable
    try
    {
        result = await businessService.Process(plantId, plantType, input, context).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Exception occurred while processing data for plant {PlantId} of type {PlantType}", plantId, plantType);
        result = ServiceResult<string>.CreateSuccess(null); // ✅ Preserve original behavior - return null
    }
    
    activity?.SetOutput(result); // ✅ Single point of exit
    return result;
}
```

**🚨 REMEMBER: The key success criteria are:**
1. **Enhanced method behaves exactly the same as the original method**
2. **Single point of exit with one SetOutput call for better maintainability**
3. **Early validation returns can skip SetOutput to avoid excessive nesting**
4. **Added observability logging with no changes to business logic**
