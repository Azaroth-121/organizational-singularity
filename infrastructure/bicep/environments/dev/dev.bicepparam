using 'main.bicep'

param regionCode = 'eus2'
param location = 'eastus2'
param instance = '01'
param budgetContactEmails = [
  'iamkurtrainiersacay@gmail.com'
]

// Deliberately not a literal here -- .bicepparam files must be complete (unlike JSON
// parameter files, a supplemental --parameters override on the CLI can't fill in a
// value missing from this file), so this reads from the environment at deploy time
// instead. Set OS_POSTGRES_ADMIN_PASSWORD before running `az deployment sub create`;
// never commit the actual value.
param postgresAdminPassword = readEnvironmentVariable('OS_POSTGRES_ADMIN_PASSWORD')
