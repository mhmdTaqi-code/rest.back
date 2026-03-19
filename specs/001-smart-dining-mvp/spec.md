# Feature Specification: MainAdmin Developer Control Panel

**Feature Branch**: `001-smart-dining-mvp`  
**Created**: 2026-03-18  
**Updated**: 2026-03-19  
**Status**: Draft  
**Input**: User description: "Improve /mainadmin to become a clean developer-only control panel."

## Scope

This feature defines the developer-only `MainAdmin` control panel inside the existing Smart Dining
ASP.NET Core project.

The feature is limited to:

- Existing ASP.NET Core MVC/Razor + Bootstrap `MainAdmin` UI under `/mainadmin`
- Viewing all accounts
- Creating accounts with roles:
  - `User`
  - `RestaurantOwner`
  - `Admin`
- Editing accounts
- Activating accounts
- Deactivating accounts
- Hard deleting accounts permanently from the database
- Hard-delete safety rules for relational integrity, especially `RestaurantOwner <-> Restaurant`
- Clean Bootstrap-based account-management UI with tables, cards, badges, alerts, and confirmation dialogs

Out of scope for the current implementation:

- Treating this as a new project
- Redesigning the existing architecture
- Introducing Payload CMS or any new framework
- Replacing ASP.NET Core MVC/Razor + Bootstrap
- Complex dashboards, charts, or analytics widgets
- Replacing or redesigning OTP, JWT, restaurant approval, or auth workflows
- Public registration behavior changes outside what account management already relies on

## User Scenarios & Testing *(mandatory)*

### User Story 1 - MainAdmin views all accounts from one clean control-panel page (Priority: P1)

The developer-only `MainAdmin` can open `/mainadmin/accounts` and view all system accounts in a
clean Bootstrap-based table.

**Why this priority**: The control panel is not useful if the developer cannot inspect the account
data currently stored in the system.

**Independent Test**: Opening `/mainadmin/accounts` shows all visible accounts in a clean table
with role, status, and core identity information.

**Acceptance Scenarios**:

1. **Given** the developer is authenticated in `/mainadmin`, **When** they open the accounts page,
   **Then** the system displays all accounts in a table view.
2. **Given** accounts exist with different roles and states, **When** the page loads, **Then** the
   UI shows role and active/inactive state clearly using simple Bootstrap-friendly presentation.
3. **Given** the accounts page is shown on desktop or mobile, **When** the table and surrounding
   UI render, **Then** the page remains readable and professional without complex dashboard elements.

---

### User Story 2 - MainAdmin creates User, RestaurantOwner, and Admin accounts manually (Priority: P1)

The developer-only `MainAdmin` can create accounts manually from the control panel for the
supported internal roles.

**Why this priority**: The control panel must allow the developer to seed or manage real accounts
without depending on public registration flows.

**Independent Test**: From `/mainadmin/accounts/create`, the developer can create a `User`,
`RestaurantOwner`, or `Admin` account with valid data; the account is persisted with a hashed
password and can use the normal username/password login flow where applicable.

**Acceptance Scenarios**:

1. **Given** the developer opens `/mainadmin/accounts/create`, **When** valid account data is
   submitted for role `User`, **Then** the backend creates a `UserAccount` with role `User`.
2. **Given** valid account data is submitted for role `Admin`, **When** the create flow succeeds,
   **Then** the backend creates a `UserAccount` with role `Admin` without exposing any public
   admin-registration endpoint.
3. **Given** valid account data is submitted for role `RestaurantOwner`, **When** the create flow
   succeeds, **Then** the backend creates a `UserAccount` with role `RestaurantOwner` and preserves
   existing restaurant-linked behavior required by the current system.
4. **Given** a MainAdmin-created account is stored successfully, **When** the account later logs in
   through the existing username/password auth flow, **Then** the stored hashed password works and
   no plaintext password has been persisted.

---

### User Story 3 - MainAdmin edits and activates or deactivates accounts (Priority: P1)

The developer-only `MainAdmin` can modify existing account details and can activate or deactivate
accounts from the control panel.

**Why this priority**: Account management is incomplete if the developer can create accounts but
cannot maintain them safely over time.

**Independent Test**: Opening an account edit page allows valid changes to be saved, and activate
or deactivate actions change the stored account state.

**Acceptance Scenarios**:

