# UniConnect Backend

Backend API for **UniConnect**, a university collaboration platform that supports academic communities, posts, comments, tasks, notifications, and role-based management (Student, Teacher, Dean, Department Manager).

This repository contains the .NET backend services, data models, and REST APIs used by the UniConnect frontend.

---

## Features
- **Auth & Roles**: JWT authentication, role-based access control.
- **Academic Structure**: Faculties, Courses, Student Groups, Subjects.
- **Communities**: Auto-created academic communities + manual department communities.
- **Posts & Comments**: Create, edit, delete, and react to posts.
- **Invitations**: Community invitation flow.
- **Subject Join Codes**: Teachers create subjects with a join code for students.
- **Testing**: Unit tests using xUnit + EF InMemory.

---

## Tech Stack
- **.NET** (Web API)
- **Entity Framework Core**
- **SQL Server**
- **JWT Authentication**
- **xUnit** (unit tests)

---

## Getting Started

### 1. Prerequisites
- .NET SDK installed
- SQL Server available (local or remote)

### 2. Restore & Build
```powershell
dotnet restore .\UniConnect\UniConnect.csproj
dotnet build .\UniConnect\UniConnect.csproj
```

### 3. Database Migrations
```powershell
dotnet ef migrations add <MigrationName> --project .\UniConnect\UniConnect.csproj --startup-project .\UniConnect\UniConnect.csproj
dotnet ef database update --project .\UniConnect\UniConnect.csproj --startup-project .\UniConnect\UniConnect.csproj
```

### 4. Run the API
```powershell
dotnet run --project .\UniConnect\UniConnect.csproj
```

By default the API runs on:
- `https://localhost:7231`
- `http://localhost:5226`

---

## API Documentation
Swagger is enabled in Development.  
Once running, open:
```
https://localhost:7231/swagger
```

---

## Project Structure
```
UniConnect/
  Controllers/     API endpoints
  Repository/      Business logic/services
  Models/          EF Core entities
  Dtos/            API request/response contracts
  INterfface/      Service interfaces
  Maping/          DbContext
  Migrations/      EF Core migrations
UniConnect.Tests/  xUnit tests
```

---

## Testing
```powershell
dotnet test
```

---

## Contributing
PRs are welcome. Please keep commits focused and include tests for new behavior where possible.

---

## License
MIT
