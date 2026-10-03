# MARTEN-SUPABASE

Sistema de verificación de códigos por correo electrónico desarrollado con .NET 10, ASP.NET Core, Marten y PostgreSQL mediante Supabase.

El proyecto implementa un flujo completo de generación, envío, almacenamiento y validación de códigos de verificación mediante correo electrónico.

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

El proyecto está organizado por responsabilidades, separando modelos, servicios y la interfaz web:

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
├── .env.example
├── .gitignore
└── README.md