# DemoTradeLab web

React and TypeScript dashboard for the DemoTradeLab ASP.NET Core API. The UI displays fictional or manually entered demo-trade data only.

## Run locally

Start the API from the repository root:

```powershell
dotnet run --project src/DemoTradeLab.Api
```

In a second terminal:

```powershell
cd web/demotrade-lab-web
npm install
npm run dev
```

Open `http://localhost:5173`. The Vite development server proxies `/api` to `http://localhost:5122`, so the browser uses one origin and the backend does not need a development-only CORS policy.

## Commands

```powershell
npm run dev
npm run lint
npm run build
npm run preview
```

`npm run build` performs TypeScript checking before creating the optimized `dist` output.

## API configuration

The default development setup uses the Vite proxy. When hosting the frontend separately, copy `.env.example` to a local `.env` and set:

```text
VITE_API_BASE_URL=https://your-api-host
```

Do not commit environment-specific `.env` files.

## Source organization

```text
src/
|-- api/          # HTTP client and response types
|-- components/   # Presentational dashboard sections
|-- hooks/        # Request lifecycle and cancellation
|-- utils/        # Display formatting
|-- App.tsx       # Page composition and UI state
`-- index.css     # Responsive visual system
```

The frontend treats server analytics as authoritative. C# calculates monetary totals with `decimal`; JavaScript numbers are used only to render the returned values and chart coordinates.
