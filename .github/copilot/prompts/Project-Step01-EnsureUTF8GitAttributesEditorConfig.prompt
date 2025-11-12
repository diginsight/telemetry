# 🔧 Project-Step01-EnsureUTF8GitAttributesEditorConfig

## 📋 **Prompt Overview**

This prompt validates and ensures proper UTF-8 encoding preservation across a Git repository by implementing comprehensive **Git configuration**, **.gitattributes**, and **.editorconfig** files. It prevents Unicode character corruption while maintaining Windows-compatible line endings (CRLF) and ensuring consistent encoding across development environments.

**⚠️ IMPORTANT: This prompt preserves existing Git LFS configuration and only adds UTF-8 encoding protections for source code files.**

## 🎯 **Prompt Goal**

### **Core Objectives:**
- **📊 Preserve Unicode Characters:** Prevent corruption of Unicode symbols (📊, ➕, 🔄, ❌, ⚠️, 🗑️) in source code
- **💻 Windows Compatibility:** Maintain CRLF line endings for Windows development environments
- **🌐 Cross-Platform Safety:** Ensure proper handling for team members on different operating systems
- **🎨 IDE Consistency:** Configure editors to use UTF-8 encoding by default
- **🔮 Future-Proof:** Establish configuration that automatically handles new files correctly
- **🗂️ LFS Preservation:** Maintain existing Git LFS configuration for large binary files

### **Implementation Targets:**

**⚙️ Configure for ALL projects:**
- Multi-target projects (.NET 6, .NET 7, .NET 8, .NET 9, .NET Standard 2.0, .NET Standard 2.1)
- C# source files (*.cs)
- Project files (*.csproj, *.sln, *.props, *.targets)
- Configuration files (*.json, *.xml, *.config)
- Documentation files (*.md, *.txt)

**🔧 Special handling for:**
- Binary files (images, executables, libraries) - **PRESERVE EXISTING LFS RULES**
- Shell scripts (Unix LF line endings)
- Cross-platform files requiring specific line endings

## 📤 **Expected Output**

### **1. Git Configuration Commands:**
```bash
git config core.autocrlf false
git config core.safecrlf warn
```
### **2. .gitattributes File Strategy:**

**🚨 CRITICAL: Do NOT remove or modify existing Git LFS rules!**

The .gitattributes file should be updated by:
1. **PRESERVING all existing `filter=lfs diff=lfs merge=lfs -text` rules**
2. **Adding UTF-8 source code protections**
3. **Only adding `binary` rules for files NOT already managed by LFS**

**Template structure:**

```gitattributes
# Auto detect text files and perform LF normalization
* text=auto

# Explicitly define line endings for specific file types
*.cs text eol=crlf
*.csproj text eol=crlf
*.sln text eol=crlf
*.props text eol=crlf
*.targets text eol=crlf
*.json text eol=crlf
*.xml text eol=crlf
*.config text eol=crlf
*.md text eol=crlf
*.txt text eol=crlf
*.yml text eol=crlf
*.yaml text eol=crlf

# Ensure shell scripts use LF (for cross-platform compatibility)
*.sh text eol=lf
*.bash text eol=lf

# Ensure PowerShell files use CRLF (Windows standard)
*.ps1 text eol=crlf
*.psm1 text eol=crlf

# Batch files must use CRLF
*.bat text eol=crlf
*.cmd text eol=crlf

# PRESERVE EXISTING GIT LFS RULES - DO NOT MODIFY THESE
# (Keep all existing filter=lfs diff=lfs merge=lfs -text rules)

# Only add 'binary' rules for files NOT managed by LFS
*.gif binary
*.so binary
*.dylib binary
```
### **3. .editorconfig File:**

```ini
root = true

# All files
[*]
indent_style = space
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
end_of_line = crlf

# XML project files
[*.{csproj,vbproj,vcxproj,vcxproj.filters,proj,projitems,shproj}]
indent_size = 2
end_of_line = crlf

# XML build files
[*.{xml,config,props,targets,nuspec,resx,ruleset,vsixmanifest,vsct}]
indent_size = 2
end_of_line = crlf

# JSON files
[*.{json,json5,webmanifest}]
indent_size = 2
end_of_line = crlf

# YAML files
[*.{yml,yaml}]
indent_size = 2
end_of_line = crlf

# Markdown
[*.md]
end_of_line = crlf
trim_trailing_whitespace = false

# Web Files
[*.{htm,html,js,jsm,ts,tsx,css,sass,scss,less,svg,vue}]
indent_size = 2
end_of_line = crlf

# Batch files
[*.{cmd,bat}]
end_of_line = crlf

# Bash files
[*.sh]
end_of_line = lf

# PowerShell files
[*.{ps1,psm1,psd1}]
end_of_line = crlf
```
## 📖 **Implementation Guidelines**

### **Git Configuration Explanation:**

**1. `core.autocrlf = false`:**
- **Purpose:** Disables automatic line ending conversion that can corrupt Unicode characters
- **Why:** Git's line ending conversion can misinterpret multi-byte Unicode sequences
- **Result:** Prevents Unicode icons from being replaced with "??" characters

