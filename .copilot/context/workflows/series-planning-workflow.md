# Series Planning Workflow

This workflow guides you through creating a cohesive, well-structured article series that takes readers on a learning journey.

## What is an Article Series?

An **article series** is a collection of related articles that:
- Cover a single broad topic in depth
- Follow a logical learning progression
- Build on each other's concepts
- Maintain consistency in style and terminology
- Have clear prerequisites and dependencies

**Examples:**
- "Getting Started with GitHub Copilot" (4 articles: Basics → Configuration → Advanced → Best Practices)
- "Docker for Developers" (5 articles: Introduction → Containers → Images → Compose → Production)
- "Prompt Engineering Fundamentals" (6 articles: Basics → Structure → Tools → Validation → Automation → Advanced)

## Why Create a Series?

### Benefits:
✅ **Structured learning path**: Guides readers from beginner to proficient
✅ **Manageable scope**: Break large topics into digestible chunks
✅ **Better organization**: Easier to maintain and update
✅ **Cross-promotion**: Articles naturally link to each other
✅ **Progressive complexity**: Build on foundation systematically

### When to Create a Series:
- Topic too large for single article (> 4000 words)
- Natural progression from simple to complex
- Multiple related subtopics
- Clear learning objectives at each stage
- Audience needs structured path

## Series Planning Process

```
Idea → Scope → Outline → Structure → Create → Validate → Publish
```

## Phase 1: Series Definition

### Step 1.1: Define the Topic

**Questions to answer:**
- What is the overarching topic?
- Who is the target audience?
- What will readers achieve by completing the series?
- What's the scope (breadth and depth)?

**Example:**
```
Topic: "Building REST APIs with Node.js"
Audience: Intermediate JavaScript developers
Goal: Readers can build production-ready REST APIs
Scope: Express.js, authentication, database, deployment
```

### Step 1.2: Identify Learning Objectives

**Define what readers will learn by the end:**
- [ ] Core concept understanding
- [ ] Practical skills acquired
- [ ] Tools they can use
- [ ] Problems they can solve

**Example:**
```markdown
After completing this series, readers will:
1. Understand REST API principles
2. Set up Express.js server
3. Implement CRUD operations
4. Add authentication and authorization
5. Connect to MongoDB
6. Deploy to production
```

### Step 1.3: Determine Prerequisites

**What should readers know before starting?**
- Required knowledge
- Assumed skills
- Tools they need
- Previous articles (if any)

**Document:**
```yaml
series:
  name: "Building REST APIs with Node.js"
  prerequisites:
    - "Basic JavaScript ES6+"
    - "Understanding of HTTP basics"
    - "Node.js installed (v18+)"
    - "VS Code or similar editor"
  skill_level: "intermediate"
```

## Phase 2: Series Structure

### Step 2.1: Brainstorm Topics

**List all potential topics related to the theme:**
- Core concepts
- Common use cases
- Advanced techniques
- Best practices
- Troubleshooting

**Example brainstorm:**
```
- What is REST?
- Setting up Express
- Routing basics
- Request/response handling
- Middleware
- Error handling
- Authentication (JWT)
- Database integration
- Testing APIs
- API documentation
- Deployment
- Monitoring
- Scaling
```

### Step 2.2: Group and Sequence

**Organize topics into logical articles:**

**Principles:**
- Foundation before advanced concepts
- Prerequisites before dependent topics
- Practical examples early
- Complexity increases gradually

**Example structure:**
```
Article 1: REST API Fundamentals
  - What is REST?
  - HTTP methods
  - Status codes
  - API design principles

Article 2: Setting Up Express Server
  - Project setup
  - Basic routing
  - Middleware basics
  - Request/response

Article 3: Building CRUD Endpoints
  - Create operations
  - Read operations
  - Update operations
  - Delete operations

Article 4: Authentication & Security
  - JWT basics
  - Auth middleware
  - Password hashing
  - Security best practices

Article 5: Database Integration
  - MongoDB setup
  - Mongoose models
  - CRUD with database
  - Error handling

Article 6: Testing & Deployment
  - Unit testing
  - Integration testing
  - Deployment options
  - Production checklist
```

### Step 2.3: Define Dependencies

**Map prerequisites for each article:**

```yaml
Article 1: None (entry point)
Article 2: Article 1
Article 3: Article 2
Article 4: Article 2, Article 3
Article 5: Article 3
Article 6: Article 4, Article 5
```

