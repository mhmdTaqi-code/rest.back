# Tasks: QR Table Menu Access With Authenticated Ordering Foundations

**Input**: Design documents from `/specs/001-smart-dining-mvp/`  
**Prerequisites**: `plan.md`, `spec.md`

**Tests**: Validation is required because this feature adds new public and authenticated backend
flows while preserving existing OTP, JWT, restaurant approval, and account behavior.

**Organization**: Tasks are grouped into implementation phases with clear, actionable backend-only
steps.

## Format: `[ID] [P?] [Phase] Description`

- **[P]**: Can run in parallel with other `[P]` tasks once dependencies are satisfied
- **[Phase]**:
  - `P1` analysis and alignment
  - `P2` data model
  - `P3` EF Core configuration
  - `P4` DTOs and validation
  - `P5` services
  - `P6` API endpoints
  - `P7` business rules and protection
  - `P8` testing and regression

## Phase 1: Analysis And Alignment

**Goal**: Confirm how the current backend is structured before adding new ordering foundations.

**Independent Test**: The implementation team can identify the correct entities, DbContext,
service boundaries, auth flow, and restaurant ownership relationship before creating new models or
endpoints.

- [ ] T001 [P1] Review `spec.md` and confirm the feature scope is backend only with no payment or kitchen dashboard work
- [ ] T002 [P1] Review `plan.md` and confirm the planned entity set, endpoint split, service responsibilities, and migration strategy
- [ ] T003 [P1] Inspect existing domain entities in `backend/SmartDiningSystem.Domain/Entities/` and identify current restaurant, user, and approval-related models
- [ ] T004 [P1] Inspect the current EF Core DbContext and entity configurations in `backend/SmartDiningSystem.Infrastructure/` to confirm naming conventions, table mapping style, and relationship configuration style
- [ ] T005 [P1] Inspect the current auth flow in `backend/SmartDiningSystem.Api/` and related application or infrastructure services to confirm how authenticated `UserId` is resolved from JWT
- [ ] T006 [P1] Confirm how `Restaurant` currently relates to `RestaurantOwner`, including foreign keys, navigation properties, and any approval-gated access rules
- [ ] T007 [P1] Inspect the existing folder structure and naming conventions for controllers, services, DTOs, entities, enums, and migrations
- [ ] T008 [P1] Record any compatibility constraints discovered during inspection that must be preserved while adding table, menu, cart, and order foundations

## Phase 2: Data Model

**Goal**: Add the domain models required for public table-menu browsing and authenticated ordering.

**Independent Test**: The domain layer contains all required entities and enum models with clear
relationships ready for EF Core mapping.

- [ ] T009 [P2] Add `RestaurantTable` entity in `backend/SmartDiningSystem.Domain/Entities/` with restaurant linkage, display identity, token field, and active-state support
- [ ] T010 [P2] Add `MenuCategory` entity in `backend/SmartDiningSystem.Domain/Entities/` with restaurant linkage and category identity fields
- [ ] T011 [P2] Add `MenuItem` entity in `backend/SmartDiningSystem.Domain/Entities/` with restaurant linkage, category linkage, pricing fields, and availability state
- [ ] T012 [P2] Add `Order` entity in `backend/SmartDiningSystem.Domain/Entities/` with required linkage to authenticated user, restaurant, table, and status
- [ ] T013 [P2] Add `OrderItem` entity in `backend/SmartDiningSystem.Domain/Entities/` with linkage to order and menu item plus submitted quantity and price snapshot fields
- [ ] T014 [P2] Decide the cart strategy in the domain layer and add the required cart or draft-order entity or entities in `backend/SmartDiningSystem.Domain/Entities/`
- [ ] T015 [P2] Add the cart line-item entity or equivalent draft-item entity needed to support add, update, remove, and submit workflows
- [ ] T016 [P2] Add order-status enum or equivalent status model in `backend/SmartDiningSystem.Domain/Enums/` with `Received`, `Preparing`, `Ready`, and `Served`
- [ ] T017 [P2] Add or update navigation properties across restaurant, table, menu, cart or draft, order, and order-item entities so the intended relationships are represented consistently

## Phase 3: EF Core Configuration

**Goal**: Map the new domain models into the existing PostgreSQL schema safely and consistently.

**Independent Test**: The DbContext and EF Core configurations support the new models,
relationships, indexes, and migration path without disturbing existing tables.

