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
name: Test Migrations

on:
  workflow_dispatch:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test-migrations:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: testdb
          POSTGRES_PASSWORD: testpassword
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v3

      - name: Run Migrations
        uses: Sergei-TOL/DbMigrationManager@v1
        with:
          connection-string-host: postgres
          connection-string-port: 5432
          connection-string-database: testdb
          connection-string-username: postgres
          connection-string-password: testpassword
          migrations-scripts-path: "./db/migrations-scripts"
          idempotent-scripts-path: "./db/idempotent-scripts"

      - name: Verify Migrations
        run: |
          PGPASSWORD=testpassword psql -h localhost -U postgres -d testdb -c "
            -- Check if the procedure and table was created
            CALL insert_into_test_table('test_record');
            SELECT * FROM test_table;
          "
```