**Visualize:**
```
Article 1 (Foundation)
    ↓
Article 2 (Setup)
    ↓
    ├→ Article 3 (CRUD)
    │      ↓
    │  Article 5 (Database)
    │      ↓
    └→ Article 4 (Auth)
           ↓
       Article 6 (Testing/Deploy)
```

### Step 2.4: Scope Each Article

**For each article, define:**
- Title
- Learning objectives (3-5)
- Main sections
- Estimated length (words)
- Examples needed
- Time to complete (for reader)

**Template:**
```markdown
## Article 3: Building CRUD Endpoints

**Learning Objectives:**
- [ ] Create POST endpoint for resources
- [ ] Implement GET endpoints (list and single)
- [ ] Build PUT/PATCH for updates
- [ ] Add DELETE functionality
- [ ] Handle validation and errors

**Sections:**
1. Introduction to CRUD
2. Create Endpoint (POST)
3. Read Endpoints (GET)
4. Update Endpoint (PUT)
5. Delete Endpoint (DELETE)
6. Error Handling
7. Testing Endpoints
8. Conclusion

**Length:** ~2000 words
**Time:** 30 minutes reading + 1 hour practice
**Prerequisites:** Article 2 (Express Setup)
**Next:** Article 4 (Authentication) or Article 5 (Database)
```

## Phase 3: Series Metadata

### Step 3.1: Create Series Metadata Document

**Create a series index file:**
```markdown
# Building REST APIs with Node.js - Series Overview

## Series Information
- **Total Articles:** 6
- **Skill Level:** Intermediate
- **Estimated Time:** 8-10 hours (reading + practice)
- **Last Updated:** 2025-11-19

## Prerequisites
- JavaScript ES6+
- Basic HTTP understanding
- Node.js installed (v18+)

## Articles

### 1. REST API Fundamentals
**Status:** Published
**File:** `01-rest-api-fundamentals.md`
**Topics:** REST principles, HTTP methods, API design
**Reading Time:** 20 minutes

### 2. Setting Up Express Server
**Status:** Published
**File:** `02-express-server-setup.md`
**Topics:** Project setup, routing, middleware
**Reading Time:** 25 minutes
**Prerequisites:** Article 1

... (continue for all articles)
```

### Step 3.2: Define Series Metadata Structure

**In each article's metadata:**
```yaml
article:
  title: "Building CRUD Endpoints"
  series:
    name: "Building REST APIs with Node.js"
    position: 3
    total: 6
    prerequisites:
      - "02-express-server-setup.md"
    next_article: "04-authentication-security.md"
    previous_article: "02-express-server-setup.md"
```

## Phase 4: Content Creation

### Step 4.1: Create in Order

**Start with Article 1 and proceed sequentially:**

**Why sequential?**
- Ensures prerequisites covered
- Maintains terminology consistency
- Allows refinement of later articles
- Catches scope issues early

**Process per article:**
1. Use `/article-writing` prompt
2. Include series context in prompt
3. Reference previous articles
4. Create metadata with series info
5. Complete draft
6. Run validations
7. Move to next article

### Step 4.2: Maintain Consistency

**Across all articles in series:**

**Terminology:**
- Use same terms for same concepts
- Define abbreviations consistently
- Reference terms from earlier articles

**Style:**
- Similar tone and voice
- Consistent formatting
- Same code style
- Uniform example patterns

**Structure:**
- Similar section organization
- Consistent heading levels
- Comparable length (within reason)

**Examples:**
- Build on previous examples
- Progressive complexity
- Same example project/scenario

### Step 4.3: Cross-Reference

**In each article:**

**Introduction:**
```markdown
This is Article 3 in the [Building REST APIs with Node.js](./series-index.md) series.

**Prerequisites:** Complete [Article 2: Express Server Setup](./02-express-server-setup.md) first.

**In this article**, you'll learn how to build CRUD endpoints...
```

**Conclusion:**
```markdown
## Next Steps

Continue to [Article 4: Authentication & Security](./04-authentication-security.md) 
to learn how to secure your API endpoints.

**Related in this series:**
- [Article 1: REST API Fundamentals](./01-rest-api-fundamentals.md)
- [Article 2: Express Server Setup](./02-express-server-setup.md)
```