- [ ] T018 [P3] Add `DbSet` entries for `RestaurantTable`, `MenuCategory`, `MenuItem`, cart or draft entities, `Order`, and `OrderItem` in the existing DbContext
- [ ] T019 [P3] Add EF Core configuration for `RestaurantTable`, including restaurant foreign key, token uniqueness, and any active-state defaults
- [ ] T020 [P3] Add EF Core configuration for `MenuCategory`, including restaurant foreign key and category constraints
- [ ] T021 [P3] Add EF Core configuration for `MenuItem`, including restaurant foreign key, category foreign key, price fields, and availability constraints
- [ ] T022 [P3] Add EF Core configuration for the chosen cart or draft entities, including user linkage, table linkage, and line-item relationships
- [ ] T023 [P3] Add EF Core configuration for `Order` and `OrderItem`, including user, restaurant, table, and menu-item relationships
- [ ] T024 [P3] Add required indexes and constraints for token lookup, cart lookup by user and table, and order lookup by restaurant, table, and user
- [ ] T025 [P3] Create EF Core migration or migrations for the new schema additions in the planned additive order
- [ ] T026 [P3] Verify the database update path applies cleanly against the current project database without requiring rewrites of existing auth, approval, or account schema

## Phase 4: DTOs And Validation

**Goal**: Define clean request and response contracts for public browsing and authenticated ordering.

**Independent Test**: Public and authenticated APIs can rely on explicit DTOs and validation rules
instead of binding EF entities directly.

- [ ] T027 [P4] Add DTOs for public table token resolution and public table-menu response in the existing DTO location used by the backend
- [ ] T028 [P4] Add DTOs for add-to-cart requests, including menu item identity and quantity
- [ ] T029 [P4] Add DTOs for update-cart-item requests, including the supported quantity or item update fields
- [ ] T030 [P4] Add DTOs for remove-cart-item requests or route models if the current API conventions require explicit request models
- [ ] T031 [P4] Add DTOs for current-cart responses, including table context, item list, and totals or summary fields required by backend consumers
- [ ] T032 [P4] Add DTOs for submit-order requests and submit-order responses
- [ ] T033 [P4] Add validation rules for invalid table tokens and inactive tables at the DTO or service-boundary level where the current project conventions expect them
- [ ] T034 [P4] Add validation rules for unavailable menu items and invalid quantities
- [ ] T035 [P4] Add validation rules that reject cross-restaurant mismatches between table, restaurant, category, and menu item context
- [ ] T036 [P4] Add validation rules that prevent client-supplied user identifiers from being used for cart or order ownership

## Phase 5: Services

**Goal**: Implement the backend business logic in services while keeping controllers thin.

**Independent Test**: Service methods can resolve tables, return public menus, manage authenticated
carts, and submit valid orders without controller-owned business logic.

- [ ] T037 [P5] Add or extend a public table resolution service interface in the current application service-contract location
- [ ] T038 [P5] Implement the public table resolution service in the infrastructure or service implementation layer to resolve table tokens and validate active-state rules
- [ ] T039 [P5] Add or extend a public menu retrieval service interface for browse-only menu access by resolved table context
- [ ] T040 [P5] Implement the public menu retrieval service to load restaurant summary, menu categories, and active menu items for a valid table token
- [ ] T041 [P5] Add or extend an authenticated cart service interface for current-cart retrieval, add-to-cart, update-cart-item, and remove-cart-item operations
- [ ] T042 [P5] Implement the authenticated cart service using the chosen cart strategy and enforce user, table, item, and quantity validation
- [ ] T043 [P5] Add or extend an order submission service interface for converting a valid authenticated cart into a persisted order
- [ ] T044 [P5] Implement the order submission service, including cart loading, revalidation, order creation, order-item creation, and transaction handling
- [ ] T045 [P5] Initialize new orders with status `Received` during order submission and keep the remaining statuses available for later phases

## Phase 6: API Endpoints

**Goal**: Expose the planned public and authenticated backend endpoints with thin controllers only.

**Independent Test**: Public endpoints support browse-only access and authenticated endpoints support
cart and order workflows using service calls.

- [ ] T046 [P6] Add public endpoint for table token resolution in the existing API controller structure
- [ ] T047 [P6] Add public endpoint for table menu retrieval in the existing API controller structure
- [ ] T048 [P6] Add authenticated endpoint for getting the current cart for a resolved table token
- [ ] T049 [P6] Add authenticated endpoint for adding an item to the cart for a resolved table token
- [ ] T050 [P6] Add authenticated endpoint for updating a cart item for a resolved table token
- [ ] T051 [P6] Add authenticated endpoint for removing a cart item for a resolved table token
- [ ] T052 [P6] Add authenticated endpoint for submitting an order from the current cart for a resolved table token
- [ ] T053 [P6] Ensure all new controllers or controller actions delegate directly to services and keep auth, validation, and response handling aligned with current API conventions

