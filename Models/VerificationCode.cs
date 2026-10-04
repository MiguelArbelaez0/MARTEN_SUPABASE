namespace MARTEN_SUPABASE.Models;

public class VerificationCode
{
    // ============================================================
    // IDENTIFICACIÓN DEL DOCUMENTO
    // ============================================================

    // Identificador único del código de verificación.
    // Cada código almacenado por Marten tiene su propio ID.
    public Guid Id { get; set; }


    // ============================================================
    // DATOS DEL USUARIO
    // ============================================================

    // Correo al que pertenece el código.
    // El sistema utiliza este dato para encontrar
    // el código correspondiente cuando se intenta verificar.
    public string Email { get; set; } = string.Empty;


    // ============================================================
    // SEGURIDAD DEL CÓDIGO
    // ============================================================

    // NO almacenamos el código original.
    //
    // Aquí se guarda el HASH SHA-256 del código.
    //
    // Ejemplo:
    //
    // Código real:
    // 583214
    //
    // Base de datos:
    // HASH(583214)
    //
    // Esto evita almacenar directamente el código
    // que recibió el usuario.
    public string CodeHash { get; set; } = string.Empty;


    // ============================================================
    // TIEMPO DE VIDA
    // ============================================================

    // Momento exacto en que se generó el código.
    public DateTime CreatedAt { get; set; }


    // Momento exacto en que el código deja de ser válido.
    //
    // En nuestro sistema se establece 10 minutos después
    // de la creación.
    //
    // Evento relacionado:
    //
    // Tiempo actual > ExpiresAt
    //
    // Transición:
    //
    // PENDIENTE → EXPIRADO
    public DateTime ExpiresAt { get; set; }


    // ============================================================
    // CONTROL DE UTILIZACIÓN
    // ============================================================

    // Indica si el código ya dejó de estar disponible.
    //
    // false → todavía puede utilizarse.
    // true  → ya fue utilizado o invalidado.
    public bool Used { get; set; }


    // ============================================================
    // CONTROL DE INTENTOS
    // ============================================================

    // Número de intentos realizados para verificar el código.
    //
    // Se incrementa cuando el usuario intenta verificar
    // un código.
    //
    // Si se alcanza el límite de 5 intentos:
    //
    // PENDIENTE → BLOQUEADO
    public int Attempts { get; set; }


    // ============================================================
    // ESTADO DEL CÓDIGO
    // ============================================================

    // Representa el estado actual dentro del ciclo de vida
    // del código de verificación.
    //
    // Estados utilizados:
    //
    // Pendiente   → código creado y todavía disponible.
    // Verificado  → código correcto.
    // Expirado    → superó los 10 minutos.
    // Bloqueado   → alcanzó el límite de intentos.
    // Reemplazado → se generó un nuevo código.
    public string Status { get; set; } = "Pendiente";


    // ============================================================
    // FECHA DE VERIFICACIÓN
    // ============================================================

    // Momento en que el código fue verificado correctamente.
    //
    // Es nullable porque mientras el código esté Pendiente,
    // todavía no existe una fecha de verificación.
    public DateTime? VerifiedAt { get; set; }
}