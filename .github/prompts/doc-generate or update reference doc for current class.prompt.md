---
name: doc-generate-or-update-class-reference-doc
description: Generate or Update Class Reference Documentation
agent: agent
model: claude-sonnet-4.5
tools: ['codebase', 'fetch']
argument-hint: 'class="ClassName"'
---

# Generate or Update Class Reference Documentation

## Goal
Generate or update a comprehensive class reference markdown document using the structure defined in the file `Class Reference Template.md`.
The reference document should be created or updated in the folder `/src/docs/03. Reference` and named as `XX.YY - [ClassName].md`.

## Instructions

### 1. Identify Current Class
Analyze the current context to identify:
- **ClassName**: The name of the class currently being viewed or discussed
- **Namespace**: The full namespace of the class
- **FilePath**: The source file path containing the class
- **Component**: The project/assembly containing the class (e.g., "Diginsight.Components", "Diginsight.Components.Azure")
- **CategoryPrefix**: A two-digit category number based on the component area (e.g., "05" for Azure, "06" for Services, "10" for Utilities)
- **DocumentNumber**: A sequential number within the category (e.g., "01", "02")

### 2. Search for Existing Documentation
Search for existing reference documentation in the folder:
`/src/docs/03. Reference`

Look for files matching patterns:
- `**/*.md` files containing the class name
- `XX.YY - [ClassName].md` format
- Similar class names or related documentation

Determine if documentation:
- ✅ **Exists**: Documentation file found → Proceed to verification
- ❌ **Missing**: No documentation found → Proceed to creation

### 3. Read and Understand Template Structure
Read the template file located at:
`.github/copilot/templates/Class Reference Template.md`

**Critical:** This template defines the complete structure, sections, and content format for class reference documentation. You MUST:
- **Read the entire template** to understand all required sections
- **Identify each section** and its purpose (Overview, Configuration, Usage, etc.)
- **Understand content requirements** for each section (what information to include)
- **Note formatting conventions** (emojis, tables, code blocks, callouts)
- **Follow the structure exactly** when creating or updating documentation
- **Use the template as a blueprint** - do not deviate from its section organization

The template serves as the authoritative source for:
- Section names and hierarchy
- Content types expected in each section
- Markdown formatting patterns
- Code example formats
- Table structures
- Navigation elements (Table of Contents, anchors)

### 4. Analyze Class Source Code
Read and analyze the class source code to extract:

#### Class Structure
- **Class declaration** (public/internal, static, abstract, sealed, generic parameters)
- **Inheritance hierarchy** (base classes and interfaces)
- **Namespace** and assembly information
- **XML documentation comments** on class and members

#### Members Inventory
- **Properties**: Name, type, accessibility, purpose
- **Methods**: Signature, parameters, return type, purpose
- **Constructors**: Parameters and initialization logic
- **Fields**: Public constants and important fields
- **Events**: Event declarations and handlers
- **Nested Types**: Inner classes, enums, delegates

#### Functionality Analysis
- **Primary purpose** and use cases
- **Key features** and capabilities
- **Dependencies** on other classes or external libraries
- **Configuration options** (if applicable)
- **Extension methods** (if applicable)
- **Design patterns** used (factory, builder, repository, etc.)

### 5. Create or Update Documentation

#### If Creating New Documentation:
Create a new reference document in:
`/src/docs/03. Reference`

Name the document as:
`<CategoryPrefix>.<DocumentNumber> - <ClassName>.md`

Fill ALL sections from the template with content derived from the class analysis.

#### If Updating Existing Documentation:
1. **Read existing documentation** completely
2. **Compare with current class code** for:
   - ✅ Accuracy of descriptions
   - ✅ Completeness of member documentation
   - ✅ Validity of code examples
   - ✅ Currency of configuration options
   - ✅ Correctness of usage patterns
3. **Identify gaps and inconsistencies**:
   - Missing new methods or properties
   - Outdated signatures or parameters
   - Incorrect behavior descriptions
   - Deprecated features still documented
   - Missing or incorrect examples
4. **Update documentation** to align with current code

### 6. Generate Content Using Template Structure

**Critical:** Use the `Class Reference Template.md` as your authoritative guide for:

#### Section Organization
- Follow the **exact section hierarchy** defined in the template
- Include **all sections** present in the template (do not skip any)
- Use the **same section names and emojis** as shown in the template
- Maintain the **same order** of sections as in the template

#### Content Population
For each section in the template:
1. **Identify the section's purpose** from the template's structure and examples
2. **Extract relevant information** from the class source code analysis
3. **Format content** following the template's patterns (tables, code blocks, lists)
4. **Include all subsections** shown in the template
5. **Use placeholder text from template** as a guide for content type and depth

#### Content Extraction Guidelines
Based on class analysis, populate template sections with:
- **YAML front matter**: Class name, purpose, relevant categories
- **Overview sections**: Purpose, features, capabilities from class XML comments and implementation
- **Configuration sections**: Constructor parameters, dependency injection patterns, settings
- **Usage sections**: Practical examples derived from class methods and common patterns
- **API Reference sections**: Complete method signatures, parameters, return types from source
- **Troubleshooting sections**: Exception types thrown, validation requirements, common mistakes
- **Best Practices sections**: Design patterns used, performance considerations, recommended approaches

#### Formatting Compliance
- Match **emoji usage** from template exactly
- Use **same table formats** for parameters, properties, exceptions
- Follow **code block formatting** patterns from template examples
- Apply **callout styles** (notes, warnings, tips) as shown in template
- Maintain **markdown syntax consistency** with template

### 7. Verification Checklist
Ensure the generated or updated document:
- ✅ Matches the template structure exactly (all sections present in same order)
- ✅ Uses the same section names and emojis as the template
- ✅ Includes complete YAML front matter following template format
- ✅ Contains Table of Contents with proper anchor links (if in template)
- ✅ Documents ALL public members (methods, properties, constructors) in API reference sections
- ✅ Includes working code examples following template's example patterns
- ✅ Uses consistent markdown formatting matching the template
- ✅ Contains accurate type information and signatures
- ✅ Reflects current class implementation (not outdated)
- ✅ Includes proper cross-references to related documentation (if template shows this pattern)
- ✅ Has clear, professional technical descriptions
- ✅ Contains troubleshooting guidance (if template includes this section)
- ✅ Includes best practices and recommendations (if template includes this section)
- ✅ Follows all table formats, code block styles, and callout patterns from template

### 8. Quality Assurance

#### For New Documentation:
- Verify all sections are populated with meaningful content
- Ensure code examples compile and run correctly
- Validate all member signatures match the source code
- Check that examples cover the most common use cases

#### For Updated Documentation:
- Confirm all changes align with current class implementation
- Verify no obsolete information remains
- Ensure new features are documented
- Validate that existing examples still work correctly
- Check that deprecated members are marked appropriately

### 9. Final Output
After creating or updating the documentation:
- **Report location**: Provide the full path to the documentation file
- **Summary of changes**: 
  - For new docs: List major sections created
  - For updates: List specific changes made (added methods, corrected examples, etc.)
- **Validation status**: Confirm the document passes the verification checklist
- **Next steps**: Suggest any follow-up documentation needs (related classes, tutorials, etc.)
