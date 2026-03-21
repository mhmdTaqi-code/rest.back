# Implementation Plan: QR Table Menu Access With Authenticated Ordering Foundations

**Branch**: `001-smart-dining-mvp` | **Date**: 2026-03-21 | **Spec**: `/specs/001-smart-dining-mvp/spec.md`  
**Input**: Feature specification from `/specs/001-smart-dining-mvp/spec.md`

## Summary

Implement backend-only foundations for QR table menu access and authenticated ordering inside the
existing Smart Dining System without redesigning the project.

The planned implementation adds:

- restaurant tables with unique public QR/table tokens
- restaurant-scoped menu categories and menu items
- public table token resolution and public menu-browsing endpoints
- authenticated cart or order-draft handling per user and table
- authenticated order submission linked to user, restaurant, and restaurant table
- order and order-item persistence with future-ready status values

The implementation explicitly excludes:

- frontend implementation
- payment processing
- kitchen dashboard behavior
- guest ordering without authentication
- redesign of OTP, JWT, role, restaurant approval, or current account flows

The design extends the current ASP.NET Core Web API, EF Core, and PostgreSQL architecture using
thin controllers, service-owned business logic, DTOs, validation, and EF Core migrations.

## Technical Context

**Language/Version**: C# on .NET 8  
**Primary Dependencies**: ASP.NET Core Web API, EF Core, Npgsql/PostgreSQL, JWT authentication, existing Iraq OTP integration  
**Storage**: PostgreSQL in Docker on port `5433`  
**Testing**: Solution build, targeted API/service verification, EF Core migration verification, and regression checks for current auth and approval flows  
**Target Platform**: Existing ASP.NET Core backend services  
**Project Type**: Existing layered backend solution using `Api`, `Application`, `Domain`, and `Infrastructure`  
**Performance Goals**: Public menu reads and authenticated cart operations should remain lightweight and responsive for normal restaurant traffic  
**Constraints**: Preserve existing architecture; backend only; no payment; no kitchen dashboard yet; do not break OTP, JWT, restaurant approval, or current account behavior; controllers must stay thin; logic must stay in services; use DTOs and validation  
**Scale/Scope**: Add menu and ordering foundations for restaurant tables within the current backend, ready for later kitchen and frontend phases

## Constitution Check

*GATE: Must pass before implementation. Re-check after design and before coding starts.*

- Pass: The feature extends the existing ASP.NET Core architecture instead of replacing it.
- Pass: Controllers remain thin and orchestration-only.
- Pass: Business logic stays in services.
- Pass: DTOs and validation remain explicit boundaries.
- Pass: Database changes are planned through EF Core migrations only.
- Pass: OTP, JWT, role behavior, restaurant approval, and current account flows remain preserved.
- Pass: Scope is backend only and excludes payment and kitchen dashboard implementation.

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
|   |-- Controllers/
|   |-- DTOs/
|   `-- Extensions/
|-- SmartDiningSystem.Application/
|   |-- DTOs/
|   |-- Services/
|   `-- Interfaces/
|-- SmartDiningSystem.Domain/
|   |-- Entities/
|   `-- Enums/
`-- SmartDiningSystem.Infrastructure/
    |-- Data/
    |-- Configurations/
    `-- Services/
```

**Structure Decision**: Extend the existing layered solution. API endpoints stay in `Api`, DTOs and
service contracts stay in `Application`, entities and enums stay in `Domain`, and EF Core
configurations plus service implementations stay in `Infrastructure`.

## Data Model Design

### Planned Entities

#### RestaurantTable

Purpose:
Represents a physical table inside a restaurant and the public context reached from a QR code.

Planned responsibilities:

- link a table to exactly one restaurant
- store a human-readable identifier such as table number or label
- store a unique public QR/table token
- support active/inactive availability for public access

Planned relationships:

- one `Restaurant` to many `RestaurantTable`
- one `RestaurantTable` to many future `Order` records
- one `RestaurantTable` to zero or more active cart or draft records

#### MenuCategory

Purpose:
Groups menu items for one restaurant in a structure suitable for public browsing.

Planned responsibilities:

- belong to exactly one restaurant
- hold category-level display information
- support menu ordering and filtering in future API responses

Planned relationships:

- one `Restaurant` to many `MenuCategory`
- one `MenuCategory` to many `MenuItem`

#### MenuItem

Purpose:
Represents a restaurant menu entry that can appear in the public menu and in authenticated cart and
order flows.

Planned responsibilities:

- belong to exactly one restaurant
- belong to one menu category
- expose name, description, price, and availability state
- support active/inactive or available/unavailable validation for ordering

Planned relationships:

- one `Restaurant` to many `MenuItem`
- one `MenuCategory` to many `MenuItem`
- one `MenuItem` to many cart or draft items
- one `MenuItem` to many `OrderItem` records

