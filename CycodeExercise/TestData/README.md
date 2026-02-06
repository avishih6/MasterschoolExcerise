# Test Data for Manual Testing

## Quick Test Commands

### 1. Test Vulnerable Packages (deep-override & mustache)

```bash
curl -X POST http://localhost:5095/api/v1/scan \
  -H "Content-Type: application/json" \
  -d '{
    "ecosystem": "npm",
    "fileContent": "ewogICJuYW1lIjogIk15IEFwcGxpY2F0aW9uIiwKICAidmVyc2lvbiI6ICIxLjAuMCIsCiAgImRlcGVuZGVuY2llcyI6IHsKICAgICJkZWVwLW92ZXJyaWRlIjogIjEuMC4xIiwKICAgICJtdXN0YWNoZSI6ICIyLjEuMSIKICB9Cn0K"
  }'
```

**Expected:** Returns vulnerabilities for both packages

### 2. Test Safe Package (underscore)

```bash
curl -X POST http://localhost:5095/api/v1/scan \
  -H "Content-Type: application/json" \
  -d '{
    "ecosystem": "npm",
    "fileContent": "ewogICJuYW1lIjogIk15IEFwcGxpY2F0aW9uIiwKICAidmVyc2lvbiI6ICIxLjAuMCIsCiAgImRlcGVuZGVuY2llcyI6IHsKICAgICJ1bmRlcnNjb3JlIjogIjEuMTIuMiIKICB9Cn0K"
  }'
```

**Expected:** Returns `{"vulnerablePackages":[]}`

### 3. Test Unsupported Ecosystem

```bash
curl -X POST http://localhost:5095/api/v1/scan \
  -H "Content-Type: application/json" \
  -d '{
    "ecosystem": "pip",
    "fileContent": "dGVzdA=="
  }'
```

**Expected:** Returns 400 with error message about unsupported ecosystem

### 4. Test Invalid Base64

```bash
curl -X POST http://localhost:5095/api/v1/scan \
  -H "Content-Type: application/json" \
  -d '{
    "ecosystem": "npm",
    "fileContent": "invalid!!base64"
  }'
```

**Expected:** Returns 400 with error message about invalid base64

## Encoding Your Own package.json

### macOS/Linux
```bash
base64 -i your-package.json
```

### Windows PowerShell
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("your-package.json"))
```

## Test Files

- `vulnerable-packages.json` - Contains the request for testing vulnerable packages
- `safe-packages.json` - Contains the request for testing safe packages
