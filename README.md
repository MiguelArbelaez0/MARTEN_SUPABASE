# Marten + Supabase

> Email verification API built with .NET 10, ASP.NET Core, Marten, PostgreSQL/Supabase and MailKit.

Marten + Supabase is a backend-focused application that implements an end-to-end email verification workflow: secure code generation, SHA-256 hashing, persistence through Marten and PostgreSQL, email delivery through Gmail SMTP, expiration, attempt limits, and verification status management.

## 🧩 Technologies

### Backend

- .NET 10
- ASP.NET Core
- C#
- Marten 9.44.0
- REST API
- OpenAPI

### Persistence

- PostgreSQL
- Supabase
- Marten Document Store

### Email

- MailKit
- MimeKit
- Gmail SMTP
- STARTTLS

### Frontend

- HTML
- CSS
- JavaScript
- ASP.NET Core Static Files

### ⚙️ Configuration

- DotNetEnv
- Environment variables

## 🏗️ Architecture

The project separates web presentation, verification logic, document persistence, and email delivery.

```text
MARTEN_SUPABASE/
├── Models/
│   └── VerificationCode.cs
├── Services/
│   ├── IEmailService.cs
│   ├── SmtpEmailService.cs
│   └── VerificationCodeService.cs
├── wwwroot/
│   ├── index.html
│   ├── styles.css
│   └── app.js
├── Program.cs
├── MARTEN_SUPABASE.csproj
├── .env.example
├── .gitignore
└── README.md
```

### Architecture Flow

```text
User
   │
   ▼
Web Interface
HTML / CSS / JavaScript
   │
   │ HTTP / JSON
   ▼
ASP.NET Core
   │
   ├──────────────► Gmail SMTP
   │                    │
   │                    ▼
   │                  MailKit
   │
   ▼
VerificationCodeService
   │
   ▼
Marten
   │
   ▼
PostgreSQL / Supabase
```

## 🔄 How It Works

The system allows users to request a six-digit verification code for a Gmail account.

The main workflow is:

1. The user enters a Gmail address.
2. ASP.NET Core validates that the email ends with `@gmail.com`.
3. `VerificationCodeService` generates a random six-digit code.
4. The code is transformed using SHA-256.
5. A `VerificationCode` document is created.
6. The code hash is stored through Marten in PostgreSQL/Supabase.
7. The original code is sent to the email address through Gmail SMTP and MailKit.
8. The user enters the received code.
9. The system generates the hash of the submitted code again.
10. The submitted hash is compared with the stored hash.
11. If verification succeeds, the record is marked as used and verified.
12. The history endpoint can be used to retrieve the latest 20 verification records.

## 🗂️ Verification Model

Each `VerificationCode` record contains information related to the verification process:

- `Id` — Unique identifier.
- `Email` — Associated Gmail address.
- `CodeHash` — SHA-256 hash of the verification code.
- `CreatedAt` — Creation timestamp.
- `ExpiresAt` — Expiration timestamp.
- `Used` — Indicates whether the code has already been used.
- `Attempts` — Number of verification attempts.
- `Status` — Current verification status.
- `VerifiedAt` — Verification timestamp, when applicable.

## 📊 Verification States

The application uses the following internal status values:

| Status | Description |
|---|---|
| `Pendiente` | Code created and available for verification. |
| `Verificado` | Code successfully verified. |
| `Expirado` | Code validity period has expired. |
| `Bloqueado` | Maximum verification attempts reached. |
| `Reemplazado` | Code was replaced by a new verification request. |

> The status values remain in Spanish because they are actual values used by the application.

## 🔐 Verification Rules

- Accepted email addresses must belong to the `@gmail.com` domain.
- Verification codes contain 6 digits.
- Codes expire after 10 minutes.
- A maximum of 5 verification attempts is allowed.
- Previous unused codes are marked as `Reemplazado` when a new code is requested.
- The original verification code is not stored directly in the database; its SHA-256 hash is stored instead.
- After successful verification, the code is marked as used.

## 🌐 REST API

### `POST /auth/send-code`

Generates a new verification code and sends it to the specified Gmail address.

Example request:

```json
{
  "email": "user@gmail.com"
}
```

### `POST /auth/verify-code`

Verifies the code submitted by the user.

Example request:

```json
{
  "email": "user@gmail.com",
  "code": "123456"
}
```

### `GET /auth/history`

Returns the latest 20 verification records ordered by creation date in descending order.

The response includes information such as:

- ID
- Email
- Creation timestamp
- Expiration timestamp
- Verification attempts
- Status
- Verification timestamp

## 🗄️ Persistence with Marten

Marten is used as a document store on top of PostgreSQL.

The `VerificationCode` model is registered as a document through the ASP.NET Core configuration:

```text
ASP.NET Core
      │
      ▼
Marten IDocumentStore
      │
      ▼
PostgreSQL
      │
      ▼
Supabase
```

Records are created, queried, and updated through Marten sessions.

## 📧 Email Delivery

Verification codes are sent through `SmtpEmailService`.

The application uses:

- MailKit
- MimeKit
- Gmail SMTP
- Port `587`
- STARTTLS

SMTP credentials are loaded through environment variables and are not included in the source code.

## Configuration

### 1. Create `.env`

In the project root, create a file named:

```text
.env
```

Use `.env.example` as a reference:

```env
SUPABASE_CONNECTION_STRING="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your_email@gmail.com
SMTP_PASSWORD=your_app_password
SMTP_FROM=your_email@gmail.com
SMTP_FROM_NAME=Marten Supabase
```

Replace the placeholder values with your own credentials and configuration.

> **Important:** `.env` contains sensitive information and must remain local. It must not be committed to GitHub.

## ▶️ Running the Application

### Restore Dependencies

```bash
dotnet restore
```

### Run the Application

```bash
dotnet run --no-launch-profile
```

The application runs locally according to the available launch configuration.

During development, it can be accessed at:

```text
http://localhost:5000
```

## 🧪 Testing the System

### Request a Verification Code

1. Open the web interface.
2. Enter a Gmail address.
3. Request a verification code.
4. Check the received email.

### Verify the Code

1. Enter the six-digit code.
2. Submit the verification request.
3. Check the verification result.
4. Review the history to confirm that the record was persisted.

## 🧱 Service Structure

### `VerificationCodeService`

Contains the main verification logic:

- Code generation.
- SHA-256 hashing.
- Persistence through Marten.
- Code expiration.
- Attempt limits.
- Verification status management.
- Code validation.

### `SmtpEmailService`

Implements email delivery through Gmail SMTP using MailKit.

### `IEmailService`

Defines the contract used by the email delivery service.

## 🔒 Security and Configuration

The project uses several controls around the verification workflow:

- Cryptographically secure code generation with `RandomNumberGenerator`.
- SHA-256 hashing of verification codes.
- 10-minute code expiration.
- Maximum of 5 verification attempts.
- Invalidation of previous codes.
- External credentials through environment variables.
- `.env` excluded from the repository through `.gitignore`.

## 🎯 What This Project Demonstrates

Marten + Supabase demonstrates practical backend development through:

- .NET 10 and ASP.NET Core minimal API endpoints.
- Document-oriented persistence with Marten over PostgreSQL.
- Supabase as the PostgreSQL hosting platform.
- Transactional email delivery through MailKit and Gmail SMTP.
- Secure random verification-code generation.
- SHA-256 hashing before persistence.
- Expiration and verification-attempt controls.
- Environment-based configuration for sensitive credentials.
- Separation between API endpoints, verification logic, persistence, and email delivery.

## 📌 Project Status

**Completed portfolio project.**

The application was developed as an academic and portfolio project to demonstrate backend API development, PostgreSQL persistence, email integration, and verification workflow design.

## 🧰 Project Stack

Marten + Supabase integrates:


```text
.NET 10
   +
ASP.NET Core
   +
Marten
   +
PostgreSQL / Supabase
   +
MailKit / Gmail SMTP
   +
HTML / CSS / JavaScript
```

The project combines a REST API, document persistence, email delivery, and a web interface in a .NET application.

## Author

**Miguel Arbeláez Vallejo**

Software Developer | Flutter & Dart | Full-Stack | Backend | AI/Data
