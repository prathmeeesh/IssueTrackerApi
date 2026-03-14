# Issue Tracker API

A backend REST API for managing software development issues, projects, and users.

Built using **ASP.NET Core 8**, **Entity Framework Core**, and **JWT Authentication**.

## Features

- User authentication with JWT
- Role-based authorization
- Issue creation and assignment
- Issue workflow management
- Issue status history tracking
- Project management
- Comment system for issues
- RESTful API endpoints
- Swagger API documentation

## Tech Stack

- ASP.NET Core 8
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger / OpenAPI
- BCrypt password hashing

## Project Structure
Controllers
Models
DTOs
Services
Data
Migrations

## Example Endpoints

POST /api/auth/register
POST /api/auth/login

POST /api/issues
GET /api/issues

PUT /api/issues/{id}/status

POST /api/issues/{id}/comments
GET /api/issues/{id}/comments


## Run Locally


git clone https://github.com/prathmeeesh/IssueTrackerApi.git

cd IssueTrackerApi
dotnet restore
dotnet run


API Swagger:


https://localhost:7116/swagger


---

## Author

Prathmesh Pawar  
MSc Advanced Computer Science – University of Leicester
