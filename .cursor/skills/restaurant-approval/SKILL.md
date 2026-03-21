---
name: restaurant-approval
description: Use this skill when implementing restaurant onboarding, admin approval/rejection, moderation workflows, and restaurant visibility rules.
---

# Restaurant Approval Workflow Skill

Use this skill for any task related to restaurant onboarding and moderation.

## Business rules

- Restaurant owners do not become publicly visible immediately
- New restaurant submissions must be marked Pending
- Admin can Approve or Reject
- Rejection must include a reason
- Only Approved restaurants appear in public restaurant lists
- Owners can view their application status
- Admin actions should be auditable

## Suggested entities

- User
- Restaurant
- RestaurantApplicationReview or review fields on Restaurant

## Suggested fields

- ApprovalStatus
- RejectionReason
- ReviewedAt
- ReviewedByAdminId
- SubmittedAt

## API expectations

- Owner submits restaurant application
- Admin lists pending applications
- Admin approves application
- Admin rejects application with reason
- Public users can only see approved restaurants
