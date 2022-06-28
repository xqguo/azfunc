# An Azure func to compute chinese calendar

A query string ?date=2000 can be used to query the full year, 200001 for the full month, or the full datetime string for the specific time.

## Install template

Templates can be used to create new func project and triggers. Remember to change the Update tag in project file to Include.

```
dotnet new --install Microsoft.Azure.WebJobs.ProjectTemplates
dotnet new --install Microsoft.Azure.WebJobs.ItemTemplates   
dotnet new func --language F# --name ChineseCal
dotnet new http --language F# --name HttpTrigger
```

## Deploy command 

For compiled .net project, use csharp option to publish.

```
func azure functionapp publish chinesecal --force --csharp
```
## Test command 

Also, specify lang as csharp.

```
func start --csharp --script-root bin\\debug\\net6.0\\publish 
```