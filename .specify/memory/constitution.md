<!--
Sync Impact Report
- Version change: 0.1.0 → 0.2.0
- Modified principles:
  - 1) Clean API layering (thin controllers) → 1) Layered architecture + DI + naming discipline
  - 3) Security first (JWT + RBAC everywhere) → 3) JWT + RBAC (role claims are contracts)
  - 4) Approval workflow integrity (restaurant onboarding) → 4) Restaurant approval workflow is a state machine
  - 5) Data correctness (EF Core migrations + async + validation) → 5) Database-first discipline (migrations, FKs, indexes, async)
- Added sections: API Contract & Error Handling
- Removed sections: N/A
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md (still compatible; "Constitution Check" stays dynamic)
  - ✅ .specify/templates/spec-template.md (still compatible)
  - ✅ .specify/templates/tasks-template.md (still compatible)
- Deferred items:
  - TODO(RATIFICATION_DATE): original adoption date not found in repo history/docs
-->

# SmartDiningSystem Constitution

## Core Principles

### 1) Layered architecture + DI + naming discipline
Use a layered backend architecture: Controllers, Services, Data, Entities, DTOs.
Controllers MUST be thin: translate HTTP ⇄ DTOs, enforce authorization, and delegate to services.
Business rules MUST live in services; persistence details MUST stay in the data layer.
Dependency injection MUST be used for all app services and data access; avoid static/global state.
Naming MUST be clean and consistent across routes, DTOs, services, and entities.

### 2) DTO boundary (no EF entities in API responses)
API request/response shapes MUST use DTOs. EF entities MUST NOT be returned directly from endpoints,
and MUST NOT be exposed as public API contracts. Mapping MAY be manual or via a mapper, but the
boundary is non-negotiable.

### 3) JWT + RBAC (role claims are contracts)
Authentication MUST use JWT. Tokens MUST include `userId` and `role` claims (contractually stable).
Protected endpoints MUST enforce role-based authorization. Roles are exactly: User, RestaurantOwner,
Admin. No endpoint may rely on “security by UI”.

### 4) Restaurant approval workflow is a state machine
Restaurant owners MUST submit restaurants for approval. Restaurant status is exactly: Pending,
Approved, Rejected. Rejected restaurants MUST include a rejection reason.
Only Approved restaurants are visible to normal users.
Owners MUST only manage their own restaurants; Admin has full control over approvals.
Status transitions MUST be validated and enforced server-side.

### 5) Database-first discipline (migrations, FKs, indexes, async)
PostgreSQL is the only database. Use EF Core with Npgsql and migrations.
Database schema MUST NOT be changed without migrations.
Relationships MUST be explicit via foreign keys.
Indexes MUST exist for frequently queried fields (and SHOULD exist for foreign keys).
Avoid unnecessary nullable fields.
All database operations MUST use async/await and be performance-aware (avoid N+1; optimize queries).

## Security & Data

- Use PostgreSQL with EF Core + Npgsql only.
- Never store plaintext passwords; use a modern password hash (e.g., ASP.NET Core Identity defaults).
- Authorization checks MUST be enforced server-side for:
  - Restaurant visibility (Approved-only for normal users)
  - Restaurant ownership boundaries (owners can only mutate their own restaurant resources)
  - Administrative actions (approve/reject, moderation)
- Log security-relevant events (login failures, approval decisions) without logging secrets.

## API Contract & Error Handling

- Follow RESTful API design and correct HTTP methods (GET, POST, PUT, DELETE).
- Use correct HTTP status codes.
- Validate all inputs using validation attributes or FluentValidation.
- Responses MUST be consistent (shape and error format). Do not expose internal exceptions, stack
  traces, connection strings, SQL, or sensitive details to clients.

## Engineering Workflow & Quality Gates

- Do not generate unnecessary or unused code.
- Build features incrementally (MVP first) and do not implement outside current task scope.
- Prefer simple and maintainable solutions over complex ones.
- Add comments only when they explain non-obvious intent or constraints.
- Every new endpoint MUST have:
  - Request/response DTOs
  - Input validation + consistent error responses
  - Authorization policy/role rules
  - Async data access
- Tests are not globally mandated by this constitution; when tests are requested in a feature spec,
  they MUST be treated as first-class deliverables and should cover critical state transitions
  (especially approval workflow).

## Governance

- This constitution supersedes other guidance if there is a conflict.
- Amendments MUST:
  - Update this document and increment the version using semantic versioning (MAJOR/MINOR/PATCH).
  - Include a brief rationale in the Sync Impact Report.
  - Identify any required migrations or backward-compatibility considerations.
- Compliance expectations:
  - Reviews MUST check for adherence to the Core Principles (layering, DTO boundary, RBAC,
    approval workflow integrity, migrations/validation).
  - If a principle must be violated, it MUST be explicitly justified in the feature plan’s
    “Complexity Tracking” section and minimized in scope.

**Version**: 0.2.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date unknown | **Last Amended**: 2026-03-18
