# ChatApp

A small ASP.NET Core 10.0 chat API with a Vue frontend. This README focuses on getting the project running locally (Docker and native), where the static frontend lives, and how to change the app port.

**Quick links:**

- Backend project: `Chatter/`
- Frontend project: `frontend/` (Vite + Vue 3 + TypeScript)
- Built frontend (served by backend): `Chatter/wwwroot/`
- **The first user is always the admin**
- **Admin user behavior:**
  - The application ensures an admin user exists on startup. If no admin or matching account exists,
    a default admin will be created.
  - Default credentials (change immediately in any non-development environment):
    - Username: `admin`
    - Email: `admin@admin.com`
    - Password: `admin`
  - To override these defaults, set environment variables before starting the app:
    - `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`
  - Security: Change the seeded admin password immediately after first run, and do not rely on
    these defaults in production. Consider deleting or rotating the seeded admin account before
    deploying a production instance.
  - Note: Migrations may also contain a development seed in this repository. The startup seeder
    will skip creating a user if an admin already exists.
  - Admin panel: visit `/admin` after logging in (requires an admin account).

---

## Native (Windows / PowerShell)

This repo includes `build.py` which automates building the frontend and copying `frontend/dist` into `Chatter/wwwroot`.

1. Build frontend and copy to backend:

```powershell
# run from repository root
python .\build.py
```

2. Restore, apply migrations and run backend:

```powershell
cd .\Chatter
dotnet restore
dotnet ef database update
dotnet run
```

The backend will serve the static files present in `Chatter/wwwroot`.

---

## Changing the application's port

There are multiple ways to change the port the ASP.NET app listens on. Two reliable approaches are:

1. Trough the admin page(persistent, requires app reboot)

As an admin you can go to the admin page and change the ports there

2. Configure Kestrel in `appsettings.json` (persistent)

If you prefer to set the port in `Chatter/appsettings.json`, add a `Kestrel` endpoints section. Example (adds an HTTP endpoint on port 9090):

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:9090"
      },
      "Https": {
        "Url": "http://*:9443"
      }
    }
  }
}
```

3. Environment variable (easy, recommended for temporary runs)

- Set `ASPNETCORE_URLS` before running `dotnet run` (PowerShell example):

```powershell
$env:ASPNETCORE_URLS = 'http://localhost:9090'
cd .\Chatter
dotnet run
```

This makes Kestrel listen on `http://localhost:9090`. You can also set `ASPNETCORE_URLS` in a Docker container or in your hosting environment.

Notes about `appsettings.json`:

- The `Kestrel` configuration in `appsettings.json` is read by the default host if Kestrel is used and the app's `Program.cs` does not explicitly override Kestrel configuration. If you changed host code, ensure it reads configuration for Kestrel.
- `http://*:9090` listens on all network interfaces. Use `http://localhost:9090` for local-only binding.

3. Development: `launchSettings.json`

- For development runs inside Visual Studio / `dotnet run` with profiles, edit `Chatter/Properties/launchSettings.json` to change the application URL used by the chosen profile. This is convenient for debugging but is not used in Docker containers.

Which method to use?

- For quick local testing, use `ASPNETCORE_URLS` environment variable.
- For a permanent configuration in a self-hosted server, prefer configuring Kestrel in `appsettings.json` or programmatically in `Program.cs`.

---

## Database migrations

If you change EF Core models, run:

```powershell
cd .\Chatter
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

The repository already contains a `Migrations/` folder with recent migrations.

---

## API (summary)

- `GET  /api/auth/check` — returns authentication status
- `POST /api/auth/login` — login (email or username + password)
- `POST /api/auth/logout` — logout
- `POST /api/messages` — send a message
- `GET  /api/messages` — fetch messages
- `POST /api/users` — create user

Refer to the `Chatter/Controllers/` folder for controller implementations and exact request/response shapes.

---

**Admin panel access**

The app will create an admin automatically on startup if one is not present. For local development this
is convenient, but for production you must secure or rotate the account immediately:

- Override credentials with environment variables: `ADMIN_USERNAME`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`.
- After initial login, change the seeded password right away via the profile or Admin panel.
- To remove the seeded admin from an existing SQLite database (dev only), you can run:

```powershell
# from the project folder containing the SQLite DB
sqlite3 ./Chatter/data/chatter.db "DELETE FROM Users WHERE Email = 'admin@admin.com';"
```

Or use a database client to delete or update the user record. For production deployments prefer
explicit provisioning (scripts, secrets manager, or a one-time setup flow) rather than relying on
default credentials baked into the repo.

---

## Project structure (high level)

- `Chatter/` — ASP.NET backend (C#)
- `frontend/` — Vue 3 + TypeScript app (Vite)
- `build.py` — helper script: builds frontend and copies `frontend/dist` → `Chatter/wwwroot`

---

## Development tips

- When you edit frontend code, run `python .\build.py` to rebuild and copy assets before starting the backend (or use the frontend dev server for hot reload).
- Use `dotnet watch run` inside `Chatter/` for automatic backend reload during development.

---

## Contributing

1. Fork → branch → commit → push → PR

---

## License

The use as you please Licence.

---
