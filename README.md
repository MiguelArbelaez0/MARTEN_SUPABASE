# MARTEN-SUPABASE

Sistema de verificación de códigos por correo electrónico desarrollado con .NET 10, ASP.NET Core, Marten y PostgreSQL mediante Supabase.

## Tecnologías

- .NET 10
- ASP.NET Core
- Marten 9.44.0
- PostgreSQL
- Supabase
- MailKit
- Gmail SMTP
- DotNetEnv
- HTML
- CSS
- JavaScript

## Arquitectura

El proyecto está organizado por responsabilidades:

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
├── .env
├── .gitignore
└── README.md

## Funcionamiento

El sistema permite enviar un código de verificación de 6 dígitos a una cuenta Gmail.

El flujo es:

1. El usuario ingresa su correo Gmail.
2. El sistema valida que termine en @gmail.com.
3. Se genera un código aleatorio de 6 dígitos.
4. El código se almacena utilizando un hash SHA-256.
5. El registro se guarda mediante Marten en PostgreSQL/Supabase.
6. El código se envía mediante Gmail SMTP.
7. El usuario ingresa el código recibido.
8. El sistema valida el código.
9. El código tiene una duración de 10 minutos.
10. Después de verificarse correctamente, el código queda marcado como utilizado.
11. El sistema permite consultar el historial de verificaciones.

## Características

- Generación segura de códigos de 6 dígitos.
- Validación exclusiva de cuentas Gmail.
- Hash SHA-256 de los códigos.
- Expiración automática después de 10 minutos.
- Máximo de 5 intentos de verificación.
- Control de estados.
- Persistencia real con Marten.
- PostgreSQL alojado en Supabase.
- Envío de correos mediante Gmail SMTP.
- Historial de verificaciones.
- Interfaz web responsive.
- API REST con ASP.NET Core.

## Estados de verificación

El sistema utiliza los siguientes estados:

- Pendiente
- Verificado
- Expirado
- Bloqueado
- Reemplazado

## Endpoints

POST /auth/send-code

Envía un nuevo código de verificación al correo Gmail indicado.

POST /auth/verify-code

Verifica el código ingresado por el usuario.

GET /auth/history

Consulta el historial de los códigos registrados en la base de datos.

## Configuración

Crear un archivo `.env` en la raíz del proyecto:

SUPABASE_CONNECTION_STRING="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu_correo@gmail.com
SMTP_PASSWORD=tu_app_password
SMTP_FROM=tu_correo@gmail.com
SMTP_FROM_NAME=Marten Supabase

El archivo `.env` no debe subirse a GitHub.

## Ejecución

Restaurar dependencias:

dotnet restore

Ejecutar el proyecto:

dotnet run --no-launch-profile

Abrir en el navegador:

http://localhost:5000

## Prueba

1. Ingresar una cuenta Gmail.
2. Presionar el botón para enviar el código.
3. Revisar el correo recibido.
4. Ingresar el código de 6 dígitos.
5. Verificar el resultado.
6. Consultar el historial para comprobar la persistencia del registro.

## Persistencia

Marten utiliza PostgreSQL como almacenamiento de documentos.

Los registros de VerificationCode se almacenan en Supabase y pueden consultarse posteriormente mediante el endpoint:

GET /auth/history

## Seguridad

El sistema implementa:

- Validación del dominio Gmail.
- Códigos aleatorios de 6 dígitos.
- Almacenamiento mediante SHA-256.
- Tiempo de expiración de 10 minutos.
- Límite de 5 intentos.
- Invalidación de códigos anteriores.
- Variables sensibles almacenadas mediante `.env`.

## Autor

Miguel Arbeláez Vallejo

Proyecto académico y de portafolio desarrollado con .NET, Marten y Supabase.