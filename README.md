# Database Migrations Action

This action applies database migrations and idempotent scripts to a specified database.

## Inputs

### `connection-string-host`

**Required** The host of the database.

### `connection-string-port`

**Required** The port of the database.

### `connection-string-database`

**Required** The name of the database.

### `connection-string-username`

**Optional** The username for the database connection.

### `connection-string-password`

**Optional** The password for the database connection.

### `aws-sm-region`

**Optional** The AWS Secret Manager region.

### `aws-sm-db-credentials-key`

**Optional** The AWS Secret Manager key for database credentials in JSON format: `{ "username": "your-username", "password": "your-password" }`.

### `migrations-scripts-path`

**Required** The path to the migration scripts. Default is `./db/migrations-scripts`.

### `idempotent-scripts-path`

**Required** The path to idempotent scripts (procedures, functions, etc.) to be executed after migrations. Default is `./db/idempotent-scripts`.

## Example usage

```yaml
name: Apply migrations

on:
  workflow_dispatch:

jobs:
  test-migrations:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Run Migrations
        uses: Sergei-TOL/DbMigrationManager@v1
        with:
          connection-string-host: postgres
          connection-string-port: 5432
          connection-string-database: testdb
          # connection-string-username: postgres
          # connection-string-password: testpassword
          aws-sm-region: ca-central-1
          aws-sm-db-credentials-key: some-key
          migrations-scripts-path: "./db/migrations-scripts"
          idempotent-scripts-path: "./db/idempotent-scripts"
```
