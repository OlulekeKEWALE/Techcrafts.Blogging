# Techcrafts Blogging

A modern, full-stack blogging platform built with **.NET 10** following **Clean Architecture** principles. The application is designed to be highly scalable, observable, and feature-rich, integrating AI capabilities and modern authentication.

## Features

- **Clean Architecture:** Organized into `Domain`, `Application`, `Infrastructure`, and `Web` layers for maintainability and separation of concerns.
- **CQRS Pattern:** Implemented using `MediatR` to cleanly separate read and write operations.
- **Rich Text & Markdown:** Support for parsing and rendering markdown content using `Markdig`.
- **Identity & Authentication:**
  - Standard ASP.NET Core Identity.
  - Social login integration via **Google OAuth** and **GitHub OAuth**.
- **AI Integration:** Powered by `OpenAI` to provide intelligent content assistance and generation features.
- **Observability & Telemetry:** Full integration with **OpenTelemetry** for metrics, distributed tracing, and logging (OTLP & Console Exporters).
- **Structured Logging:** Powered by **Serilog**.
- **Data Access:** Entity Framework Core (SQL Server) for reliable and scalable data persistence.
- **Caching:** Utilizes `.NET` Hybrid Caching (`Microsoft.Extensions.Caching.Hybrid`) for high-performance data retrieval.

## Project Structure

The solution follows a standard Clean Architecture layout:

- **`Techcrafts.Blogging.Domain`**: Contains the core enterprise logic, entities, interfaces, and domain exceptions. Has no external dependencies.
- **`Techcrafts.Blogging.Application`**: Contains the business logic, use cases, MediatR handlers, and validation rules (`FluentValidation`).
- **`Techcrafts.Blogging.Infrastructure`**: Contains implementation details for external concerns, including Entity Framework Core DbContexts, SQL Server configurations, Identity implementations, and external API wrappers (e.g., OpenAI).
- **`Techcrafts.Blogging.SharedKernel`**: Contains shared types, MediatR contracts, and utilities used across the solution.
- **`Techcrafts.Blogging.Web`**: The presentation layer (ASP.NET Core application) handling HTTP requests, dependency injection setup, middleware, and observability configurations.

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB or an external instance)

### Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone <your-repo-url>
   cd Techcrafts.Blogging
   ```

2. **Restore Dependencies:**
   This project uses Central Package Management (`Directory.Packages.props`).
   ```bash
   dotnet restore
   ```

3. **Configure User Secrets / App Settings:**
   Update your `appsettings.Development.json` or use `dotnet user-secrets` to configure your connection strings and external API keys:
   - Database Connection String (`DefaultConnection`)
   - OpenAI API Key
   - Google OAuth Client ID & Secret
   - GitHub OAuth Client ID & Secret

4. **Apply Database Migrations:**
   ```bash
   dotnet ef database update --project Techcrafts.Blogging.Infrastructure --startup-project Techcrafts.Blogging.Web
   ```

5. **Run the Application:**
   ```bash
   dotnet run --project Techcrafts.Blogging.Web
   ```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
