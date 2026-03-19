# Implementation Plan: MainAdmin UI Cleanup And Hard Delete Account Management

**Branch**: `001-smart-dining-mvp` | **Date**: 2026-03-19 | **Spec**: `/specs/001-smart-dining-mvp/spec.md`  
**Input**: Feature specification from `/specs/001-smart-dining-mvp/spec.md`

## Summary

Improve the existing `/mainadmin` developer control panel inside the current Smart Dining ASP.NET
Core project without changing the core system architecture.

The current implementation scope includes:

- Sidebar + topbar MainAdmin layout cleanup
- Accounts table page cleanup
- Create account page
- Edit account page
- Delete confirmation modal
- Service-based account create, update, activate, deactivate, and hard delete
- DTO/ViewModel-based create and update flows
- Validation rules for account-management operations
- Permanent hard delete with safety validation, relationship handling, and transaction usage

The implementation does not include:

- Introducing new frameworks or replacing MVC/Razor + Bootstrap
- Rewriting the system or redesigning the architecture
- Replacing existing OTP, JWT, or restaurant approval workflows
- Complex dashboard widgets, charts, or analytics
- Public-auth redesign or approval-flow redesign

The implementation must follow the constitution strictly:

- Controllers remain thin and delegate business logic to services
- DTO and ViewModel boundaries remain explicit
- Authorization and identity rules remain server-enforced
- PostgreSQL, EF Core migrations, and async data access remain required
- Account deletion safety must preserve relational integrity
- Existing OTP, JWT, and restaurant approval flows must remain compatible

## Technical Context

**Language/Version**: C# on .NET 8  
**Primary Dependencies**: ASP.NET Core MVC/Razor, Bootstrap, ASP.NET Core Web API, EF Core, Npgsql, JWT auth, cookie auth for `MainAdmin`  
**Storage**: PostgreSQL  
**Testing**: Build verification plus targeted validation of MainAdmin account CRUD, activation/deactivation, hard delete safety, and compatibility with existing auth/approval flows  
**Target Platform**: Existing ASP.NET Core backend and MainAdmin UI  
**Project Type**: Existing layered .NET solution using `Api`, `Application`, `Domain`, and `Infrastructure`  
**Performance Goals**: MainAdmin account-management operations should remain responsive while preserving transactional safety and integrity validation for hard delete  
**Constraints**: Keep current ASP.NET MVC/Razor + Bootstrap architecture; do not introduce new frameworks; do not rewrite the system; controllers remain thin; logic remains in services; use DTOs/ViewModels and validation; preserve OTP, JWT, and restaurant approval flows  
**Scale/Scope**: MainAdmin UI cleanup, account-management UX improvement, and safe hard delete only

## Constitution Check

*GATE: Must pass before implementation. Re-check after design and before coding starts.*

- Pass: Controllers stay thin and hand business logic to services.
- Pass: Request/response UI models stay separate from EF entities.
- Pass: Existing security and role behavior are preserved rather than redesigned.
- Pass: Database changes, if any, must still go through EF Core migrations only.
- Pass: Scope is limited to MainAdmin UI cleanup and hard-delete account management.

## Project Structure

### Documentation (this feature)

```text
specs/001-smart-dining-mvp/
|-- plan.md
|-- spec.md
`-- tasks.md
```

### Source Code (planned implementation areas)

```text
backend/
|-- SmartDiningSystem.Api/
|   |-- Areas/
|   |   `-- Admin/
|   |       |-- Controllers/
|   |       `-- Views/
|   `-- wwwroot/
|-- SmartDiningSystem.Application/
|   |-- Areas/
|   |   `-- Admin/
|   |       `-- Models/
|   `-- Services/
|       `-- Interfaces/
|-- SmartDiningSystem.Domain/
|   |-- Entities/
|   `-- Enums/
`-- SmartDiningSystem.Infrastructure/
    |-- Data/
    `-- Services/
