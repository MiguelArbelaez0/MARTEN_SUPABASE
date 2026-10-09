# Marten + Supabase

> API de verificación de correo desarrollada con .NET 10, ASP.NET Core, Marten, PostgreSQL/Supabase y MailKit.

Marten + Supabase es una aplicación enfocada en servidor que implementa un flujo completo de verificación de correo: generación segura de códigos, hash SHA-256, persistencia mediante Marten y PostgreSQL, envío mediante Gmail SMTP, expiración, límite de intentos y gestión del estado de verificación.

## 🧩 Tecnologías

### Servidor
- .NET 10
- ASP.NET Core
- C#
- Marten 9.44.0
- API REST
- OpenAPI

### Persistencia
- PostgreSQL
- Supabase
- Marten Document Store

### Correo
- MailKit
- MimeKit
- Gmail SMTP
- STARTTLS

### Interfaz
- HTML
- CSS
- JavaScript
- Archivos estáticos de ASP.NET Core

### ⚙️ Configuración
- DotNetEnv
- Variables de entorno

## 🏗️ Arquitectura

El proyecto separa la presentación web, la lógica de verificación, la persistencia documental y el envío de correo.

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
Interfaz web
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

## 🔄 Funcionamiento

El sistema permite solicitar un código de verificación de seis dígitos para una cuenta de Gmail.

Flujo principal:

1. El usuario ingresa una dirección de Gmail.
2. ASP.NET Core valida que el correo termine en `@gmail.com`.
3. `VerificationCodeService` genera un código aleatorio de seis dígitos.
4. El código se transforma mediante SHA-256.
5. Se crea un documento `VerificationCode`.
6. El hash se almacena mediante Marten en PostgreSQL/Supabase.
7. El código original se envía al correo mediante Gmail SMTP y MailKit.
8. El usuario introduce el código recibido.
9. El sistema vuelve a generar el hash del código enviado.
10. Se compara el hash enviado con el almacenado.
11. Si la verificación es correcta, el registro se marca como utilizado y verificado.
12. El endpoint de historial permite consultar los últimos 20 registros.

## 🗂️ Modelo de verificación

Cada registro `VerificationCode` contiene:

- `Id` — Identificador único.
- `Email` — Dirección de Gmail asociada.
- `CodeHash` — Hash SHA-256 del código.
- `CreatedAt` — Fecha y hora de creación.
- `ExpiresAt` — Fecha y hora de expiración.
- `Used` — Indica si el código ya fue utilizado.
- `Attempts` — Número de intentos.
- `Status` — Estado actual de la verificación.
- `VerifiedAt` — Fecha y hora de verificación, cuando corresponde.

## 📊 Estados de verificación

| Estado | Descripción |
|---|---|
| `Pendiente` | Código creado y disponible para verificación. |
| `Verificado` | Código verificado correctamente. |
| `Expirado` | El periodo de validez terminó. |
| `Bloqueado` | Se alcanzó el máximo de intentos. |
| `Reemplazado` | El código fue sustituido por una nueva solicitud. |

## 🔐 Reglas de verificación

- Solo se aceptan direcciones del dominio `@gmail.com`.
- Los códigos contienen 6 dígitos.
- Los códigos expiran después de 10 minutos.
- Se permiten máximo 5 intentos de verificación.
- Los códigos anteriores sin utilizar se marcan como `Reemplazado` al solicitar uno nuevo.
- El código original no se almacena directamente; se guarda su hash SHA-256.
- Después de una verificación correcta, el código se marca como utilizado.

## 🌐 API REST

### `POST /auth/send-code`

Genera un nuevo código y lo envía a la dirección de Gmail indicada.

```json
{
  "email": "usuario@gmail.com"
}
```

### `POST /auth/verify-code`

Verifica el código enviado por el usuario.

```json
{
  "email": "usuario@gmail.com",
  "code": "123456"
}
```

### `GET /auth/history`

Devuelve los últimos 20 registros de verificación ordenados por fecha de creación descendente.

## 🗄️ Persistencia con Marten

Marten funciona como almacén documental sobre PostgreSQL.

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

Los registros se crean, consultan y actualizan mediante sesiones de Marten.

## 📧 Envío de correo

Los códigos se envían mediante `SmtpEmailService`.

La aplicación utiliza:

- MailKit
- MimeKit
- Gmail SMTP
- Puerto `587`
- STARTTLS

Las credenciales SMTP se cargan mediante variables de entorno y no forman parte del código fuente.

## ⚙️ Configuración

### 1. Crear `.env`

En la raíz del proyecto, crea:

```text
.env
```

Utiliza `.env.example` como referencia:

```env
SUPABASE_CONNECTION_STRING="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu_correo@gmail.com
SMTP_PASSWORD=tu_clave_de_aplicacion
SMTP_FROM=tu_correo@gmail.com
SMTP_FROM_NAME=Marten Supabase
```

> **Importante:** `.env` contiene información sensible y debe permanecer únicamente en local. Nunca debe publicarse en GitHub.

## ▶️ Ejecución

### Restaurar dependencias

```bash
dotnet restore
```

### Ejecutar la aplicación

```bash
dotnet run --no-launch-profile
```

Durante el desarrollo puede accederse mediante:

```text
http://localhost:5000
```

## 🧪 Prueba del sistema

### Solicitar un código

1. Abrir la interfaz web.
2. Ingresar una dirección de Gmail.
3. Solicitar el código de verificación.
4. Revisar el correo recibido.

### Verificar el código

1. Ingresar el código de seis dígitos.
2. Enviar la solicitud.
3. Revisar el resultado.
4. Consultar el historial para confirmar la persistencia.

## 🧱 Servicios principales

### `VerificationCodeService`

Contiene la lógica principal de verificación:

- Generación de códigos.
- Hash SHA-256.
- Persistencia mediante Marten.
- Expiración.
- Límite de intentos.
- Gestión de estados.
- Validación del código.

### `SmtpEmailService`

Gestiona el envío de correo mediante Gmail SMTP y MailKit.

### `IEmailService`

Define el contrato utilizado por el servicio de correo.

## 🔒 Seguridad y configuración

El proyecto incorpora:

- Generación criptográficamente segura mediante `RandomNumberGenerator`.
- Hash SHA-256 de los códigos.
- Expiración de 10 minutos.
- Máximo de 5 intentos.
- Invalidación de códigos anteriores.
- Credenciales externas mediante variables de entorno.
- Exclusión de `.env` mediante `.gitignore`.

## 🎯 Qué demuestra este proyecto

- Desarrollo de servidor con .NET 10 y ASP.NET Core.
- Persistencia documental con Marten sobre PostgreSQL.
- Uso de Supabase como plataforma PostgreSQL.
- Envío de correo mediante MailKit y Gmail SMTP.
- Generación segura de códigos de verificación.
- Hash SHA-256 antes de la persistencia.
- Control de expiración e intentos.
- Configuración mediante variables de entorno.
- Separación entre API, lógica de verificación, persistencia y correo.

## 📌 Estado del proyecto

**Proyecto académico y de portafolio terminado.**

## 🧰 Tecnologías integradas

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

## 👨‍💻 Autor

**Miguel Arbeláez Vallejo**

Flutter & Dart · Full-Stack · Backend · AI/Data
