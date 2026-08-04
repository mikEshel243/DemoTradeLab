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

Status: Planned

- Trade domain model and validation
- EF Core with SQLite and migrations
- Fictional seed data
- Trade CRUD endpoints
- Unit and integration tests

## Milestone 2 - Analytics API

Status: Planned

- Dashboard statistics
- Filtering and sorting
- Instrument summaries
- Profit/loss timeline
- Calculation edge-case tests

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
