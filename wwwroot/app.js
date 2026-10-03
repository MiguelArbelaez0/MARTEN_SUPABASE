/* ============================================================
   MARTEN SUPABASE
   JAVASCRIPT PRINCIPAL
   ============================================================ */


/* ============================================================
   ELEMENTOS DEL DOM
   ============================================================ */

const emailInput =
    document.getElementById("email");

const codeInput =
    document.getElementById("code");

const sendCodeButton =
    document.getElementById("sendCodeButton");

const verifyCodeButton =
    document.getElementById("verifyCodeButton");

const verificationCard =
    document.getElementById("verificationCard");

const codeCard =
    document.getElementById("codeCard");

const resultCard =
    document.getElementById("resultCard");

const result =
    document.getElementById("result");

const history =
    document.getElementById("history");

const refreshHistoryButton =
    document.getElementById("refreshHistoryButton");


/* ============================================================
   CORREO ACTUAL
   ============================================================ */

let currentEmail = "";


/* ============================================================
   VALIDAR GMAIL
   ============================================================ */

function isGmailAddress(email) {

    if (!email) {
        return false;
    }

    return email
        .trim()
        .toLowerCase()
        .endsWith("@gmail.com");
}


/* ============================================================
   ENVIAR CÓDIGO
   ============================================================ */

sendCodeButton.addEventListener(
    "click",
    async () => {

        const email =
            emailInput.value
                .trim()
                .toLowerCase();


        if (!email) {

            showResult(
                "Debes ingresar un correo electrónico.",
                "error"
            );

            return;
        }


        if (!isGmailAddress(email)) {

            showResult(
                "Solo se permiten cuentas Gmail (@gmail.com).",
                "error"
            );

            return;
        }


        currentEmail = email;


        sendCodeButton.disabled = true;

        sendCodeButton.textContent =
            "Enviando...";


        try {

            const response =
                await fetch(
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


            const data =
                await response.text();


            if (!response.ok) {

                throw new Error(data);
            }


            /*
             * Mostramos el paso 02.
             */

            codeCard.classList.remove(
                "hidden"
            );


            resultCard.classList.add(
                "hidden"
            );


            codeInput.value = "";

            codeInput.focus();


            /*
             * Actualizamos el historial.
             */

            await loadHistory();


            /*
             * Desplazamos la pantalla
             * hacia el código.
             */

            codeCard.scrollIntoView({
                behavior: "smooth",
                block: "start"
            });

        }
        catch (error) {

            showResult(
                error.message ||
                "No fue posible enviar el código.",
                "error"
            );

        }
        finally {

            sendCodeButton.disabled =
                false;

            sendCodeButton.textContent =
                "Enviar código";
        }

    }
);


/* ============================================================
   VERIFICAR CÓDIGO
   ============================================================ */

verifyCodeButton.addEventListener(
    "click",
    async () => {

        const code =
            codeInput.value.trim();


        if (!currentEmail) {

            showResult(
                "Primero debes solicitar un código.",
                "error"
            );

            return;
        }


        if (!/^\d{6}$/.test(code)) {

            showResult(
                "El código debe contener 6 dígitos.",
                "error"
            );

            return;
        }


        verifyCodeButton.disabled = true;

        verifyCodeButton.textContent =
            "Verificando...";


        try {

            const response =
                await fetch(
                    "/auth/verify-code",
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        body: JSON.stringify({
                            email: currentEmail,
                            code: code
                        })
                    }
                );


            const data =
                await response.text();


            if (!response.ok) {

                throw new Error(data);
            }


            /*
             * Código correcto.
             */

            showVerifiedScreen(
                currentEmail
            );


            /*
             * Actualizamos el historial.
             */

            await loadHistory();

        }
        catch (error) {

            showResult(
                error.message ||
                "El código no es válido.",
                "error"
            );


            /*
             * También actualizamos el historial
             * cuando existe un intento incorrecto.
             */

            await loadHistory();

        }
        finally {

            verifyCodeButton.disabled =
                false;

            verifyCodeButton.textContent =
                "Verificar código";
        }

    }
);


/* ============================================================
   MOSTRAR RESULTADO
   ============================================================ */