**Body references:**
```markdown
As we learned in [Article 2](./02-express-server-setup.md), middleware 
functions have access to the request and response objects...
```

## Phase 5: Series Validation

### Step 5.1: Complete All Articles First

**Before final validation:**
- [ ] All articles drafted
- [ ] All metadata created
- [ ] Individual validations run
- [ ] Cross-references added

### Step 5.2: Run Series Validation

**Use the series validation prompt:**
```
/series-validation
```

**Checks:**
- Logical progression
- Terminology consistency
- Non-redundancy
- Cross-reference accuracy
- Coverage completeness
- Style consistency

**Address issues:**
1. Review validation report
2. Fix inconsistencies
3. Fill gaps
4. Remove redundancy
5. Update cross-references
6. Re-run validation

### Step 5.3: Test Learning Path

**Manual walkthrough:**
1. Read articles in sequence (as if you're the reader)
2. Note any jumps in difficulty
3. Verify prerequisites are sufficient
4. Check that examples build logically
5. Ensure no missing steps

**Ask yourself:**
- Can a reader follow the path?
- Are concepts introduced in the right order?
- Do examples make sense in context?
- Is anything assumed but not explained?

## Phase 6: Navigation and Publishing

### Step 6.1: Create Series Index

**Create series landing page:**
```markdown
# Building REST APIs with Node.js

[Introduction explaining the series]

## What You'll Learn
[Overall learning objectives]

## Prerequisites
[What readers need to know]

## Articles

1. **[REST API Fundamentals](./01-rest-api-fundamentals.md)** - 20 min
   Learn REST principles, HTTP methods, and API design basics

2. **[Setting Up Express Server](./02-express-server-setup.md)** - 25 min
   Set up your Node.js project and create your first Express server

... [Continue for all articles]

## Estimated Time
8-10 hours (reading + hands-on practice)

## Source Code
[Link to example repository with completed code]
```

### Step 6.2: Add Series Navigation

**In each article, add navigation bar:**

```markdown
---
📚 **Series:** Building REST APIs with Node.js (Article 3 of 6)

[← Previous: Express Server Setup](./02-express-server-setup.md) | 
[Series Index](./series-index.md) | 
[Next: Authentication & Security →](./04-authentication-security.md)

---
```

### Step 6.3: Update Cross-References

**In existing articles, add series references:**
- Related articles section
- "See also" links
- Prerequisites mentions

### Step 6.4: Publish Series

**Publish in order:**
1. Publish Article 1 first
2. Wait for any reader feedback
3. Address issues before publishing more
4. Publish remaining articles sequentially
5. Update series index as you go

## Series Maintenance

### Ongoing Updates

**When updating any article in series:**
1. Check impact on dependent articles
2. Update cross-references if needed
3. Re-run series validation
4. Update series index (last updated date)
5. Consider version incrementing for series

**Series metadata tracking:**
```yaml
series_validation:
  last_run: "2025-11-19"
  consistency_score: 9
  progression_score: 10
  notes: "All articles consistent and well-linked"
```

### Adding Articles to Series

**To expand a series:**
1. Update series metadata (total count)
2. Determine insertion point
3. Update navigation (previous/next links)
4. Run series validation
5. Update series index

## Common Patterns

### Pattern 1: Foundational Series
```
Article 1: Concepts and Theory
Article 2: Getting Started (Practical)
Article 3: Core Features
Article 4: Advanced Techniques
Article 5: Best Practices
```

### Pattern 2: Tutorial Series
```
Article 1: Introduction & Setup
Article 2: Build Basic Feature
Article 3: Add Functionality
Article 4: Improve & Refine
Article 5: Deploy & Maintain
```

### Pattern 3: Deep Dive Series
```
Article 1: Overview & Basics
Article 2-N: Deep dive into each major aspect
Article N+1: Integration & Advanced Topics
```

## Tips for Successful Series

### Do:
✅ Plan the entire series before writing
✅ Maintain consistent terminology
✅ Build examples progressively
✅ Add clear prerequisites
✅ Link articles together
✅ Create series index
✅ Run series validation

### Don't:
❌ Skip articles out of order
❌ Repeat content (link instead)
❌ Make articles too dependent (some standalone value)
❌ Ignore reader feedback mid-series
❌ Forget to update cross-references

---

*Use this workflow to create cohesive, valuable article series that guide readers through complex topics.*
