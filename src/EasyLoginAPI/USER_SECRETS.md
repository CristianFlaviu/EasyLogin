# User Secrets

Local-only config store. Keeps secrets (SMTP passwords, API keys, connection strings) out of `appsettings.json` and git.

## Why

- `appsettings.Development.json` is committed → never put real secrets there
- User Secrets file lives outside repo, per developer
- Loaded automatically in `Development` environment, **overrides** `appsettings.*.json`

## Location

Project: `EasyLoginAPI`
UserSecretsId: `ceb148fc-9570-4e73-bccc-7c36364e3abe`

File path (Windows):
```
%APPDATA%\Microsoft\UserSecrets\ceb148fc-9570-4e73-bccc-7c36364e3abe\secrets.json
```

Open in editor:
```powershell
notepad "$env:APPDATA\Microsoft\UserSecrets\ceb148fc-9570-4e73-bccc-7c36364e3abe\secrets.json"
```

## Config Precedence (highest wins)

1. Command-line args
2. Environment variables
3. **User Secrets** (Development only)
4. `appsettings.{Environment}.json`
5. `appsettings.json`

A value in User Secrets overrides the same key in `appsettings.Development.json`.

## CLI Commands

Run from project folder (`EasyLoginAPI/`):

```powershell
# List all secrets
dotnet user-secrets list

# Set a value (use : for nested keys)
dotnet user-secrets set "Email:SmtpPassword" "plgwqpixnucbdmrr"

# Remove a single key
dotnet user-secrets remove "Email:SmtpPassword"

# Wipe all secrets for this project
dotnet user-secrets clear
```

Or pass `--project`:
```powershell
dotnet user-secrets list --project c:\Repos\EasyLogin\src\EasyLoginAPI\EasyLoginAPI
```

## File Format

Flat JSON, colon-separated keys:

```json
{
  "Email:SmtpPassword": "plgwqpixnucbdmrr",
  "Jwt:Key": "dev-only-key",
  "ConnectionStrings:DefaultConnection": "Server=..."
}
```

Nested objects also valid:
```json
{
  "Email": {
    "SmtpPassword": "plgwqpixnucbdmrr"
  }
}
```

## Required Secrets for EasyLoginAPI

| Key | Purpose |
|-----|---------|
| `Email:SmtpPassword` | Gmail App Password (16 chars, no spaces) |
| `ConnectionStrings:DefaultConnection` | SQL Server password (if not in appsettings) |
| `Jwt:Key` | Override dev JWT signing key |
| `ADMIN_PASSWORD` | Seed admin password |

## Production

User Secrets are **Development-only**. In prod use:
- Environment variables (`Email__SmtpPassword`, double underscore = nested)
- Azure Key Vault / AWS Secrets Manager
- Docker secrets / `.env` file (already used by `docker-compose.yml`)

## Troubleshooting

**Stale value after edit?** Restart the API. Config loads at startup.

**Watch window shows different value than appsettings?** Secrets override is active. Check with `dotnet user-secrets list`.

**`UserSecretsId not found`?** Ensure `<UserSecretsId>` element exists in `EasyLoginAPI.csproj`.
