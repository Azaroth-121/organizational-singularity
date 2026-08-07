using 'main.bicep'

param regionCode = 'eus2'
param location = 'eastus2'
param instance = '01'
param budgetContactEmails = [
  'iamkurtrainiersacay@gmail.com'
]

// postgresAdminPassword is intentionally NOT set here — pass it at deploy time via
// --parameters postgresAdminPassword=$env:OS_POSTGRES_ADMIN_PASSWORD (sourced from a
// GitHub Actions secret / Key Vault), never committed to source control.