#### Cart Or Draft-Order Approach

Recommended approach:
Use a persisted cart or order-draft model scoped by authenticated user and restaurant table.

Planning direction:

- maintain exactly one active cart or draft per authenticated user per table context
- include restaurant linkage either directly or derivable through the table
- store line items separately for quantity-based updates
- convert the active cart or draft into a submitted order without losing relational integrity

Why this approach:

- keeps add/update/remove behavior straightforward
- preserves table-specific state before submission
- avoids mixing unsubmitted state directly into the final `Order` lifecycle
- stays extensible for later timeout, merge, and recovery behavior

#### Order

Purpose:
Represents a submitted order ready for future operational processing.

Planned responsibilities:

- always belong to an authenticated user
- always belong to a restaurant
- always belong to a restaurant table
- start in `Received` status
- remain ready for future kitchen status progression

Planned relationships:

- one `UserAccount` to many `Order`
- one `Restaurant` to many `Order`
- one `RestaurantTable` to many `Order`
- one `Order` to many `OrderItem`

#### OrderItem

Purpose:
Represents a submitted line item captured from the cart at order time.

Planned responsibilities:

- belong to exactly one order
- reference the selected menu item
- capture quantity and price snapshot data needed for future processing
- preserve the submitted state even if menu data changes later

Planned relationships:

- one `Order` to many `OrderItem`
- many `OrderItem` records may reference one `MenuItem`

### Relationship Rules

Orders must relate to:

- `UserAccount`: required; guest orders are not allowed in this phase
- `Restaurant`: required; must match the restaurant derived from the table
- `RestaurantTable`: required; must match the scanned table context
- `MenuItem`: required through order items; all items must belong to the same restaurant as the table

Consistency rules:

- a `RestaurantTable` cannot belong to multiple restaurants
- a `MenuCategory` cannot mix menu items from different restaurants
- a cart or draft cannot contain items from multiple restaurants
- an order cannot contain items from multiple restaurants
- the restaurant on the order must match the restaurant on the table and all included menu items

## Access Design

### Public Endpoints

Planned public capabilities:

- resolve table by token
- get public menu for a restaurant table

Endpoint direction:

- `GET /api/public/tables/{token}`
  - resolves a public table token
  - returns safe public context such as restaurant summary, table summary, and table validity state
- `GET /api/public/tables/{token}/menu`
  - returns menu categories and menu items for the resolved restaurant table context

Public endpoint rules:

- no authentication required for browse-only access
- no cart mutation or order submission capability
- response shape must remain browse-oriented only
- invalid or inactive token must return a clear failure response

### Authenticated Endpoints

Planned authenticated capabilities:

- add to cart
- update/remove cart items
- view current cart
- submit order

Endpoint direction:

- `GET /api/table-ordering/tables/{token}/cart`
  - returns the authenticated user's active cart for the resolved table
- `POST /api/table-ordering/tables/{token}/cart/items`
  - adds a menu item to the authenticated user's cart for that table
- `PUT /api/table-ordering/tables/{token}/cart/items/{itemId}`
  - updates quantity or item state in the cart
- `DELETE /api/table-ordering/tables/{token}/cart/items/{itemId}`
  - removes an item from the authenticated user's cart
- `POST /api/table-ordering/tables/{token}/orders`
  - submits the authenticated user's current cart as an order

Authenticated endpoint rules:

- require JWT-authenticated user context
- derive `UserId` from the authenticated principal, not from client input
- resolve table by token before performing cart or order actions
- enforce restaurant consistency between table and menu items
- return validation errors for unavailable items or invalid quantities

## Authentication Rules

- Guests can access public browse-only endpoints.
- Guests cannot create, view, modify, or submit cart or order data.
- Authenticated ordering endpoints must require a valid authenticated user.
- Submitted orders must always include a valid server-derived `UserId`.
- Client requests must not be trusted to supply or override `UserId`.
- Existing JWT behavior must be reused rather than replaced.
- Existing OTP behavior remains unchanged because this feature does not alter public authentication flows.

## Order Lifecycle Design

### Initial Status Model

Planned order status enum:

- `Received`
- `Preparing`
- `Ready`
- `Served`

### Lifecycle Rules

- every newly submitted order starts as `Received`
- `Preparing`, `Ready`, and `Served` are included now for future kitchen workflow compatibility
- no kitchen dashboard or kitchen transition APIs are included in this phase
- no payment status is introduced in this phase

### Future-Readiness Notes

- state naming should be stable enough to support later staff-facing workflows
- order records should store enough relational context for later kitchen filtering by restaurant and table
- order history should not depend on the cart record remaining active after submission

## Service-Layer Design

### Table Resolution Service

Purpose:
Centralize table-token lookup and validation for both public and authenticated flows.

Planned responsibilities:

- resolve a table from token
- validate token existence
- validate table activity state
- validate restaurant availability state as required by current business rules

