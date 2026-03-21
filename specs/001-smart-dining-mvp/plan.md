# Implementation Plan: Restaurant Menu Management With Images And IQD Pricing

**Branch**: `001-smart-dining-mvp` | **Date**: 2026-03-21 | **Spec**: `/specs/001-smart-dining-mvp/spec.md`  
**Input**: Feature specification from `/specs/001-smart-dining-mvp/spec.md`

## Summary

Implement backend-only restaurant menu management inside the existing Smart Dining System so an
approved `RestaurantOwner` can manage menu categories and menu items for their own restaurant.

The planned implementation adds:

- restaurant-scoped menu categories
- restaurant-scoped menu items
- item image URLs
- decimal IQD pricing
- approved-owner-only menu management
- seed data for 3 restaurants with categories and items

The implementation explicitly excludes:

- frontend work
- architecture redesign
- payment work
- changes to OTP, JWT, login, or restaurant approval behavior

The design extends the current ASP.NET Core Web API, EF Core, and PostgreSQL structure using thin
controllers, service-owned business logic, DTOs, validation, and EF Core migrations.

## Technical Context

**Language/Version**: C# on .NET 8  
**Primary Dependencies**: ASP.NET Core Web API, EF Core, Npgsql/PostgreSQL, JWT authentication, existing Iraq OTP integration  
**Storage**: PostgreSQL in Docker on port `5433`  
**Testing**: Solution build, targeted service and API verification, migration verification, and regression checks for current auth and approval flows  
**Target Platform**: Existing ASP.NET Core backend services  
**Project Type**: Existing layered backend solution using `Api`, `Application`, `Domain`, and `Infrastructure`  
**Performance Goals**: Menu-management operations should remain lightweight and responsive for normal owner usage  
**Constraints**: Keep current architecture; no redesign; backend only; no frontend; no breaking existing logic; controllers must stay thin; logic must stay in services; use DTOs and validation  
**Scale/Scope**: Add menu-category and menu-item management for restaurant owners within the current backend and database

## Constitution Check

*GATE: Must pass before implementation. Re-check after design and before coding starts.*

- Pass: The feature extends the existing ASP.NET Core architecture instead of replacing it.
- Pass: Controllers remain thin and orchestration-only.
- Pass: Business logic stays in services.
- Pass: DTOs and validation remain explicit boundaries.
- Pass: Database changes are planned through EF Core migrations only.
- Pass: OTP, JWT, login, and restaurant approval flows remain preserved.
- Pass: Scope is backend only and excludes frontend and unrelated redesign work.

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
|   `-- Extensions/
|-- SmartDiningSystem.Application/
|   |-- DTOs/
|   |-- Services/
|   |-- Interfaces/
|   `-- Validation/
|-- SmartDiningSystem.Domain/
|   |-- Entities/
|   `-- Enums/
`-- SmartDiningSystem.Infrastructure/
    |-- Data/
    |-- Configurations/
    |-- Seed/
    `-- Services/
```

**Structure Decision**: Extend the existing layered solution. API endpoints stay in `Api`, DTOs and
service contracts stay in `Application`, entities remain in `Domain`, and EF Core plus business
implementations stay in `Infrastructure`.

## Data Model Design

### Planned Entities

#### MenuCategory

Purpose:
Represents a category belonging to exactly one restaurant.

Planned fields:

- `Id`
- `RestaurantId`
- `Name`
- `Description`
- `DisplayOrder`

Planned responsibilities:

- group menu items under one restaurant
- support owner-managed ordering and menu structure
- support optional display ordering for later menu presentation

Planned relationships:

- one `Restaurant` to many `MenuCategory`
- one `MenuCategory` to many `MenuItem`

#### MenuItem

Purpose:
Represents a restaurant menu item under one category and one restaurant.

Planned fields:

- `Id`
- `RestaurantId`
- `CategoryId`
- `Name`
- `Description`
- `Price`
- `ImageUrl`
- `IsAvailable`
- `DisplayOrder`

Planned responsibilities:

- store the core menu item details used by current and future menu browsing
- store price as decimal IQD value
- store a required image URL
- support owner-controlled availability

Planned relationships:

- one `Restaurant` to many `MenuItem`
- one `MenuCategory` to many `MenuItem`
- one `MenuItem` belongs to exactly one `Restaurant`
- one `MenuItem` belongs to exactly one `MenuCategory`

### Relationship Rules

- one restaurant owns many categories
- one restaurant owns many menu items
- one category owns many items
- a menu item's category must belong to the same restaurant as the item
- an owner may manage only the restaurant linked to their own account context

## Field And Storage Design

### MenuCategory Field Direction

