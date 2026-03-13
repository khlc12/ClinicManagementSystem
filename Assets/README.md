# UI Overlay Image

Place your transparent PNG texture(s) here as:

- `Assets/header-overlay.png` (top app header)
- `Assets/dashboard-hero-overlay.png` ("Welcome back to the clinic floor")
- `Assets/login-hero-overlay.png` (login page left colored panel)
- `Assets/app-logo.png` (top-left brand badge logo)

Optional fallback:

- `Assets/hero-overlay.png`
- `Assets/clinic-logo.png`
- `Assets/logo.png`

Behavior:

- Header uses `header-overlay.png`, then falls back to `hero-overlay.png`.
- Dashboard hero uses `dashboard-hero-overlay.png`, then falls back to `hero-overlay.png`.
- Login hero uses `login-hero-overlay.png`, then falls back to `hero-overlay.png`.
- Header brand badge uses `app-logo.png`, then `clinic-logo.png`, then `logo.png`.
- If none exist, panels fall back to the built-in painted background.