### Public Menu Service

Purpose:
Provide public browse-only menu data for a resolved table context.

Planned responsibilities:

- load restaurant summary for the table
- load menu categories and active menu items
- shape public response DTOs
- exclude any authenticated-only cart or order data

### Cart Service

Purpose:
Own all add/view/update/remove behavior for authenticated user carts or drafts.

Planned responsibilities:

- create or load active cart for authenticated user and table
- add items with quantity validation
- update item quantities
- remove items
- validate menu item availability
- validate restaurant and table consistency
- return cart DTOs

### Order Submission Service

Purpose:
Turn a valid authenticated cart or draft into a persisted order.

Planned responsibilities:

- load authenticated user's active cart for the target table
- validate cart is non-empty
- revalidate menu item availability and restaurant consistency at submit time
- create order and order items
- assign initial `Received` status
- clear, close, or mark the draft/cart as submitted according to the chosen persistence approach
- use transaction handling for order creation and cart finalization

## Validation And Safety Plan

### Validation Rules

#### Invalid Table Tokens

- reject unknown tokens with not-found style responses
- do not leak internal identifiers when token resolution fails

#### Inactive Tables

- block public and authenticated ordering flows when the table is inactive
- return clear validation or business-rule responses

#### Unavailable Menu Items

- block add-to-cart when the item is unavailable or inactive
- revalidate availability again during order submission to prevent stale-cart ordering

#### Invalid Quantities

- reject zero, negative, or otherwise invalid quantities
- define reasonable quantity validation at DTO and service levels

#### Cross-Restaurant Consistency

- reject cart or order actions when the item restaurant does not match the table restaurant
- reject inconsistent data if a stale client tries to submit mixed-context requests

#### Unauthorized Guest Ordering Attempts

- reject guest requests to cart or order endpoints using the existing auth conventions
- keep public endpoints browse-only and free of state mutation

### Safety Measures

- derive table context from token, not from client-submitted restaurant IDs
- derive user identity from authenticated principal, not request body
- use service-layer checks before persisting cart or order data
- use transactional persistence for order submission
- preserve referential integrity with explicit foreign keys and constraints

## Database Migration Strategy

### Required Schema Changes

Planned schema additions:

- add `RestaurantTables` table
- add `MenuCategories` table
- add `MenuItems` table
- add cart or draft-order tables plus line-item table
- add `Orders` table
- add `OrderItems` table
- add order status enum or mapped status field as appropriate to the existing project style

Potential schema updates:

- add indexes for public table token lookup
- add indexes for cart lookup by user and table
- add indexes for order lookup by restaurant, table, and user
- add uniqueness constraints where needed for token safety and table identity rules

### Migration Order

Recommended migration sequence:

1. Add foundational enums and domain models if required by the project structure.
2. Add `RestaurantTable` schema and token uniqueness/indexing.
3. Add menu schema: `MenuCategory` then `MenuItem`.
4. Add cart or draft schema and line items.
5. Add `Order` and `OrderItem` schema plus order status mapping.
6. Add or refine indexes and constraints for performance and consistency.

### Migration Safety Notes

- migrations must extend the existing schema rather than rewrite existing tables
- no existing auth, OTP, restaurant approval, or account tables should be structurally rewritten for this feature
- data seeding, if later needed, should be additive and optional

## Compatibility Strategy

This feature extends the current system without rewriting it.

Compatibility direction:

- reuse the existing layered architecture and project boundaries
- keep current authentication and authorization mechanisms intact
- add new entities and services rather than refactoring unrelated subsystems
- preserve existing restaurant approval behavior while attaching new table and menu data to restaurants
- keep current account and role flows unchanged
- avoid introducing frontend dependencies in this phase

Expected integration behavior:

- existing auth endpoints remain unchanged
- existing OTP flows remain unchanged
- existing restaurant approval flows remain unchanged
- new endpoints live alongside current APIs without replacing them
- future frontend and kitchen features can build on these backend contracts later

## Implementation Direction For Next Tasks Phase

Recommended execution order:

1. Add domain enums and entities for restaurant tables, menus, cart or draft, orders, and order items.
2. Configure EF Core mappings, relationships, constraints, and indexes.
3. Add migration(s) in the planned additive order.
4. Add DTOs and validation models for public menu, cart, and order requests and responses.
5. Implement table resolution and public menu services.
6. Implement authenticated cart service.
7. Implement order submission service with transaction handling.
8. Add thin API controllers for public table menu and authenticated table ordering.
9. Verify compatibility with JWT, OTP, restaurant approval, and existing account behavior.

## Complexity Tracking

No constitution violations are expected in this plan.

Clarifications:

- This plan is backend only.
- No payment implementation is included.
- No kitchen dashboard implementation is included.
- Guests can browse only.
- Only authenticated users can create or modify cart and order data.
