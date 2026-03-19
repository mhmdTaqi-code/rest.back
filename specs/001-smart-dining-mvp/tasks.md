# Tasks: MainAdmin Improvement + Hard Delete

**Input**: Design documents from `/specs/001-smart-dining-mvp/`  
**Prerequisites**: `plan.md`, `spec.md`

**Tests**: Validation is required because this feature changes developer control-panel account
management and adds permanent hard delete behavior while preserving OTP, JWT, and restaurant
approval compatibility.

**Organization**: Tasks are grouped into clear, implementation-ready phases covering design review,
DTOs/validation, service methods, hard delete safety, UI cleanup, and regression testing.

## Format: `[ID] [P?] [Phase] Description`

- **[P]**: Can run in parallel with other `[P]` tasks once dependencies are satisfied
- **[Phase]**:
  - `P1` review
  - `P2` DTOs + validation
  - `P3` service layer
  - `P4` hard delete safety
  - `P5` UI
  - `P6` testing

## Phase 1: Review Spec And Plan

**Goal**: Confirm implementation boundaries before any code changes.

**Independent Test**: The implementation team can point to the approved spec, plan, constraints,
and invariants before touching account-management code.

- [ ] T001 [P1] Review `spec.md` and confirm the feature scope is limited to MainAdmin cleanup and hard delete
- [ ] T002 [P1] Review `plan.md` and confirm the required UI structure, service ownership, and delete strategy
- [ ] T003 [P1] Review current MainAdmin account-management code paths and identify files for:
  - layout
  - accounts list
  - create/edit pages
  - controller
  - service
  - DTO/ViewModel models
- [ ] T004 [P1] Confirm compatibility constraints for OTP, JWT, and restaurant approval flows before implementation starts

## Phase 2: DTOs + Validation

**Goal**: Define clean UI/input models and validation rules for account management.

**Independent Test**: Create/edit/delete flows have explicit ViewModels/DTO-shaped models and
clear validation rules without binding EF entities directly to Razor forms.

- [ ] T005 [P2] Review existing MainAdmin account ViewModels/DTO-shaped models and identify needed extensions
- [ ] T006 [P2] Define or update the create-account model with all required fields for `User`, `RestaurantOwner`, and `Admin`
- [ ] T007 [P2] Define or update the edit-account model with supported editable fields
- [ ] T008 [P2] Define or update the delete-confirmation model/input shape for hard delete actions
- [ ] T009 [P2] Add validation rules for required fields, role validity, and any uniqueness checks used by account management
- [ ] T010 [P2] Add validation rules for activate/deactivate and delete request inputs where needed

## Phase 3: Service Layer

**Goal**: Keep all account-management behavior in the service layer.

**Independent Test**: The MainAdmin controller delegates all account-management operations to
service methods, and those methods cover the required CRUD/status behaviors.

- [ ] T011 [P3] Implement or refine `GetAllAccounts`
- [ ] T012 [P3] Implement or refine `GetAccountById`
- [ ] T013 [P3] Implement or refine `CreateAccount`
- [ ] T014 [P3] Implement or refine `UpdateAccount`
- [ ] T015 [P3] Implement or refine `ActivateAccount`
- [ ] T016 [P3] Implement or refine `DeactivateAccount`
- [ ] T017 [P3] Implement or refine `HardDeleteAccount`
- [ ] T018 [P3] Ensure all service methods use async EF Core access and return UI-friendly results/errors
- [ ] T019 [P3] Keep controllers thin by ensuring they only call service methods and handle redirects/messages

## Phase 4: Hard Delete Safety

**Goal**: Make permanent delete safe, explicit, and transaction-backed.

**Independent Test**: Eligible accounts are permanently removed, while unsafe deletes are rejected
without damaging relational integrity.

- [ ] T020 [P4] Add service-layer relational checks before hard delete
- [ ] T021 [P4] Add explicit `RestaurantOwner <-> Restaurant` relationship handling rules for hard delete
- [ ] T022 [P4] Add protected-account rules if certain accounts must not be hard deleted under defined safety conditions
- [ ] T023 [P4] Add transaction handling for multi-step hard delete operations
- [ ] T024 [P4] Return clear business-rule errors when hard delete is blocked
- [ ] T025 [P4] Verify hard delete removes records permanently rather than performing soft delete

## Phase 5: UI

**Goal**: Clean up the MainAdmin UI while preserving current architecture and behavior.

**Independent Test**: `/mainadmin` shows a cleaner sidebar/topbar layout, accounts table, create/edit
pages, delete modal, and clear alerts/badges using Bootstrap.

- [ ] T026 [P5] Update the shared MainAdmin layout to use a clean sidebar + topbar structure
- [ ] T027 [P5] Refine the accounts table page layout and action-button placement
- [ ] T028 [P5] Update the create page UI to match the cleaned MainAdmin design
- [ ] T029 [P5] Update the edit page UI to match the cleaned MainAdmin design
- [ ] T030 [P5] Add a delete confirmation modal for permanent delete actions
- [ ] T031 [P5] Add or refine alerts for success and error feedback
- [ ] T032 [P5] Add or refine role/status badges for clear account state visibility
- [ ] T033 [P5] Keep the UI simple, professional, and Bootstrap-based without dashboard complexity

## Phase 6: Testing

**Goal**: Verify account-management behavior and guard against regressions.

**Independent Test**: MainAdmin create/edit/delete flows work, hard delete succeeds or fails correctly,
and existing OTP/JWT/approval flows still behave as before.

- [ ] T034 [P6] Verify create-account flow for `User`, `RestaurantOwner`, and `Admin`
- [ ] T035 [P6] Verify edit-account flow persists valid changes
- [ ] T036 [P6] Verify activate/deactivate flows update account state correctly
- [ ] T037 [P6] Verify hard delete success path permanently removes eligible accounts
- [ ] T038 [P6] Verify hard delete failure path rejects unsafe deletions cleanly
- [ ] T039 [P6] Verify no regression in OTP-related flows
- [ ] T040 [P6] Verify no regression in JWT login/auth flows
- [ ] T041 [P6] Verify no regression in restaurant approval flow behavior
- [ ] T042 [P6] Build the solution and fix any compile or integration issues introduced by the feature work

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 must complete before implementation starts.
- Phase 2 depends on Phase 1 because model boundaries and validation rules must be agreed first.
- Phase 3 depends on Phase 2 because services need stable input/output models.
- Phase 4 depends on Phase 3 because hard delete rules live inside the account service workflow.
- Phase 5 can begin once the underlying service and model shapes are stable enough to drive the UI.
- Phase 6 depends on Phases 2-5 being complete.

### Parallel Opportunities

- T006-T010 can run in parallel once the review phase is complete.
- T011-T017 can be split among service methods after DTOs and validation are stable.
- T020-T024 can run in parallel after `HardDeleteAccount` has a base implementation path.
- T027-T033 can run in parallel after the shared layout direction is established.
- T034-T041 can be distributed as verification tasks once implementation is complete.

## Recommended First Slice

Start with the smallest safe vertical slice:

1. T001-T004 to lock scope and constraints
2. T005-T010 to finalize account-management DTOs and validation
3. T011-T019 to make service ownership explicit
4. T020-T025 to implement safe permanent hard delete
5. T026-T033 to clean up MainAdmin UI
6. T034-T042 to verify behavior and regressions

This keeps the work actionable, service-oriented, and compatible with the existing Smart Dining
system.
