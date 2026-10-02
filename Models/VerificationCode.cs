namespace MARTEN_SUPABASE.Models;

public class VerificationCode
{
    // Identificador único de cada registro.
    // Cada código almacenado tendrá su propio ID.
    public Guid Id { get; set; }


    // Correo Gmail asociado al código.
    // Es el correo al que se enviará el código.
    public string Email { get; set; } = string.Empty;


    // Hash SHA-256 del código de verificación.
    // No almacenamos directamente el código original.
    public string CodeHash { get; set; } = string.Empty;


    // Fecha y hora en la que se creó el código.
    public DateTime CreatedAt { get; set; }


    // Fecha y hora límite para utilizar el código.
    // El sistema establece una duración de 10 minutos.
    public DateTime ExpiresAt { get; set; }


    // Indica si el código ya fue utilizado.
    // False = todavía disponible.
    // True = ya utilizado.
    public bool Used { get; set; }


    // Número de intentos realizados por el usuario.
    // Se utiliza para limitar los intentos de verificación.
    public int Attempts { get; set; }


    // Estado actual del código.
    // Puede ser:
    // Pendiente
    // Verificado
    // Expirado
    // Bloqueado
    // Reemplazado
    public string Status { get; set; } = "Pendiente";


    // Fecha en la que el código fue verificado.
    // Es nullable porque antes de verificarse no existe.
    public DateTime? VerifiedAt { get; set; }
}