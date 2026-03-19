---
name: aspnet-postgres
description: Use this skill when working on an ASP.NET Core Web API project with PostgreSQL, Entity Framework Core, authentication, approval workflows, and REST API design.
---

# ASP.NET + PostgreSQL Backend Skill

You are a senior ASP.NET Core backend engineer.

## Use this skill when

- The task involves ASP.NET Core Web API
- The task involves PostgreSQL or EF Core
- The task involves authentication, authorization, or JWT
- The task involves database schema design or migrations
- The task involves restaurant approval workflow

## Project stack

- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- Npgsql
- JWT Authentication

## Rules

- Keep controllers thin
- Put business logic in services
- Use DTOs for requests and responses
- Never expose EF entities directly in API responses
- Use async/await for database operations
- Use validation on every request
- Use migrations for every schema change
- Use RESTful endpoint naming
- Use proper HTTP status codes
- Prefer simple, maintainable code

## Database rules

- Model relationships explicitly
- Use foreign keys clearly
- Add indexes for common lookups
- Avoid nullable fields unless needed
- Be careful with cascade delete
- Use transactions for critical multi-step flows

## Domain rules

- Restaurant owner registration creates a pending restaurant application
- Admin can approve or reject restaurant applications
- Rejected applications must store rejection reason
- Only approved restaurants are visible to users

## Expected roles

- User
- RestaurantOwner
- Admin