**2. `core.safecrlf = warn`:**
- **Purpose:** Warns about potential line ending inconsistencies
- **Why:** Provides visibility into line ending issues without blocking operations
- **Result:** Early detection of encoding problems

### **.gitattributes Configuration Strategy:**

**🚨 CRITICAL IMPLEMENTATION RULES:**

1. **NEVER remove existing Git LFS rules** - These are expensive to recreate
2. **READ the current .gitattributes file first** to identify existing LFS patterns
3. **ONLY add UTF-8 source code protections**
4. **AVOID duplicate or conflicting rules**

**Text File Handling:**

```gitattributes
* text=auto                    # Git auto-detects text files
*.cs text eol=crlf            # Force CRLF for C# files
*.json text eol=crlf          # Force CRLF for JSON files
```

**Binary File Protection (LFS-Aware):**

```gitattributes
# PRESERVE existing LFS rules like:
*.png filter=lfs diff=lfs merge=lfs -text

# ONLY add binary for non-LFS files:
*.gif binary                  # Small files not needing LFS
```

**Cross-Platform Compatibility:**

```gitattributes
*.sh text eol=lf             # Unix shell scripts need LF
*.bat text eol=crlf          # Windows batch files need CRLF
```
### **.editorconfig Standards:**

**Global Settings:**

```ini
[*]
charset = utf-8              # Force UTF-8 for all files
trim_trailing_whitespace = true
insert_final_newline = true
```

**Language-Specific:**

```ini
[*.cs]
indent_size = 4              # C# standard
end_of_line = crlf           # Windows compatibility

[*.json]
indent_size = 2              # JSON standard
end_of_line = crlf           # Consistency
```
## 🔄 **Implementation Steps**

### **Step 1: Validate Current State**

1. **Check Git Configuration:**

```bash
git config --get core.autocrlf
git config --get core.safecrlf
```

2. **Analyze Existing .gitattributes:**

```bash
# READ the current file first
cat .gitattributes
# Look for existing LFS patterns
grep "filter=lfs" .gitattributes
```

3. **Check for Existing Files:**

```bash
ls -la .gitattributes .editorconfig
```

4. **Scan for Unicode Characters:**

```bash
grep -r "📊\|➕\|🔄\|❌\|⚠️\|🗑️" --include="*.cs" .
```
### **Step 2: Configure Git Settings**

1. **Set Repository Configuration:**

```bash
git config core.autocrlf false
git config core.safecrlf warn
```

2. **Verify Configuration:**

```bash
git config --list | grep -E "(autocrlf|safecrlf)"
```
### **Step 3: Update Configuration Files**
1. **Update .gitattributes** - PRESERVE existing LFS rules, ADD source code protections
2. **Update .editorconfig** - Ensure UTF-8 charset is specified
3. **Validate file encoding is UTF-8**

### **Step 4: Test and Commit**

1. **Stage Configuration Files:**

```bash
git add .gitattributes .editorconfig
```

2. **Commit Changes:**

```bash
git commit -m "Configure Git for Unicode preservation while preserving LFS

- Set core.autocrlf=false to prevent Unicode corruption
- Configure CRLF line endings for Windows-specific files
- Ensure UTF-8 encoding is preserved for all text files
- Preserve existing Git LFS configuration for large binary files
- This prevents Unicode icons from being corrupted during Git operations"
```

3. **Verify Unicode Preservation:**

```bash
git show HEAD --name-only
grep -r "📊" --include="*.cs" . | head -n 5
```
## 🚀 **Common Scenarios**

### **Scenario 1: New Repository Setup**

```bash
# Initial setup
git init
git config core.autocrlf false
git config core.safecrlf warn

# Create configuration files
# (Use templates above)

# Commit configuration
git add .gitattributes .editorconfig
git commit -m "Initial Git and editor configuration for UTF-8 preservation"
```
### **Scenario 2: Existing Repository with LFS**

```bash
# Fix Git configuration
git config core.autocrlf false
git config core.safecrlf warn

# READ existing .gitattributes first
cat .gitattributes
grep "filter=lfs" .gitattributes

# Update configuration files (PRESERVE LFS rules)
# Add UTF-8 protections without removing LFS

# Commit changes
git add .gitattributes .editorconfig
git commit -m "Add UTF-8 encoding protections while preserving Git LFS configuration"
```
### **Scenario 3: Team Environment Setup**

```bash
# Global Git configuration for all team members
git config --global core.autocrlf false
git config --global core.safecrlf warn

# Ensure IDE settings
# Visual Studio: Tools > Options > Environment > Documents > "Save as UTF-8"
# VS Code: "files.encoding": "utf8" in settings.json
```
## ✅ **Validation Checks**

### **Configuration Validation:**

