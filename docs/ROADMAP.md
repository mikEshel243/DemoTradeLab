# Roadmap

## Milestone 0 - Repository foundation

Status: Complete

- [x] Validate .NET and Git tooling
- [x] Create and reference Api, Core, Infrastructure, UnitTests, and IntegrationTests projects
- [x] Configure repository ignores for build output, local databases, uploads, and secrets
- [x] Add repository and architecture documentation
- [x] Add a controller-based health endpoint
- [x] Add an integration test for the health endpoint
- [x] Build and test the backend solution

## Milestone 1 - Trade domain

Status: Complete

- [x] Trade domain model and validation
- [x] Unit tests for trade invariants
- [x] EF Core with SQLite and migrations
- [x] Integration test for migration and persistence mapping
- [x] Fictional seed data
- [x] Trade CRUD endpoints
- [x] Integration tests for CRUD endpoints

## Milestone 2 - Analytics API

Status: Complete

- [x] Dashboard statistics
- [x] Filtering and sorting
- [x] Instrument summaries
- [x] Profit/loss timeline
- [x] Calculation edge-case tests

## Milestone 3 - React dashboard

Status: Planned

- React, TypeScript, and Vite application
- Summary, trade table, and trade details
- Loading, error, empty, and responsive states

## Milestone 4 - Importing

Status: Planned

- Generic importer boundary
- Safe CSV or JSON import
- Duplicate prevention
- No broker-specific parser until an anonymized sample is provided

## Milestone 5 - Reliability simulator

Status: Planned

- Explicit order state machine
- Idempotency and retry safety
- Simulated failure and concurrency scenarios
- Audit events and reconciliation

## Milestone 6 - Final polish

Status: Planned

- Documentation and screenshots
- Architecture diagram
- Test-coverage improvements
- Interview demonstration script
