# AI Instructions

## Table of Contents
- [Structure of the documentation](#structure-of-the-documentation)
- [Overview section for References pages](#overview-section-for-references-pages)

## Structure of the documentation
**documentation source** is into folder **src/doc**. 

It is divided into:

- '00. Getting Started'
- '01. Concepts'
- '02. Advanced'
- '03. Reference'
- '05. About'

## Structure of a reference page
class reference pages should include the following mandatory sections:

- 📋 **Overview**
- 🔍 **Additional Details**
- ⚙️ **Configuration**
- 🔧 **Troubleshooting**
- 📚 **Reference**
- 📖 (optional) **Appendices**

(first level sections should have icons representing their content).

### Overview section 
Overview section for reference pages should:

- be readable and easy to understand
- be concise and to the point
- explain what the component does

``` # QueryCostMetricRecorder Overview
example:
The `QueryCostMetricRecorder` captures and records **CosmosDB query costs** as the <mark>**diginsight.query_cost**</mark> OpenTelemetry metric.
`QueryCostMetricRecorder` is part of the **Observable extensions for CosmosDB** that provide observability into database that are part of **Diginsight.Components.Azure**.
`QueryCostMetricRecorder` tracks **Request Units (RU) consumption** across your application's database operations.
``` 
### Configuration section 
Overview section for reference pages should:
- explain how to configure the component in appsettings.json
  settings (default:): description
- (optional) possible values
 
- explain how to configure the component into the startup sequence 
- 