## Phase 7: Business Rules And Protection

**Goal**: Enforce the ordering rules and protect the current system from invalid or unauthorized use.

**Independent Test**: Guests are browse-only, authenticated ordering stays user-owned, and all
restaurant-table-menu consistency rules are enforced.

- [ ] T054 [P7] Ensure public endpoints remain browse-only and do not expose any cart or order mutation behavior
- [ ] T055 [P7] Ensure guests cannot create carts, modify carts, view authenticated cart state, or submit orders
- [ ] T056 [P7] Ensure only authenticated users can submit orders and that submitted orders always use a valid server-derived `UserId`
- [ ] T057 [P7] Ensure menu items can be added to cart only when they belong to the same restaurant as the resolved table
- [ ] T058 [P7] Ensure the resolved table always belongs to the restaurant context used for public menu and authenticated ordering flows
- [ ] T059 [P7] Ensure unavailable or inactive menu items cannot be added to cart or submitted in an order
- [ ] T060 [P7] Ensure order submission revalidates cart contents before creating `Order` and `OrderItem` records
- [ ] T061 [P7] Ensure the feature does not alter existing OTP, JWT, restaurant approval, or current account behavior while integrating the new endpoints and services

## Phase 8: Testing And Regression

**Goal**: Verify the new backend flows and guard existing system behavior from regression.

**Independent Test**: Public browsing, authenticated cart flow, order submission, and existing auth
and restaurant behavior all work as expected after the feature is added.

- [ ] T062 [P8] Test public table token resolution and public menu browsing for a valid table token
- [ ] T063 [P8] Test that guests are blocked from add-to-cart, update-cart, remove-cart, view-cart, and submit-order actions
- [ ] T064 [P8] Test authenticated user cart flow for add, update, remove, and current-cart retrieval
- [ ] T065 [P8] Test authenticated order submission from a valid non-empty cart and verify order plus order-item persistence
- [ ] T066 [P8] Test invalid token handling for both public and authenticated table-based requests
- [ ] T067 [P8] Test invalid item handling, including unavailable menu items, cross-restaurant mismatches, and invalid quantities
- [ ] T068 [P8] Regression-check existing JWT authentication flows after adding the new authenticated endpoints
- [ ] T069 [P8] Regression-check existing OTP-related flows to confirm they remain unchanged
- [ ] T070 [P8] Regression-check restaurant ownership and approval-related flows to confirm they remain unchanged
- [ ] T071 [P8] Build the solution, apply migrations in a safe test path, and resolve any compile or integration issues introduced by this feature

---

## Dependencies And Execution Order

### Phase Dependencies

- Phase 1 must complete before implementation starts.
- Phase 2 depends on Phase 1 because entity design must align with existing domain and ownership rules.
- Phase 3 depends on Phase 2 because EF Core mapping requires finalized entity shapes.
- Phase 4 depends on Phase 1 and should align with Phase 2 before service implementation begins.
- Phase 5 depends on Phases 2-4 because services need stable models, mappings, and DTO contracts.
- Phase 6 depends on Phase 5 because controllers should only expose completed service behavior.
- Phase 7 depends on Phases 5-6 because protection rules must be enforced through final service and endpoint paths.
- Phase 8 depends on Phases 3-7 being complete.

### Parallel Opportunities

- T003-T007 can run in parallel during the analysis phase.
- T009-T016 can run in parallel once the domain model direction is confirmed.
- T019-T024 can run in parallel after the new entities are added.
- T027-T032 can run in parallel after the endpoint and service contract shapes are agreed.
- T037-T040 can run in parallel with T041-T045 if service ownership is split cleanly.
- T046-T052 can be split across public and authenticated controllers after services are ready.
- T062-T070 can be distributed across verification tasks once implementation is complete.

## Recommended First Slice

Start with the smallest safe vertical slice:

1. T001-T008 to confirm the current architecture, auth flow, and restaurant ownership relationship.
2. T009-T017 to establish the new domain model and status model.
3. T018-T026 to wire the models into EF Core and produce migrations.
4. T027-T036 to finalize DTOs and validation boundaries.
5. T037-T045 to implement service-owned business logic.
6. T046-T061 to expose endpoints and enforce protection rules.
7. T062-T071 to verify the new flows and guard existing system behavior.

This keeps the work additive, backend-only, and aligned with the existing Smart Dining System.
