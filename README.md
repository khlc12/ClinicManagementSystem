# Clinic Management System

## Project Overview
The Clinic Management System is an offline Windows desktop application developed for academic demonstration of core clinic operations.  
It centralizes patient records, appointment scheduling, consultations, billing, medicine inventory, and operational reporting in one local workspace.

This project is designed for two audiences:
- **Instructor/Panel**: to evaluate workflow completeness, role-based access, and system usability.
- **Developers**: to review implementation structure, technology choices, and maintainability.

## Technology Stack
- **Language**: C#
- **Framework**: .NET `net10.0-windows`
- **UI**: Windows Forms (WinForms)
- **Database**: SQLite (local file-based database)
- **Data Access Package**: `Microsoft.Data.Sqlite`
- **Architecture Style**: layered desktop app (`UI`, `Services/Business Rules`, `Data`, `Models`)
- **Operation Mode**: fully offline (no internet dependency for core features)

## Default Login Accounts
The application seeds default accounts for initial access and testing.

| Role | Username | Password |
|---|---|---|
| Administrator | `admin` | `admin123` |
| Doctor | `doctor` | `doc123` |
| Receptionist | `reception` | `recep123` |

## Role Permissions
The system applies role-based module visibility and edit permissions.

| Module | Administrator | Doctor | Receptionist |
|---|---|---|---|
| Dashboard | View | View | View |
| Patients | View / Create / Edit / Delete | View only | View / Create / Edit / Delete |
| Patient History | View | View | View |
| Appointments | View / Create / Edit / Delete | View only | View / Create / Edit / Delete |
| Consultations | View / Create / Edit / Delete | View / Create / Edit / Delete | Not visible |
| Billing | View / Create / Edit / Delete | Not visible | View / Create / Edit / Delete |
| Medicines | View / Create / Edit / Delete | View only | Not visible |
| Reports | View / Export | View / Export | View / Export |
| Users | View / Create / Edit / Delete | Not visible | Not visible |

## Notes
- The application stores operational data locally on the machine (per-user app data directory).
- Reporting supports date-range preview and CSV export.

## Developer Study Guides
- `CODEBASE_GUIDE.md` - full folder/file architecture guide.
- `CODE_WALKTHROUGH.md` - step-by-step runtime feature flows for debugging and study.

## Presentation Guide
- `DEMO_GUIDE.md` - 10-15 minute live demo script with role-switch flow and speaker notes.