1. **Given** an existing account is opened in `/mainadmin/accounts/{id}/edit`, **When** the
   developer updates valid fields, **Then** the changes persist to the database.
2. **Given** an active account exists, **When** the developer deactivates it, **Then** the
   account state is stored as inactive.
3. **Given** an inactive account exists, **When** the developer activates it, **Then** the
   account state is stored as active.
4. **Given** the UI presents account status, **When** the page reloads after the action, **Then**
   success or error feedback is shown clearly through alerts or badges.

---

### User Story 4 - MainAdmin permanently hard deletes accounts without breaking integrity (Priority: P1)

The developer-only `MainAdmin` can permanently delete accounts from the database, but only when
doing so does not violate system integrity or relational rules.

**Why this priority**: The requested control panel must support true hard delete, but permanent
deletion must not corrupt linked data or leave invalid relationships.

**Independent Test**: A deletable account is permanently removed from the database, while an
account whose relationships would break integrity is blocked or handled according to explicit
service-owned rules.

**Acceptance Scenarios**:

1. **Given** an account has no blocking related data, **When** the developer confirms deletion,
   **Then** the backend permanently deletes the account from the database.
2. **Given** an account has linked relational data that would break integrity if deleted,
   **When** the developer attempts hard delete, **Then** the backend blocks the operation with a
   clear error instead of damaging the system.
3. **Given** a `RestaurantOwner` account has a linked `Restaurant`, **When** hard delete is
   attempted, **Then** the backend enforces explicit relationship-safe behavior and does not leave
   orphaned or invalid records.
4. **Given** a permanent delete action is available in the UI, **When** the developer triggers it,
   **Then** the UI requires clear confirmation before the deletion is executed.

---

### User Story 5 - MainAdmin remains separate from public auth and operational workflows (Priority: P2)

The `MainAdmin` panel stays a developer-only internal UI and does not replace the existing public
registration, JWT, OTP, or operational approval architecture.

**Why this priority**: The requested work is an improvement to the existing control panel, not an
architecture redesign or a replacement of current auth and approval flows.

**Independent Test**: `/mainadmin` remains MVC/Razor + Bootstrap UI only, public auth endpoints
remain unchanged, and restaurant approval flows still rely on the existing service-backed business
logic.

**Acceptance Scenarios**:

1. **Given** the MainAdmin feature is implemented, **When** the project is reviewed, **Then** it
   still uses ASP.NET Core MVC/Razor + Bootstrap rather than a new framework.
2. **Given** OTP and JWT already exist, **When** MainAdmin account management changes are added,
   **Then** those existing flows remain preserved rather than redesigned.
3. **Given** restaurant approval workflows already exist, **When** the control panel is improved,
   **Then** those workflows remain service-owned and behaviorally intact.

## Edge Cases

- What happens when MainAdmin attempts to create an account with a duplicate username?
- What happens when MainAdmin attempts to create an account with a duplicate email?
- What happens when MainAdmin attempts to create an account with a duplicate phone number?
- What happens when MainAdmin attempts to hard delete an account that has linked restaurant data?
- What happens when MainAdmin attempts to hard delete a `RestaurantOwner` but a related
  `Restaurant` still depends on that relationship?
- What happens when MainAdmin attempts to hard delete an account that is referenced by OTP,
  orders, or other existing records?
- What happens when activation or deactivation is requested for an account that is already in the
  target state?
- What happens when the UI submits invalid data for create or edit?
- What happens when the control panel becomes visually cluttered or dashboard-like instead of
  staying simple and professional?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep `/mainadmin` as an ASP.NET Core MVC/Razor + Bootstrap control panel.
- **FR-002**: The system MUST NOT be treated as a new project and MUST NOT introduce a new admin
  framework or CMS.
- **FR-003**: MainAdmin account-management controllers MUST remain thin and delegate business logic
  to services.
- **FR-004**: MainAdmin account-management flows MUST use DTOs/ViewModels and validation rather than
  exposing entity binding directly to the UI.
- **FR-005**: `/mainadmin/accounts` MUST display all accounts in a clean table-based UI.
- **FR-006**: MainAdmin MUST be able to create accounts with roles:
  - `User`
  - `RestaurantOwner`
  - `Admin`