- `Name`: required, trimmed, safe length-limited text
- `Description`: optional, safe length-limited text
- `DisplayOrder`: optional integer used for ordered category display
- `RestaurantId`: required foreign key

### MenuItem Field Direction

- `Name`: required, trimmed, safe length-limited text
- `Description`: optional, safe length-limited text
- `Price`: required decimal value representing IQD
- `ImageUrl`: required string
- `IsAvailable`: required boolean for operational availability
- `DisplayOrder`: optional integer used for ordered item display
- `RestaurantId`: required foreign key
- `CategoryId`: required foreign key

### Price Rules

- price must be stored as decimal
- price represents IQD
- price must be greater than or equal to `0`
- currency text must not be stored in the database field
- EF Core configuration must define explicit decimal precision

## Authorization Design

### Allowed Actor

- approved `RestaurantOwner` only

### Authorization Rules

- menu management endpoints require authenticated owner access
- the owner must have a linked restaurant
- the linked restaurant must be approved
- the owner may only manage categories and items for their own restaurant
- guests cannot modify menu data
- authenticated `User`, `Admin`, and `MainAdmin` flows are not changed by this feature unless
  current role policy explicitly grants access, which is not planned here

### Ownership Enforcement Direction

- derive owner identity from JWT user context
- resolve the owner's linked restaurant server-side
- reject client attempts to act on another restaurant's menu data

## Service-Layer Design

### CategoryService

Purpose:
Own all menu-category business logic.

Planned responsibilities:

- create categories
- list categories for the owner's restaurant
- update categories
- delete categories if allowed by current business rules
- validate owner approval and ownership
- validate category input data

### MenuItemService

Purpose:
Own all menu-item business logic.

Planned responsibilities:

- create menu items
- list menu items for the owner's restaurant
- update menu items
- delete menu items if allowed by current business rules
- toggle item availability
- validate owner approval and ownership
- validate item input data
- validate category-to-restaurant consistency

## Validation Plan

### Category Validation

- category name is required
- category name should be trimmed and safe length-limited
- category must belong to the owner's restaurant context

### Item Validation

- item name is required
- item image URL is required
- item price must be greater than or equal to `0`
- category must exist
- category must belong to the same restaurant as the item
- item restaurant must match the owner's restaurant

### Authorization Validation

- user must be authenticated
- user must be a `RestaurantOwner`
- owner must have an approved restaurant
- owner must only manage their own restaurant

### Safety Validation

- reject invalid restaurant/category combinations
- reject invalid restaurant/item combinations
- reject non-owner access
- reject unapproved owner access

## Database Plan

### Required Schema Changes

Planned schema work:

- add `DbSet<MenuCategory>` if not already present
- add or update `DbSet<MenuItem>` if needed for the final model
- configure restaurant-to-category relationship
- configure restaurant-to-item relationship
- configure category-to-item relationship
- configure decimal precision for item price
- configure required `ImageUrl`
- add indexes and constraints as needed for restaurant/category/item lookups

### Migration Direction

- create additive EF Core migrations only
- avoid rewriting existing auth or approval tables
- keep schema changes focused on menu management

## Seed Data Plan

Seed requirements:

- 3 restaurants
- each restaurant has categories
- each category has items
- each item includes image URL and decimal IQD price

Seed design direction:

- seed data should align with current database initialization style
- seed records should respect current restaurant ownership and approval expectations
- seeded values should be valid for testing and API verification

## Compatibility Strategy

This feature extends the current system without rewriting it.

Compatibility direction:

- no changes to auth system
- no changes to approval system
- no changes to OTP flow
- no changes to JWT token issuance behavior
- no changes to current login flow
- menu management is added as a focused extension to restaurant-owner capabilities

Expected integration behavior:

- existing auth endpoints remain unchanged
- existing restaurant approval process remains authoritative
- existing public restaurant visibility rules remain intact
- new menu-management logic lives beside current restaurant logic rather than replacing it

## Implementation Direction For Next Tasks Phase

Recommended execution order:

1. Finalize `MenuCategory` and `MenuItem` domain shape.
2. Add or update EF Core mappings, relationships, and decimal precision rules.
3. Add additive migration(s).
4. Add DTOs and validation models for category and item management.
5. Implement `CategoryService`.
6. Implement `MenuItemService`.
7. Add thin owner-only API endpoints for category and item management.
8. Add seed data for 3 restaurants with categories and items.
9. Verify compatibility with auth and approval flows.

## Complexity Tracking

No constitution violations are expected in this plan.

Clarifications:

- This plan is backend only.
- No frontend work is included.
- No redesign is included.
- Existing auth and approval behavior remain unchanged.
