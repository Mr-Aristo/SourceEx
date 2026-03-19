# SourceEx Web Client

This folder contains a standalone Angular client shell for the SourceEx expense API and identity service.

## Why It Lives Separately

The backend is still under active testing, so the client is intentionally isolated from the `.NET` solution and worker projects. This lets the team shape the frontend without risking accidental changes to API, infrastructure, or solution wiring.

## What Is Already Wired

- Angular standalone application structure
- typed API models aligned with the current backend contracts
- token and refresh-token based session handling
- HTTP interceptors for bearer token and API version header support
- facades for auth and expense flows
- placeholder pages for:
  - login and registration through the identity module
  - expense creation
  - expense detail loading
  - expense approval

## What Is Intentionally Missing

- final UI design
- real forms and validation messages
- state persistence beyond the access token
- production-grade theming and component library

Those pieces should be added after the product interface is agreed on.

## Local Run

Install dependencies:

```bash
npm install
```

Start the Angular development server:

```bash
npm start
```

The development server uses `proxy.conf.json`, so browser calls are forwarded without changing the frontend code:

- `/api/v1.0/identity/*` -> `http://localhost:5001`
- `/api/*` -> `http://localhost:5000`
- `/health` -> `http://localhost:5000`

If your API runs on a different origin, update both:

- [environment.ts](src/environments/environment.ts)
- [proxy.conf.json](proxy.conf.json)

## Page Strategy

Each page intentionally contains:

- a minimal shell
- comments that explain which API model belongs to which future UI section
- small developer-only buttons to prove the API integration path

When the UI direction becomes clear, the team can replace the placeholder sections without rewriting the integration layer.
