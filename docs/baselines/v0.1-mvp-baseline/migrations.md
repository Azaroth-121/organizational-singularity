# Migration state at baseline

Captured 2026-08-16 via:

```
dotnet ef migrations list \
  --project src/OrganizationalSingularity.Infrastructure \
  --startup-project src/OrganizationalSingularity.Api
```

All 9 migrations below are applied to the local development database as of this baseline — none pending.

1. `20260807052759_InitialCreate`
2. `20260807171229_AddMembershipTenantForeignKey`
3. `20260807200603_AddInvitations`
4. `20260810153836_RedesignAssessmentFrameworkForOiqV1`
5. `20260810155503_AddIntelligenceDebtRegister`
6. `20260812165849_AddIntelligenceDebtCategoryMapping`
7. `20260812170456_AddRoadmapInitiatives`
8. `20260812174507_HardenIntelligenceDebtMethodology`
9. `20260813142619_AddReassessmentLineageTracking`

Full schema resulting from these migrations: [`schema.sql`](./schema.sql) (`pg_dump --schema-only`, same database).
