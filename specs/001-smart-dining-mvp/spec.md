# Feature Specification: Restaurant Menu Management

**Feature Branch**: `001-smart-dining-mvp`  
**Created**: 2026-03-18  
**Updated**: 2026-03-21  
**Status**: Draft  
**Input**: User description: "Allow approved RestaurantOwner to manage a full menu for their restaurant."

## Feature Overview

This feature adds backend-only restaurant menu management to the existing Smart Dining System so an
approved `RestaurantOwner` can manage menu categories and menu items for the restaurant they own.

This work MUST extend the current ASP.NET Core Web API architecture and MUST NOT redesign the
project. Controllers must remain thin, business logic must stay in services, DTOs and validation
must be used, and the current OTP, JWT, login, role, restaurant approval, and `/mainadmin`
behavior must remain intact.

This phase is explicitly backend only. It does not include frontend implementation, payment, or any
architecture rewrite.

## Scope

This feature is limited to:

- managing `MenuCategory` records per restaurant
- managing `MenuItem` records per restaurant and category
- storing item prices as decimal IQD values
- storing required image URLs for menu items
- restricting menu management to approved `RestaurantOwner` accounts
- ensuring each owner can manage only the menu of their own restaurant
- adding seed data for 3 restaurants, each with categories and items

Out of scope for this phase:

- treating this as a new project
- redesigning the architecture
- changing OTP integration behavior
- changing JWT login behavior
- changing restaurant approval flow behavior
- guest or normal user menu-management access
- payment handling
- frontend implementation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approved RestaurantOwner creates and manages menu categories (Priority: P1)

An approved `RestaurantOwner` can create and manage menu categories for the restaurant they own.

**Why this priority**: Menu structure starts with categories. Without category management, menu
items cannot be organized safely for the restaurant.

**Independent Test**: An approved owner can create one or more categories for their own restaurant,
while non-approved owners and other roles cannot.

**Acceptance Scenarios**:

1. **Given** an authenticated `RestaurantOwner` whose restaurant is approved, **When** they create
   a valid menu category for their restaurant, **Then** the category is stored successfully.
2. **Given** an authenticated `RestaurantOwner`, **When** they attempt to create or update a menu
   category for another owner's restaurant, **Then** the request is rejected.
3. **Given** a `RestaurantOwner` whose restaurant is not approved, **When** they attempt to manage
   menu categories, **Then** the request is rejected.

---

### User Story 2 - Approved RestaurantOwner adds menu items with price and image (Priority: P1)

An approved `RestaurantOwner` can add menu items under their restaurant categories with a valid IQD
price and a required image URL.

**Why this priority**: The feature's core value is enabling owners to build a complete menu with
category assignment, pricing, and images.

**Independent Test**: An approved owner can create a menu item with valid category, price, and
image URL under their own restaurant; invalid or cross-restaurant relationships are rejected.

**Acceptance Scenarios**:

1. **Given** an approved `RestaurantOwner` has a valid category in their restaurant, **When** they
   add a menu item with required name, non-negative decimal price, and required image URL, **Then**
   the item is stored successfully.
2. **Given** a menu item request uses a category that belongs to a different restaurant, **When**
   the owner submits the item, **Then** the request is rejected.
3. **Given** a menu item request omits the image URL or uses an invalid price value, **When** the
   request is submitted, **Then** validation fails with a clear error response.

---

### User Story 3 - Approved RestaurantOwner updates menu item availability (Priority: P1)

An approved `RestaurantOwner` can update whether a menu item is available for their restaurant.

**Why this priority**: Availability changes are a core operational need even before broader menu
editing workflows become more advanced.

**Independent Test**: An approved owner can toggle availability for one of their own menu items,
and the updated state is persisted.

**Acceptance Scenarios**:

1. **Given** an approved `RestaurantOwner` owns a restaurant menu item, **When** they update the
   item availability, **Then** the stored `isAvailable` state changes successfully.
2. **Given** a user who does not own the restaurant attempts to update item availability, **When**
   the request is made, **Then** the request is rejected.
3. **Given** a guest or normal authenticated user attempts to update a menu item, **When** the
   request is made, **Then** the request is rejected.

---

### User Story 4 - System stores prices in IQD consistently and seeds valid sample menus (Priority: P2)

The backend stores menu prices as numeric IQD values and includes valid seed data for restaurants,
categories, and menu items.

**Why this priority**: Consistent price storage and valid seed data are important for correctness,
testing, and future restaurant/menu browsing features.

**Independent Test**: Seeded restaurants exist with categories and items, and all seeded menu item
prices are stored as numeric decimal IQD values with image URLs.

**Acceptance Scenarios**:

1. **Given** menu items are stored in the database, **When** price fields are reviewed, **Then**
   they are stored as decimal numeric values representing IQD without currency text.
2. **Given** seed data is applied, **When** the database is inspected, **Then** 3 restaurants exist
   with categories and menu items under each.
3. **Given** seeded menu items exist, **When** they are reviewed, **Then** each item includes a
   price and an image URL.

## Edge Cases

- What happens when an approved owner tries to create a category with an empty name?
- What happens when two categories in the same restaurant use the same name?
- What happens when a menu item is assigned to a category from another restaurant?
- What happens when a non-approved `RestaurantOwner` attempts any menu-management action?
- What happens when an owner account exists but has no linked restaurant record?
- What happens when a menu item price is negative?
- What happens when a menu item image URL is empty?
- What happens when guests or normal users attempt create, update, or delete actions?
- What happens when seed data is applied multiple times?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST implement this feature within the existing ASP.NET Core Web API
  project and MUST NOT treat the work as a new project.
