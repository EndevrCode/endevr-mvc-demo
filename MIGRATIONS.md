# Database Migrations

After installing .NET 9 SDK and restoring packages, run:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

For development, the app uses SQLite (`hearthly.db` in project root).
For production, configure `DefaultConnection` in `appsettings.json` with your Azure SQL connection string.
