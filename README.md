# Marten + Supabase

> Verification code API built with .NET 10, ASP.NET Core, Marten and PostgreSQL through Supabase.

The project implements a complete workflow for generating, sending, storing, and validating email verification codes using Marten document persistence and PostgreSQL.

## Technologies

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

### Configuration

- DotNetEnv
- Variables de entorno

## Architecture

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
Usuario
   │
   ▼
Interfaz Web
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

## How It Works

The system allows users to request a six-digit verification code for a Gmail account.

The main workflow is:

1. El usuario ingresa una dirección de correo Gmail.
2. ASP.NET Core valida que el correo termine en `@gmail.com`.
3. `VerificationCodeService` genera un código aleatorio de 6 dígitos.
4. El código se transforma mediante SHA-256.
5. Se crea un documento `VerificationCode`.
6. El hash del código se almacena mediante Marten en PostgreSQL/Supabase.
7. El código original se envía al correo mediante Gmail SMTP y MailKit.
8. El usuario introduce el código recibido.
9. El sistema genera nuevamente el hash del código introducido.
10. Se compara con el hash almacenado.
11. Si la verificación es correcta, el registro se marca como utilizado y verificado.
12. El historial permite consultar los últimos 20 registros.

## Verification Model

Each `VerificationCode` record contains information related to the verification process:

- `Id` — Identificador único.
- `Email` — Correo Gmail asociado.
- `CodeHash` — Hash SHA-256 del código.
- `CreatedAt` — Fecha de creación.
- `ExpiresAt` — Fecha de expiración.
- `Used` — Indica si el código ya fue utilizado.
- `Attempts` — Número de intentos realizados.
- `Status` — Estado actual de la verificación.
- `VerifiedAt` — Fecha de verificación, cuando corresponde.

## Verification States

The system uses the following states:

| Estado | Descripción |
|---|---|
| `Pendiente` | Código creado y disponible para verificación. |
| `Verificado` | Código validado correctamente. |
| `Expirado` | El período de validez del código terminó. |
| `Bloqueado` | Se alcanzó el límite de intentos de verificación. |
| `Reemplazado` | El código fue reemplazado por una nueva solicitud. |

## Verification Rules

- Los correos aceptados deben pertenecer al dominio `@gmail.com`.
- Los códigos tienen 6 dígitos.
- El código expira después de 10 minutos.
- Se permiten hasta 5 intentos de verificación.
- Los códigos anteriores no utilizados se marcan como `Reemplazado` cuando se solicita uno nuevo.
- El código original no se almacena directamente en la base de datos; se almacena su hash SHA-256.
- Después de una verificación correcta, el código queda marcado como utilizado.

## REST API

### `POST /auth/send-code`

Generates a new verification code and sends it to the specified Gmail address.

Ejemplo de solicitud:

```json
{
  "email": "usuario@gmail.com"
}
```

### `POST /auth/verify-code`

Verifies the code submitted by the user.

Ejemplo de solicitud:

```json
{
  "email": "usuario@gmail.com",
  "code": "123456"
}
```

### `GET /auth/history`

Returns the latest 20 verification records ordered by creation date in descending order.

The response includes information such as:

- ID
- Correo
- Fecha de creación
- Fecha de expiración
- Intentos
- Estado
- Fecha de verificación

## Persistence with Marten

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

## Email Delivery

Verification codes are sent through `SmtpEmailService`.

The application uses:

- MailKit
- MimeKit
- Gmail SMTP
- Puerto `587`
- STARTTLS

SMTP credentials are loaded through environment variables and are not part of the source code.

## Configuration

### 1. Crear `.env`

In the project root, create a file named:

```text
.env
```

Use `.env.example` as a reference:

```env
SUPABASE_CONNECTION_STRING="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu_correo@gmail.com
SMTP_PASSWORD=tu_app_password
SMTP_FROM=tu_correo@gmail.com
SMTP_FROM_NAME=Marten Supabase
```

Replace the placeholder values with your own credentials and configuration.

> **Importante:** `.env` contiene información sensible y debe permanecer únicamente en el entorno local. No debe subirse a GitHub.

## Running the Application

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

## Testing the System

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

## Service Structure

### `VerificationCodeService`

Contains the main verification logic:

- Generación de códigos.
- Hash SHA-256.
- Persistencia mediante Marten.
- Expiración.
- Control de intentos.
- Estados de verificación.
- Validación de códigos.

### `SmtpEmailService`

Implements email delivery through Gmail SMTP using MailKit.

### `IEmailService`

Defines the contract used by the email delivery service.

## Security and Configuration

The project uses several controls around the verification workflow:

- Generación de códigos mediante `RandomNumberGenerator`.
- Almacenamiento del código mediante hash SHA-256.
- Expiración de 10 minutos.
- Límite de 5 intentos.
- Invalidación de códigos anteriores.
- Credenciales externas mediante variables de entorno.
- `.env` excluido del repositorio mediante `.gitignore`.

## Project

Marten + Supabase demonstrates the integration of:

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

Systems Engineering — Fundación Universitaria CEIPA

Academic and portfolio project.
