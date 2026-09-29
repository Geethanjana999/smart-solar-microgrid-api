# Smart Solar Microgrid Trading System API

ASP.NET Core Web API using MongoDB. All business rules are in services; controllers expose REST endpoints only.

## Setup and run

1. Install/start MongoDB locally, or use MongoDB Atlas.
2. Copy `appsettings.example.json` to the untracked `appsettings.json`, or set `MongoDb__ConnectionString`, `MongoDb__DatabaseName`, and `Jwt__Key` environment variables. Set `Jwt__Key` to a long secret outside development.
3. Run `dotnet restore` then `dotnet run` from this project directory.
4. Browse to `/swagger` (normally `https://localhost:xxxx/swagger`).

The startup seed creates Mongo indexes and test data. Test password for all accounts is `Password123!`: `backoffice` (Backoffice), `operator` (GridOperator), and `199012345678` (Prosumer).

## Endpoint groups

- `POST /api/auth/login`
- `GET|POST|PUT /api/users` (Backoffice)
- `POST /api/prosumers/register`, `GET|PUT /api/prosumers/me`, `POST /api/prosumers/me/deactivation-request`
- `GET|POST|PUT|DELETE /api/stations`
- `GET|POST|PUT|DELETE /api/slots`
- `GET|POST|PUT|DELETE /api/reservations` (Prosumer)
- `POST /api/operator/verify-qr`, `POST /api/operator/reservations/{id}/finalize`
- `GET /api/dashboard`

Use `Authorization: Bearer <token>` after login. Booking is restricted to the next seven days; changing/cancelling requires 12 hours' notice. A station or slot with active reservations cannot be deactivated/deleted.

## IIS deployment

Install the .NET Hosting Bundle on the server. Publish with `dotnet publish -c Release -o ./publish`; create an IIS site pointing at `publish`, use an app pool with **No Managed Code**, and set the MongoDB/JWT values as IIS environment variables. Ensure MongoDB/Atlas is reachable from the server.
