// ============================================================
// VALIDACIÓN DE GMAIL
// ============================================================

// Comprueba si el correo termina en @gmail.com.
function isGmailAddress(email) {

    // Rechazamos valores vacíos.
    if (!email) {
        return false;
    }

    // Normalizamos el correo y comprobamos el dominio.
    return email
        .trim()
        .toLowerCase()
        .endsWith("@gmail.com");
}


// ============================================================
// ENVÍO DEL CÓDIGO
// ============================================================

async function sendCode() {

    // Obtenemos el correo introducido por el usuario.
    const email =
        document.getElementById("email")
            .value
            .trim();


    // Validamos que sea Gmail.
    if (!isGmailAddress(email)) {

        alert(
            "Solo se permiten cuentas Gmail."
        );

        return;
    }


    // Enviamos la información al endpoint
    // POST /auth/send-code.
    const response = await fetch(
        "/auth/send-code",
        {
            method: "POST",

            headers: {
                "Content-Type":
                    "application/json"
            },

            body: JSON.stringify({
                email: email
            })
        }
    );


    // Convertimos la respuesta del servidor
    // en texto.
    const message =
        await response.text();


    // Mostramos el resultado al usuario.
    document.getElementById("result")
        .textContent = message;


    // Actualizamos el historial.
    loadHistory();
}


// ============================================================
// VERIFICACIÓN
// ============================================================

async function verifyCode() {

    // Obtenemos el correo.
    const email =
        document.getElementById("email")
            .value
            .trim();


    // Obtenemos el código.
    const code =
        document.getElementById("code")
            .value
            .trim();


    // Enviamos ambos datos al backend.
    const response = await fetch(
        "/auth/verify-code",
        {
            method: "POST",

            headers: {
                "Content-Type":
                    "application/json"
            },

            body: JSON.stringify({
                email: email,
                code: code
            })
        }
    );


    // Obtenemos la respuesta.
    const message =
        await response.text();


    // Mostramos el resultado.
    document.getElementById("result")
        .textContent = message;


    // Actualizamos el historial.
    loadHistory();
}


// ============================================================
// HISTORIAL
// ============================================================

async function loadHistory() {

    // Consultamos el endpoint de historial.
    const response =
        await fetch("/auth/history");


    // Convertimos la respuesta JSON
    // en un objeto JavaScript.
    const history =
        await response.json();


    const container =
        document.getElementById("history");


    // Limpiamos el contenido anterior.
    container.innerHTML = "";


    // Recorremos todos los registros.
    history.forEach(item => {

        const element =
            document.createElement("div");


        // Mostramos la información del registro.
        element.innerHTML = `
            <strong>${item.email}</strong>
            <br>
            Estado: ${item.status}
            <br>
            Intentos: ${item.attempts}
        `;


        container.appendChild(element);
    });
}


// ============================================================
// EVENTOS
// ============================================================

// Cuando se pulsa "Enviar código",
// ejecutamos sendCode().
document
    .getElementById("sendCodeButton")
    .addEventListener(
        "click",
        sendCode
    );


// Cuando se pulsa "Verificar código",
// ejecutamos verifyCode().
document
    .getElementById("verifyCodeButton")
    .addEventListener(
        "click",
        verifyCode
    );


// Cargamos el historial al abrir la página.
loadHistory();