- **FR-007**: MainAdmin MUST be able to edit accounts.
- **FR-008**: MainAdmin MUST be able to activate accounts.
- **FR-009**: MainAdmin MUST be able to deactivate accounts.
- **FR-010**: MainAdmin MUST be able to permanently hard delete accounts from the database.
- **FR-011**: Hard delete MUST be permanent and MUST NOT be implemented as soft delete.
- **FR-012**: Hard delete MUST respect relational integrity and MUST NOT leave broken references or
  orphaned data.
- **FR-013**: Hard delete rules for `RestaurantOwner` and linked `Restaurant` data MUST be enforced
  explicitly in the service layer.
- **FR-014**: If a hard delete would break integrity, the backend MUST reject the operation with a
  clear error response instead of forcing the delete.
- **FR-015**: MainAdmin account creation MUST support password entry and MUST store only a hashed
  password, never plaintext.
- **FR-016**: MainAdmin-created accounts MUST remain compatible with the existing username/password
  login flow.
- **FR-017**: MainAdmin account creation MUST NOT require OTP for internally created accounts.
- **FR-018**: Existing OTP integration MUST remain intact for public flows that already use it.
- **FR-019**: Existing JWT authentication behavior MUST remain intact.
- **FR-020**: Existing restaurant approval behavior MUST remain intact.
- **FR-021**: Existing `RestaurantOwner` approval-gated access behavior MUST remain intact.
- **FR-022**: Existing `Admin` operational approval capabilities MUST remain intact.
- **FR-023**: The MainAdmin UI MUST remain clean, simple, and professional.
- **FR-024**: The MainAdmin UI MUST use Bootstrap-based tables, cards, badges, alerts, and
  confirmation dialogs.
- **FR-025**: The MainAdmin UI MUST NOT introduce complex dashboards or charts for this feature.
- **FR-026**: The backend MUST keep `UserAccount` and `Restaurant` as separate persisted entities.
- **FR-027**: The backend MUST preserve the current ASP.NET Core architecture and MUST NOT redesign
  the project to implement this feature.

### Assumptions

- The existing Smart Dining project already contains working ASP.NET Core Web API, EF Core,
  PostgreSQL, JWT, OTP integration, and a `/mainadmin` MVC/Razor UI.
- The existing Bootstrap-based MainAdmin UI is the correct foundation and should be improved rather
  than replaced.
- Account creation, editing, activation, deactivation, and deletion remain service-owned workflows.
- Hard delete is desired only when it can be performed safely without violating relational
  integrity.
- Relationship-safe hard delete may require rejecting deletion attempts for some accounts if
  dependent data still exists.

### Key Entities *(include if feature involves data)*

- **UserAccount**: system account entity for `User`, `RestaurantOwner`, and `Admin`.
- **Restaurant**: restaurant entity that may be linked to a `RestaurantOwner`.
- **MainAdmin UI Model**: account-management request/response models used by MVC/Razor pages.

### Relationships (conceptual)

- `UserAccount` with role `RestaurantOwner` **may own** one or more related `Restaurant` records
  according to current system behavior.
- Hard delete eligibility for a `UserAccount` **depends on** whether linked records can be removed
  safely without breaking integrity.

### State transitions (non-negotiable)

**Account status**

- `Active -> Inactive`: after MainAdmin deactivation
- `Inactive -> Active`: after MainAdmin activation

**Hard delete**

- `Existing account -> Permanently removed`: after confirmed MainAdmin hard delete when integrity
  rules allow it
- `Existing account -> Delete rejected`: after MainAdmin hard delete attempt when integrity rules
  would be violated

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: MainAdmin can view all accounts in a clean Bootstrap-based accounts page.
- **SC-002**: MainAdmin can create `User`, `RestaurantOwner`, and `Admin` accounts from the control panel.
- **SC-003**: MainAdmin can edit existing accounts and persist changes successfully.
- **SC-004**: MainAdmin can activate and deactivate accounts, and the stored account state updates correctly.
- **SC-005**: MainAdmin can permanently hard delete eligible accounts from the database.
- **SC-006**: Hard delete attempts that would break system integrity are safely rejected instead of corrupting data.
- **SC-007**: MainAdmin-created accounts store hashed passwords and can later authenticate through
  the existing username/password login flow.
- **SC-008**: The UI remains clean, simple, Bootstrap-based, and professional without complex dashboard features.
- **SC-009**: Existing OTP, JWT, and restaurant approval workflows remain intact after this feature update.