```

**Structure Decision**: Extend the existing solution structure and current MainAdmin implementation
without architectural redesign. UI stays in `Api`, account-management models/interfaces stay in
`Application`, entities remain in `Domain`, and persistence/business implementations remain in
`Infrastructure`.

## Feature Design

### UI Structure

The MainAdmin UI should be cleaned up around a simple, consistent developer-control-panel layout:

- Sidebar navigation on the left
- Topbar/header across the main content area
- Accounts index page with:
  - table
  - role/status badges
  - alerts
  - action buttons
- Create and edit pages using card-based Bootstrap forms
- Delete confirmation modal before hard delete

UI design rules:

- Keep the design clean, simple, and professional
- Use Bootstrap components and light custom styling only where needed
- Avoid complex dashboard widgets, charts, or analytics visuals
- Favor readable spacing, clear alerts, and obvious action buttons

### Backend Design

Account-management behavior remains service-owned.

Backend design includes:

- Service layer for account management
- DTO/ViewModel-driven create and update forms
- Validation rules for create, update, activation, deactivation, and delete
- Thin MVC controllers that orchestrate request/response only

Planned account-management service responsibilities:

- load account list data
- load create/edit models
- create accounts
- update accounts
- activate accounts
- deactivate accounts
- validate hard delete eligibility
- execute hard delete transaction safely

### Hard Delete Strategy

Hard delete is permanent and must be implemented through a service-owned workflow.

Delete design requirements:

- Perform hard delete in the service layer only
- Validate whether related data allows permanent deletion
- Handle `RestaurantOwner <-> Restaurant` relationships explicitly
- Use transactions where multiple delete steps or relationship updates are required
- Reject delete operations that would break integrity rather than forcing unsafe removal

Planned hard-delete flow:

1. Load the target account and all relevant related data
2. Evaluate safety rules and relational dependencies
3. If safe, begin transaction
4. Remove dependent data in the required order, or stop if the relationship must remain protected
5. Permanently delete the account
6. Commit transaction
7. Return clear success or validation failure feedback

### Architecture Rules

- Keep controllers thin
- Keep business logic in services
- Extend existing code instead of rewriting the subsystem
- Reuse current account-management pages, services, and models where possible
- Keep the current ASP.NET Core MVC/Razor + Bootstrap architecture intact

### Compatibility Rules

The feature must remain compatible with current system behavior:

- Do NOT break OTP-based public registration flows
- Do NOT break JWT-based login/auth flows
- Do NOT break restaurant approval workflow behavior
- Do NOT break existing role behavior for `User`, `RestaurantOwner`, and `Admin`

## Data Considerations

### Existing Domain Reuse

- Reuse `UserAccount` as the core account entity
- Reuse `Restaurant` and any existing linked entities without redesign
- Reuse current account service boundaries where they already exist
- Reuse current identity fields (`FullName`, `Email`, `PhoneNumber`, `Username`, `PasswordHash`, `Role`, `IsActive`)

### Database Design Requirements

Hard delete must preserve database correctness:

- Relationships must remain valid
- Foreign-key violations must be prevented
- Transactions must be used when a delete operation spans multiple related records
- Existing database structure should remain unchanged unless a migration is truly required for compatibility or integrity support

### Relationship Handling

Special care is required for:

- `RestaurantOwner` and linked `Restaurant` records
- Accounts referenced by OTP/history/auth tables
- Accounts referenced by any ordering or restaurant ownership records already in the system

Deletion policy direction:

- If safe relationship cleanup is possible without violating integrity, perform permanent delete in transaction
- If delete would break integrity or violate required relationship rules, reject the operation with a clear error

## Backend Contract Direction

### Services

- `IAdminAccountService` remains the core owner of MainAdmin account-management behavior
- Service implementation handles:
  - account listing
  - create/update
  - activate/deactivate
  - hard delete validation
  - hard delete execution
  - transaction coordination

Implementation notes:

- Validation and persistence stay in services
- Controllers should not directly manipulate EF entities
- Existing password hashing, login compatibility, and role-handling rules must be preserved
- Existing account models should be extended rather than replaced where practical

### Controllers

- MainAdmin controllers stay UI-only
- Controllers coordinate:
  - loading pages
  - model-state handling
  - success/error alerts
  - redirects after actions
- Controllers do not:
  - decide hard delete safety
  - manage transactions
  - manipulate relationship cleanup directly
  - duplicate business validation already owned by services

### DTO / ViewModel Rules

- Create and edit forms use ViewModels/DTO-shaped UI models
- Validation attributes and/or service validation must enforce required rules
- Delete confirmation should pass only the minimal account identity needed for the operation
- UI models must remain separate from EF entities

## Validation and Error Handling

Validation rules should cover:

- required account fields for create/update
- role selection validity
- uniqueness constraints where relevant
- activation/deactivation safety rules where relevant
- hard delete eligibility and relationship safety

Error-handling rules:

- Return clear, user-friendly validation or business-rule messages to the MainAdmin UI
- Use alerts and validation summaries for feedback
- Do not expose internal exceptions or persistence details to the UI

## Testing Strategy

### Core Verification

- `/mainadmin` layout renders with sidebar + topbar structure
- accounts table page renders correctly
- create page and edit page render and submit correctly
- delete confirmation modal is shown before hard delete
- service-layer account create/update/activate/deactivate flows still work
- hard delete permanently removes eligible accounts from the database
- hard delete rejects unsafe deletes that would break integrity
- `RestaurantOwner` relationship handling remains safe
- OTP behavior still works
- JWT behavior still works
- restaurant approval flow still works
- the solution builds successfully after the changes

## Implementation Direction for Next Tasks Phase

Recommended execution order:

1. Clean up MainAdmin shared layout with sidebar + topbar consistency
2. Refine accounts list page UI and actions
3. Refine create/edit page UI and validation behavior
4. Implement or tighten service-owned hard delete validation
5. Add transaction-backed hard delete execution
6. Add delete confirmation modal and end-to-end UI feedback
7. Verify compatibility with OTP, JWT, and restaurant approval flows

## Complexity Tracking

No constitution violations are expected in this plan.

Clarification:

- This plan applies only to MainAdmin UI cleanup and hard-delete account management.
- The project remains the existing ASP.NET Core MVC/Razor + Bootstrap system.
- OTP, JWT, and restaurant approval workflows are preserved, not redesigned.
- Hard delete is permanent, but only when the service layer determines the operation is safe.