- **FR-002**: The system MUST preserve the existing architecture and MUST NOT redesign current auth,
  OTP, JWT, login, role, restaurant approval, or `/mainadmin` behavior.
- **FR-003**: Controllers related to menu management MUST remain thin and MUST delegate business
  logic to services.
- **FR-004**: Menu-management endpoints MUST use DTOs and validation for requests and responses.
- **FR-005**: Each restaurant MUST be able to have menu categories.
- **FR-006**: Each restaurant MUST be able to have menu items.
- **FR-007**: `MenuCategory` MUST include:
  - required `name`
  - optional `description`
  - optional `displayOrder`
  - `restaurantId`
- **FR-008**: `MenuItem` MUST include:
  - required `name`
  - optional `description`
  - required decimal `price`
  - required `imageUrl`
  - `isAvailable`
  - `displayOrder`
  - `restaurantId`
  - `categoryId`
- **FR-009**: Menu item price MUST be stored as a decimal numeric value.
- **FR-010**: Menu item price MUST represent IQD.
- **FR-011**: Menu item price MUST be greater than or equal to `0`.
- **FR-012**: Currency text MUST NOT be stored in the database price field.
- **FR-013**: Only approved `RestaurantOwner` accounts MUST be allowed to manage menu data.
- **FR-014**: A `RestaurantOwner` MUST only be allowed to manage the menu of their own restaurant.
- **FR-015**: Guests MUST NOT be able to modify menu categories or menu items.
- **FR-016**: Authenticated users without approved owner access MUST NOT be able to modify menu
  categories or menu items.
- **FR-017**: The system MUST validate that a menu category belongs to the same restaurant being
  managed.
- **FR-018**: The system MUST validate that a menu item belongs to the same restaurant as its
  assigned category.
- **FR-019**: The system MUST support creating menu categories for an approved owner's restaurant.
- **FR-020**: The system MUST support creating menu items for an approved owner's restaurant.
- **FR-021**: The system MUST support updating menu items, including availability state, for an
  approved owner's restaurant.
- **FR-022**: The system MUST reject menu-management actions when the owner's restaurant is not
  approved.
- **FR-023**: The system MUST add seed data for 3 restaurants.
- **FR-024**: Each seeded restaurant MUST have categories.
- **FR-025**: Each seeded category MUST have menu items.
- **FR-026**: Each seeded menu item MUST include a valid IQD price and an image URL.
- **FR-027**: This phase MUST remain backend only and MUST NOT require frontend implementation.

### Non-Functional Requirements

- **NFR-001**: The feature design MUST align with the existing ASP.NET Core, EF Core, and
  PostgreSQL architecture already used in the project.
- **NFR-002**: Menu-management business rules MUST be owned by services rather than controllers.
- **NFR-003**: Validation rules MUST be explicit and safe for authorization, ownership, price, and
  relationship checks.
- **NFR-004**: The feature MUST preserve current system behavior outside this feature scope.
- **NFR-005**: The implementation MUST remain maintainable and extend the current project structure
  rather than introducing parallel architecture.

### Assumptions

- The existing Smart Dining System already contains working ASP.NET Core Web API, EF Core,
  PostgreSQL, JWT authentication, Iraq OTP integration, existing roles, and restaurant approval
  behavior.
- A restaurant becomes owner-manageable for menu operations only when its approval state allows it
  to be visible in the current system.
- A restaurant owner is linked to exactly their own restaurant context for private management
  actions according to current system behavior.
- Seed data should be additive and valid within the current project's database initialization flow.

## Data Model Expectations

### Key Entities *(include if feature involves data)*

- **Restaurant**: Existing restaurant entity that owns menu categories and menu items.
- **MenuCategory**: Category entity for a specific restaurant with name, optional description,
  optional display order, and restaurant linkage.
- **MenuItem**: Menu item entity for a specific restaurant and category with name, description,
  decimal IQD price, required image URL, availability state, and display order.
- **UserAccount**: Existing authenticated account entity used to enforce approved
  `RestaurantOwner` authorization and ownership checks.

### Relationships (conceptual)

- One `Restaurant` **has many** `MenuCategory` records.
- One `Restaurant` **has many** `MenuItem` records.
- One `MenuCategory` **belongs to** one `Restaurant`.
- One `MenuCategory` **has many** `MenuItem` records.
- One `MenuItem` **belongs to** one `Restaurant`.
- One `MenuItem` **belongs to** one `MenuCategory`.
- One approved `RestaurantOwner` **manages** only the menu data of their linked restaurant.

## Acceptance Criteria

- Only approved restaurant owners can manage menu data.
- A restaurant owner can manage only their own restaurant menu.
- Each restaurant has its own menu categories and menu items.
- Menu items include both price and image URL.
- Price is stored as numeric decimal IQD without currency text in the database.
- Guests and non-owner users cannot modify menu data.
- Seed data exists for 3 restaurants with categories and menu items.
- Seeded items have valid price and image URL values.
- The feature remains backend only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An approved `RestaurantOwner` can create menu categories for their own restaurant.
- **SC-002**: An approved `RestaurantOwner` can add menu items with valid decimal IQD price and
  image URL for their own restaurant.
- **SC-003**: An approved `RestaurantOwner` can update menu item availability successfully.
- **SC-004**: Unauthorized users, guests, and non-approved owners are blocked from menu-management
  actions.
- **SC-005**: Menu item prices are stored as numeric IQD values without currency text.
- **SC-006**: Seed data provides 3 restaurants with valid categories and items.
- **SC-007**: Existing OTP, JWT, login, and restaurant approval flows remain behaviorally intact
  after this feature is added.
