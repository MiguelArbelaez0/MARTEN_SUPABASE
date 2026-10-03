# MARTEN-SUPABASE

Sistema de verificación de códigos por correo electrónico desarrollado con .NET 10, ASP.NET Core, Marten y PostgreSQL mediante Supabase.

El proyecto implementa un flujo completo de generación, envío, almacenamiento y validación de códigos de verificación mediante correo electrónico, utilizando persistencia documental con Marten y PostgreSQL.

## Tecnologías

### Backend

- .NET 10
- ASP.NET Core
- C#
- Marten 9.44.0
- REST API
- OpenAPI

### Persistencia

- PostgreSQL
- Supabase
- Marten Document Store

### Correo electrónico

- MailKit
- MimeKit
- Gmail SMTP
- STARTTLS

### Frontend

- HTML
- CSS
- JavaScript
- ASP.NET Core Static Files

### Configuración

- DotNetEnv
- Variables de entorno

## Arquitectura

El proyecto separa la lógica de dominio, persistencia, envío de correo y presentación web.

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

### Flujo de arquitectura

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

## Funcionamiento

El sistema permite solicitar un código de verificación de 6 dígitos para una cuenta Gmail.

El flujo principal es:

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

## Modelo de verificación

Cada registro `VerificationCode` contiene información relacionada con el proceso de verificación:

- `Id` — Identificador único.
- `Email` — Correo Gmail asociado.
- `CodeHash` — Hash SHA-256 del código.
- `CreatedAt` — Fecha de creación.
- `ExpiresAt` — Fecha de expiración.
- `Used` — Indica si el código ya fue utilizado.
- `Attempts` — Número de intentos realizados.
- `Status` — Estado actual de la verificación.
- `VerifiedAt` — Fecha de verificación, cuando corresponde.

## Estados

El sistema maneja los siguientes estados:

| Estado | Descripción |
|---|---|
| `Pendiente` | Código creado y disponible para verificación. |
| `Verificado` | Código validado correctamente. |
| `Expirado` | El período de validez del código terminó. |
| `Bloqueado` | Se alcanzó el límite de intentos de verificación. |
| `Reemplazado` | El código fue reemplazado por una nueva solicitud. |

## Reglas de verificación

- Los correos aceptados deben pertenecer al dominio `@gmail.com`.
- Los códigos tienen 6 dígitos.
- El código expira después de 10 minutos.
- Se permiten hasta 5 intentos de verificación.
- Los códigos anteriores no utilizados se marcan como `Reemplazado` cuando se solicita uno nuevo.
- El código original no se almacena directamente en la base de datos; se almacena su hash SHA-256.
- Después de una verificación correcta, el código queda marcado como utilizado.

## API REST

### `POST /auth/send-code`

Genera un nuevo código de verificación y lo envía al correo Gmail indicado.

Ejemplo de solicitud:

```json
{
  "email": "usuario@gmail.com"
}
```

### `POST /auth/verify-code`

Verifica el código introducido por el usuario.

Ejemplo de solicitud:

```json
{
  "email": "usuario@gmail.com",
  "code": "123456"
}
```

### `GET /auth/history`

Devuelve los últimos 20 registros de verificación ordenados por fecha de creación descendente.

La respuesta incluye información como:

- ID
- Correo
- Fecha de creación
- Fecha de expiración
- Intentos
- Estado
- Fecha de verificación

## Persistencia con Marten

Marten se utiliza como document store sobre PostgreSQL.

El modelo `VerificationCode` se registra como documento mediante la configuración de ASP.NET Core:

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

Las operaciones de creación, consulta y actualización de los registros se realizan mediante sesiones de Marten.

## Envío de correo

El envío de los códigos se realiza mediante `SmtpEmailService`.

La aplicación utiliza:

- MailKit
- MimeKit
- Gmail SMTP
- Puerto `587`
- STARTTLS

Las credenciales SMTP se obtienen mediante variables de entorno y no forman parte del código fuente.

## Configuración

### 1. Crear `.env`

En la raíz del proyecto, crea un archivo llamado:

```text
.env
```

Utiliza `.env.example` como referencia:

```env
SUPABASE_CONNECTION_STRING="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu_correo@gmail.com
SMTP_PASSWORD=tu_app_password
SMTP_FROM=tu_correo@gmail.com
SMTP_FROM_NAME=Marten Supabase
```

Completa los valores con tus propias credenciales y configuración.

> **Importante:** `.env` contiene información sensible y debe permanecer únicamente en el entorno local. No debe subirse a GitHub.

## Ejecución

### Restaurar dependencias

```bash
dotnet restore
```

### Ejecutar la aplicación

```bash
dotnet run --no-launch-profile
```

La aplicación se ejecuta localmente según la configuración de lanzamiento disponible.

En el entorno utilizado durante el desarrollo, puede accederse mediante:

```text
http://localhost:5000
```

## Prueba del sistema

### Solicitar un código

1. Abrir la interfaz web.
2. Introducir una cuenta Gmail.
3. Solicitar el código de verificación.
4. Revisar el correo recibido.

### Verificar el código

1. Introducir el código de 6 dígitos.
2. Enviar la solicitud de verificación.
3. Comprobar el resultado.
4. Consultar el historial para verificar la persistencia del registro.

## Estructura de servicios

### `VerificationCodeService`

Contiene la lógica principal del proceso de verificación:

- Generación de códigos.
- Hash SHA-256.
- Persistencia mediante Marten.
- Expiración.
- Control de intentos.
- Estados de verificación.
- Validación de códigos.

### `SmtpEmailService`

Implementa el envío de correos mediante Gmail SMTP utilizando MailKit.

### `IEmailService`

Define el contrato utilizado por el servicio de envío de correo.

## Seguridad y configuración

El proyecto utiliza varias medidas para controlar el proceso de verificación:

- Generación de códigos mediante `RandomNumberGenerator`.
- Almacenamiento del código mediante hash SHA-256.
- Expiración de 10 minutos.
- Límite de 5 intentos.
- Invalidación de códigos anteriores.
- Credenciales externas mediante variables de entorno.
- `.env` excluido del repositorio mediante `.gitignore`.

## Proyecto

MARTEN-SUPABASE demuestra la integración de:

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

El proyecto combina una API REST, persistencia documental, envío de correo electrónico y una interfaz web en una aplicación .NET.

## Autor

**Miguel Arbeláez Vallejo**

Ingeniería de Sistemas — Fundación Universitaria CEIPA

Proyecto académico y de portafolio.
