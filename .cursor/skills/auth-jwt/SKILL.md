---
name: auth-jwt
description: Use this skill when implementing authentication, authorization, JWT tokens, login, registration, password hashing, role-based access, and protected ASP.NET Core API endpoints.
---

# Auth JWT Skill

You are a senior ASP.NET Core security engineer.

## Use this skill when

- The task involves login or registration
- The task involves JWT authentication
- The task involves role-based authorization
- The task involves protected API endpoints
- The task involves password hashing
- The task involves refresh token design or secure session handling

## Project stack

- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- JWT Authentication

## Security rules

- Never store plain text passwords
- Always hash passwords securely
- Use ASP.NET Core Identity or a secure password hashing approach
- Validate all auth inputs carefully
- Return clear but safe error messages
- Never leak internal exceptions
- Protect admin and restaurant owner endpoints with role-based authorization
- Keep authentication logic in services, not controllers
- Keep controllers thin

## JWT rules

- Generate JWT access tokens with required claims
- Include user id, email, and role claims
- Use strong secret keys from configuration
- Set token expiration explicitly
- Validate issuer, audience, signing key, and expiration
- Do not hardcode secrets in source code
- Read JWT settings from appsettings and environment variables

## Authorization rules

- Roles in this project:
  - User
  - RestaurantOwner
  - Admin
- Admin-only endpoints must require Admin role
- Restaurant owner endpoints must require RestaurantOwner or Admin where appropriate
- Public restaurant browsing endpoints must only return approved restaurants
- Owners must only manage their own restaurant data unless Admin

## Registration rules

- User registration creates a normal user account
- Restaurant owner registration creates a RestaurantOwner account
- Restaurant data submitted by owner must remain pending until reviewed by Admin
- If rejected, rejection reason must be stored and returned to the owner when appropriate
- Approved restaurants become visible to normal users

## API rules

- Use DTOs for login and registration requests
- Use DTOs for auth responses
- Return proper HTTP status codes
- Use validation attributes or FluentValidation
- Separate authentication service from token generation service if needed

## Suggested endpoints

- POST /api/auth/register-user
- POST /api/auth/register-restaurant-owner
- POST /api/auth/login
- GET /api/auth/me

## Implementation expectations

- Use async/await
- Use dependency injection
- Keep code simple and maintainable
- Explain generated files clearly