function showResult(
    message,
    type = ""
) {

    resultCard.classList.remove(
        "hidden"
    );


    result.className =
        "result-message";


    if (type) {

        result.classList.add(type);
    }


    result.textContent =
        message;


    resultCard.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


/* ============================================================
   PANTALLA DE CORREO VERIFICADO
   ============================================================ */

function showVerifiedScreen(
    email
) {

    verificationCard.innerHTML = `

        <div class="verification-success">

            <div class="success-icon">
                ✓
            </div>


            <h2 class="success-title">
                Correo verificado
            </h2>


            <p class="success-description">
                El correo electrónico fue verificado correctamente.
            </p>


            <div class="email-result">

                <span>
                    Correo
                </span>

                <strong>
                    ${escapeHtml(email)}
                </strong>

            </div>


            <button
                id="newVerificationButton"
                class="new-verification">

                Nueva verificación

            </button>

        </div>

    `;


    codeCard.classList.add(
        "hidden"
    );


    resultCard.classList.add(
        "hidden"
    );


    const newVerificationButton =
        document.getElementById(
            "newVerificationButton"
        );


    newVerificationButton.addEventListener(
        "click",
        resetVerification
    );
}


/* ============================================================
   NUEVA VERIFICACIÓN
   ============================================================ */

function resetVerification() {

    window.location.reload();
}


/* ============================================================
   CARGAR HISTORIAL
   ============================================================ */

async function loadHistory() {

    try {

        const response =
            await fetch(
                "/auth/history"
            );


        if (!response.ok) {

            throw new Error(
                "No fue posible cargar el historial."
            );
        }


        const records =
            await response.json();


        renderHistory(records);

    }
    catch (error) {

        history.innerHTML = `

            <div class="history-empty">

                No fue posible cargar el historial.

            </div>

        `;
    }
}


/* ============================================================
   RENDERIZAR HISTORIAL
   ============================================================ */

function renderHistory(
    records
) {

    /*
     * Si no existen registros.
     */

    if (
        !records ||
        records.length === 0
    ) {

        history.innerHTML = `

            <div class="history-empty">

                No existen verificaciones registradas.

            </div>

        `;

        return;
    }


    /*
     * Generamos cada registro verticalmente.
     */

    history.innerHTML =
        records
            .map(
                record => {

                    /*
                     * Obtenemos la información visual
                     * dependiendo del estado real
                     * almacenado por Marten.
                     */

                    const status =
                        getHistoryStatus(
                            record.status
                        );


                    /*
                     * Formateamos las fechas.
                     */

                    const createdAt =
                        formatDate(
                            record.createdAt
                        );


                    const expiresAt =
                        formatDate(
                            record.expiresAt
                        );


                    const verifiedAt =
                        record.verifiedAt
                            ? formatDate(
                                record.verifiedAt
                            )
                            : null;


                    /*
                     * Creamos la tarjeta completa
                     * del registro.
                     */

                    return `

                        <article
                            class="history-item">


                            <!-- =================================
                                 CABECERA
                                 ================================= -->

                            <div
                                class="history-item-header">


                                <strong>

                                    ${escapeHtml(
                                        record.email
                                    )}

                                </strong>


                                <span
                                    class="
                                        history-status
                                        ${status.className}
                                    ">

                                    <span>
                                        ${status.icon}
                                    </span>

                                    ${status.label}

                                </span>

                            </div>


                            <!-- =================================
                                 INFORMACIÓN
                                 ================================= -->

                            <div
                                class="history-details">


                                <!-- Fecha creación -->

                                <div
                                    class="history-detail">

                                    <span>
                                        Creado
                                    </span>

                                    <strong>
                                        ${createdAt}
                                    </strong>

                                </div>


                                <!-- Intentos -->

                                <div
                                    class="history-detail">

                                    <span>
                                        Intentos
                                    </span>

                                    <strong>
                                        ${record.attempts}
                                    </strong>

                                </div>


                                <!-- Expiración -->

                                <div
                                    class="history-detail">

                                    <span>
                                        Expira
                                    </span>

                                    <strong>
                                        ${expiresAt}
                                    </strong>

                                </div>

                            </div>


                            <!-- =================================
                                 FECHA DE VERIFICACIÓN
                                 ================================= -->

                            ${
                                verifiedAt
                                    ? `

                                        <div
                                            class="history-verified">

                                            ✓ Código verificado el
                                            ${verifiedAt}

                                        </div>

                                      `
                                    : ""
                            }


                        </article>

                    `;
                }
            )
            .join("");
}


/* ============================================================
   ESTADOS
   ============================================================ */

function getHistoryStatus(
    status
) {

    const normalizedStatus =
        String(status || "")
            .trim()
            .toLowerCase();


    switch (normalizedStatus) {


        /* =============================================
           PENDIENTE
           ============================================= */

        case "pendiente":

            return {

                label: "En proceso",

                icon: "⏳",

                className:
                    "status-proceso"
            };


        /* =============================================
           VERIFICADO
           ============================================= */

        case "verificado":

            return {

                label: "Completado",

                icon: "✓",

                className:
                    "status-completado"
            };


        /* =============================================
           REEMPLAZADO
           ============================================= */

        case "reemplazado":

            return {

                label: "Reemplazado",

                icon: "↻",

                className:
                    "status-reemplazado"
            };


        /* =============================================
           EXPIRADO
           ============================================= */

        case "expirado":

            return {

                label: "Expirado",

                icon: "⌛",

                className:
                    "status-expirado"
            };


        /* =============================================
           BLOQUEADO
           ============================================= */

        case "bloqueado":

            return {

                label: "Bloqueado",

                icon: "🔒",

                className:
                    "status-bloqueado"
            };


        /* =============================================
           ESTADO DESCONOCIDO
           ============================================= */

        default:

            return {

                label:
                    status ||
                    "Sin estado",

                icon: "•",

                className: ""
            };
    }
}


/* ============================================================
   FORMATEAR FECHAS
   ============================================================ */

function formatDate(
    value
) {

    if (!value) {

        return "Sin fecha";
    }


    const date =
        new Date(value);


    if (
        Number.isNaN(
            date.getTime()
        )
    ) {

        return String(value);
    }


    return date.toLocaleString(
        "es-CO",
        {
            dateStyle: "short",
            timeStyle: "short"
        }
    );
}


/* ============================================================
   PROTEGER HTML
   ============================================================ */

function escapeHtml(
    value
) {

    if (
        value === null ||
        value === undefined
    ) {

        return "";
    }


    return String(value)

        .replace(
            /&/g,
            "&amp;"
        )

        .replace(
            /</g,
            "&lt;"
        )

        .replace(
            />/g,
            "&gt;"
        )

        .replace(
            /"/g,
            "&quot;"
        )

        .replace(
            /'/g,
            "&#039;"
        );
}


/* ============================================================
   BOTÓN ACTUALIZAR HISTORIAL
   ============================================================ */

refreshHistoryButton.addEventListener(
    "click",
    async () => {

        refreshHistoryButton.disabled =
            true;


        refreshHistoryButton.textContent =
            "Actualizando...";


        await loadHistory();


        refreshHistoryButton.disabled =
            false;


        refreshHistoryButton.textContent =
            "Actualizar";

    }
);


/* ============================================================
   CARGA INICIAL
   ============================================================ */

loadHistory();