```bash
# Check Git settings
git config --get core.autocrlf    # Should return: false
git config --get core.safecrlf     # Should return: warn

# Check file existence
test -f .gitattributes && echo "✅ .gitattributes exists" || echo "❌ .gitattributes missing"
test -f .editorconfig && echo "✅ .editorconfig exists" || echo "❌ .editorconfig missing"

# Verify LFS rules are preserved
grep "filter=lfs" .gitattributes | wc -l  # Should show existing LFS rule count
```
### **Unicode Character Validation:**

```bash
# Find files with Unicode characters
grep -r "📊\|➕\|🔄\|❌\|⚠️\|🗑️" --include="*.cs" . | head -n 10

# Check for corrupted characters
grep -r "?" --include="*.cs" . | grep -E "(logger\.Log|Console\.Write)" | head -n 5
```
### **Line Ending Validation:**

```bash
# Check for consistent line endings in C# files
find . -name "*.cs" -exec file {} \; | grep -v CRLF | head -n 5

# Check for mixed line endings
git ls-files | xargs file | grep "with CR"
```
## 🚫 **Common Pitfalls to Avoid**

### **❌ Don't Remove Git LFS Rules**

```gitattributes
# ❌ DON'T DO THIS - This breaks LFS
*.png binary     # This replaces LFS rule

# ✅ PRESERVE LFS RULES
*.png filter=lfs diff=lfs merge=lfs -text  # Keep existing LFS
```
### **❌ Don't Set core.autocrlf=true**

```bash
# ❌ This causes Unicode corruption
git config core.autocrlf true

# ✅ Use this instead
git config core.autocrlf false
```
### **❌ Don't Create Conflicting Rules**

```gitattributes
# ❌ Conflicting configuration
*.dll filter=lfs diff=lfs merge=lfs -text
*.dll binary    # This conflicts with LFS

# ✅ Consistent approach - pick ONE
*.dll filter=lfs diff=lfs merge=lfs -text  # For large DLLs
# OR
*.dll binary    # For small DLLs
```
### **❌ Don't Forget EditorConfig Charset**

```ini
# ❌ Missing charset specification
[*]
indent_style = space

# ✅ Always specify UTF-8
[*]
charset = utf-8
indent_style = space
```
## ✅ **Checklist**

### **Git Configuration:**
- [ ] **Core Settings:** `core.autocrlf = false` configured
- [ ] **Safety Settings:** `core.safecrlf = warn` configured
- [ ] **Verification:** Git configuration validated with `git config --list`

### **File Configuration:**
- [ ] **GitAttributes:** `.gitattributes` file updated (not replaced)
- [ ] **LFS Preservation:** All existing `filter=lfs` rules maintained
- [ ] **EditorConfig:** `.editorconfig` file created/updated
- [ ] **File Encoding:** Both files saved as UTF-8
- [ ] **No Conflicts:** No duplicate or conflicting attribute rules

### **Content Validation:**
- [ ] **Text Files:** All development files (.cs, .csproj, .json) configured for CRLF
- [ ] **Binary Files:** Large files use LFS, small files use binary
- [ ] **Cross-Platform:** Shell scripts (.sh) configured for LF
- [ ] **Windows Files:** Batch files (.bat, .cmd) configured for CRLF

### **Unicode Preservation:**
- [ ] **Character Test:** Unicode characters (📊, ➕, 🔄, ❌, ⚠️, 🗑️) display correctly
- [ ] **No Corruption:** No "??" characters in place of Unicode symbols
- [ ] **Editor Support:** IDE configured to use UTF-8 encoding
- [ ] **Team Setup:** Documentation provided for team member setup

### **LFS Validation:**
- [ ] **LFS Rules:** All existing LFS rules preserved
- [ ] **No Conflicts:** No binary rules for LFS-managed files
- [ ] **LFS Function:** Git LFS continues to work for large files
- [ ] **Repository Size:** Repository stays lean with LFS handling large files

### **Project Compatibility:**
- [ ] **Multi-Target:** Configuration works for .NET 6, 7, 8, 9, Standard 2.0, 2.1
- [ ] **Build Success:** All projects compile without encoding errors
- [ ] **No Regression:** Existing functionality unaffected
- [ ] **Future-Proof:** New files automatically follow correct encoding

## 🏆 **Success Criteria**

✅ **Complete Success:**
- Unicode characters preserved across all Git operations
- CRLF line endings maintained for Windows compatibility
- UTF-8 encoding enforced for all text files
- **Git LFS configuration fully preserved and functional**
- Configuration committed and documented for team use
- All multi-target projects (.NET 6-9, .NET Standard) function correctly

✅ **Technical Success:**
- Git configuration prevents Unicode corruption
- .gitattributes handles line endings appropriately
- .editorconfig ensures consistent encoding across IDEs
- **Binary files protected without disrupting LFS workflow**
- No conflicting or duplicate attribute rules

✅ **Team Success:**
- Documentation provided for team member onboarding
- Consistent development environment across all machines
- Future Unicode characters automatically preserved
- **Git LFS continues to optimize repository performance**
- No performance impact on development workflow

This prompt ensures comprehensive, reliable UTF-8 encoding preservation while maintaining Windows development compatibility and **preserving existing Git LFS infrastructure** across the entire Diginsight repository ecosystem.