# Use of AI in the Project

This document describes how the group used artificial intelligence (AI) as a support tool throughout the project. AI was used to assist the group, while the group members evaluated the suggestions, rewrote content where necessary, and tested the solution themselves.

## Tools

- Microsoft Copilot for brainstorming, explanations, and troubleshooting.
- GitHub Copilot for code suggestions while writing C#, Razor, SQL/EF Core, and Markdown.
- A ChatGPT-like AI assistant for planning, documentation, and questions about ASP.NET Core, .NET Aspire, MariaDB, and Docker.

## Areas of Use

### Design and Planning Phase

AI was used to discuss possible solutions before implementation began. This included using AI to:

- Compare ASP.NET Core MVC with other possible approaches and explain why MVC was suitable for forms, controllers, and Razor Views.
- Suggest a layered architecture consisting of Controller, ViewModel, Service, Mapper, Repository, Unit of Work, and database.
- Discuss the entities and relationships required for `Need`, `Resource`, `Category`, `Attachment`, `Match`, and `User`.
- Suggest approaches for creating an ER diagram and an architecture diagram.
- Plan the request flow from the browser through the controller and service layers to MariaDB.

AI suggestions served as a starting point for discussion. The group independently decided which suggestions were appropriate for the assignment and the actual codebase.

### Coding Phase

AI was used to help understand and implement parts of the application, including:

- Basic structures for MVC controllers with GET and POST actions.
- ViewModels with validation and form field binding.
- Razor Views, HTML, and CSS for forms and responsive design.
- EF Core entities, relationships, enum conversions, and the repository pattern.
- .NET Aspire configuration for Web, Migrator, and MariaDB.
- `dotnet user-secrets` for storing database passwords that should not be committed to GitHub.
- Explanations of error messages and troubleshooting suggestions for build and startup issues.
- Suggestions for unit tests covering the service and mapper layers.

Code suggestions were not accepted as authoritative. The group adapted them to the project's class names, existing architecture, and requirements, and checked that the solution built and worked with the rest of the project.

### Documentation Phase

AI was used to:

- Structure the README and the documentation in `docs/`.
- Explain the architecture, request flow, and database connection in simpler language.
- Improve wording, headings, and Markdown tables.
- Generate draft diagrams in Mermaid format.
- Identify documentation details that needed to be checked against the actual code.

The group reviewed and corrected the documentation. Test results in the README must be filled in after the application has been tested.

## Specific Examples of Prompts

The examples below illustrate the types of questions used during development and how AI supported the work. They document the working process and do not imply that AI independently implemented the functionality.

### Example 1 – Architecture

> **Prompt:** We are building an ASP.NET Core MVC application with Need and Resource. How can we divide the system into Controller, ViewModel, Service, Mapper, Repository, Unit of Work, and MariaDB? Also explain the data flow for a POST request.
>
> **Result:** AI suggested a layered request flow. The group used this as a starting point and adapted it to the actual classes: `NeedController`, `NeedService`, `NeedMapper`, `NeedRepository`, and `AppDbContext`.

### Example 2 – .NET Aspire and Secrets

> **Prompt:** How can a .NET Aspire AppHost use a database password without storing it in `appsettings.json` or pushing it to GitHub?
>
> **Result:** AI suggested using an Aspire parameter as a secret and using `dotnet user-secrets`. The group used this as a basis for the configuration and documented the command in the README.

### Example 3 – GET and POST

> **Prompt:** Show an example of an ASP.NET Core MVC controller where GET displays a form, POST validates a ViewModel, and invalid data causes the form to be displayed again with validation errors.
>
> **Result:** AI explained the difference between GET and POST, `ModelState.IsValid`, and redirecting after successful persistence. The group adapted the pattern to the Need and Resource controllers and the existing ViewModels.

### Example 4 – EF Core and Database

> **Prompt:** How can EF Core store enums as readable text values in a MariaDB database, and how can a unique combination of `NeedId` and `ResourceId` be enforced?
>
> **Result:** AI suggested `HasConversion<string>()` and a unique index. The group checked the suggestions against `AppDbContext` and used them where appropriate for the data model.

### Example 5 – Documentation and Diagrams

> **Prompt:** Create a simple Mermaid architecture diagram for the browser, ASP.NET Core MVC, Migrator, and MariaDB, and show which components AppHost starts.
>
> **Result:** AI generated an initial diagram. The group removed generic or misleading elements and kept a diagram describing the project's actual AppHost, Web, Migrator, and MariaDB components.

## What AI Did Not Do

- AI did not independently make decisions about requirements, architecture, or which features to deliver.
- AI did not have access to the group's local runtime, database, or development environment and therefore could not confirm that its suggestions worked.
- AI did not produce the final solution without review. The group integrated, adapted, and quality-checked the code.
- AI did not perform the final testing. The group must run `dotnet test`, start the application, and carry out manual tests itself.
- AI did not generate test results. Results must only be documented after the tests have actually been run.
- AI was not used to commit secrets, passwords, or personal information to the repository.
- AI did not replace code review or collaboration within the group.

## How Much Was Used Directly Versus Adapted

No exact percentage is available for the project as a whole. In general, explanations, ideas, and documentation drafts were frequently rewritten and checked against the code. Code suggestions were used as starting points but adapted to the project's own names, models, validation, error handling, and structure. Nothing was considered complete simply because AI had suggested it; the group had to understand, build, and test the changes itself.

## Summary

AI was used for planning, learning, documentation, troubleshooting, and inspiration. Its main purpose was to help the group obtain suggestions and explanations more efficiently, not to transfer responsibility for the solution to AI. The group made the architectural decisions, adapted the implementation, evaluated security, and carried out quality assurance. AI was a support tool, while responsibility for the code and content remained with the group.
