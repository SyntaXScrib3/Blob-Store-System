# Database-Based Blob Store System

**A scalable, database-driven file system providing multi-tenant isolation, deduplication, and pluggable blob storage.**

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the repository](#1-clone-the-repository)
  - [2. Configure the database](#2-configure-the-database)
  - [3. Update app settings](#3-update-app-settings)
  - [4. Run migrations](#4-run-migrations)
  - [5. Build and run the application](#5-build-and-run-the-application)
- [Usage](#usage)
  - [REST API Endpoints](#rest-api-endpoints)
  - [React Frontend](#react-frontend)
- [Deduplication Logic](#deduplication-logic)
- [Orphan Blob Cleanup](#orphan-blob-cleanup)
- [Testing](#testing)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

The **Database-Based Blob Store System** merges traditional file system semantics with a relational database for storing file metadata (paths, sizes, owners) and an external blob provider for actual file contents. By separating metadata from blob storage, this project provides:

- **Multi-tenant** user isolation
- **Deduplication** via reference-counted blobs
- Seamless **move/copy** operations for files and folders
- A **REST API** (ASP.NET Core) secured with JWT authentication
- A **React** frontend for intuitive file management

---

## Features

- **Multi-Tenant Isolation**: Each user can only access their own folder hierarchy.
- **Robust File Operations**: Create, rename, move, copy, delete files/directories.
- **Reference Counting**: Multiple files referencing the same hashed blob.
- **Orphan Blob Cleanup**: Safe removal of unreferenced blobs.
- **Pluggable Storage**: Local disk by default; easily switch to Amazon S3 or other providers.
- **DDD + SOLID**: Clean separation of domain, infrastructure, and application layers.

---

## Architecture

```
Domain Layer  ->  Entities (FileNode, DirectoryNode, Blob, User)
                 IFsProvider interface

Infrastructure Layer ->  EF Core DbContext (AppDbContext)
                         IBlobStorageProvider for actual blob operations
                         EfFsProvider: Implementation of IFsProvider

ASP.NET Core Web API -> Controllers for /auth, /directory, /file endpoints
                       JWT-based authentication

React Frontend -> UI for folder browsing, file upload, rename, move/copy, preview
```

---

## Prerequisites

- **.NET 8.0** installed
- **SQL Server** (or another relational DB if you update the EF Core config)
- **Node.js** (16+ recommended) for the React frontend
- (Optional) Docker for containerizing the application

---

## Getting Started

### 1. Clone the Repository

```bash
git https://github.com/SyntaXScrib3/Blob-Store-System.git
cd DatabaseBlobStore
```

### 2. Configure the Database

- Create a local or remote SQL Server database (e.g., `BlobStoreDB`).
- Update the **connection string** in `appsettings.json` (for the ASP.NET Core project) or your secrets configuration.

### 3. Update App Settings

- In `appsettings.json` (or `appsettings.Development.json`), specify:
  ```json
  {
    "ConnectionStrings": {
      "BlobStoreDb": "Server=localhost;Database=BlobStoreDB;Trusted_Connection=True;"
    },
    "JwtSettings": {
      "SecretKey": "YOUR_STRONG_SECRET_KEY",
      "Issuer": "BlobStoreSystem",
      "Audience": "BlobStoreUsers"
    }
  }
  ```
- **SecretKey** should be a long random string for JWT security.

### 4. Run Migrations

```bash
cd BlobStoreSystem.WebApi
dotnet ef database update
```

_(If you’re not using migrations, ensure you have a valid schema or run any seed scripts.)_

### 5. Build and Run the Application

```bash
# In the Web API project directory
dotnet run
```

- This should launch the ASP.NET Core app on `https://localhost:5001` (by default).

For the React frontend:

```bash
cd blobstore-frontend
npm install
npm run dev
```

- Access the UI at `http://localhost:5173` (or whatever port Vite displays).

---

## Usage

### REST API Endpoints

- **`POST /api/auth/register`**: Register a new user
- **`POST /api/auth/login`**: Obtain JWT
- **`GET /api/directory/list?path=/somePath`**: List items in a directory
- **`POST /api/file/upload?path=/somePath/fileName`**: Upload a file
- **`DELETE /api/file/delete?path=/somePath/fileName`**: Delete a file
- **`POST /api/directory/move?oldPath=...&newPath=...`**: Move or rename a directory
- (Etc.)

See the [API documentation](docs/API.md) for detailed request/response formats.

### React Frontend

1. **Register** or **log in** to obtain a token.
2. **Dashboard**:
   - Create folders via “Add New” → “Create Folder”
   - Upload files with the “Upload File” option
   - Preview text/image files
   - Select multiple items to move/copy/delete in bulk

---

## Deduplication Logic

When a file is uploaded:

1. **Compute** a SHA-256 hash of the file content.
2. **Check** if a `Blob` entity with that hash already exists in the database.
3. If yes, re-use it (increment `ReferenceCount`).
4. If not, create a new `Blob` record, `ReferenceCount = 1`.
5. Update or create a `FileNode` referencing that blob.

Deletion:

- If the `Blob.ReferenceCount` hits zero, it may be removed from the database and disk.

---

## Orphan Blob Cleanup

Unreferenced blobs (i.e., `ReferenceCount <= 0`) can be removed via:

- **Scheduled job** (background service) scanning for orphaned blobs, removing them from the database and physical storage.

---

## Testing

- **Unit Tests**: Domain logic (e.g., `EfFsProviderTests`) verifying moves, copies, dedup behavior.
- **Integration Tests**: Using `WebApplicationFactory` or Docker-based setups to confirm multi-tenant boundaries, path manipulations, and blob reference updates.
- **Frontend Tests**: Basic UI tests (Cypress, Jest) ensuring the file explorer operations run smoothly.

---

## Contributing

1. **Fork** this repo
2. **Create** a feature branch (`git checkout -b new-feature`)
3. **Commit** your changes
4. **Push** the branch to GitHub
5. **Open** a Pull Request

Before submitting, please:

- Include relevant **unit/integration tests**
- Ensure **linting** and **format** checks pass

---

## License

This project is licensed under the [MIT License](LICENSE.md). Feel free to modify, distribute, and use it in commercial or private software, provided the license terms are respected.
