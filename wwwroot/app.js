const emailSection =
    document.getElementById("email-section");

const codeSection =
    document.getElementById("code-section");

const successSection =
    document.getElementById("success-section");

const emailForm =
    document.getElementById("email-form");

const codeForm =
    document.getElementById("code-form");

const emailInput =
    document.getElementById("email");

const codeInput =
    document.getElementById("code");

const emailDisplay =
    document.getElementById("email-display");

const successEmail =
    document.getElementById("success-email");

const emailMessage =
    document.getElementById("email-message");

const codeMessage =
    document.getElementById("code-message");

const timer =
    document.getElementById("timer");

const changeEmailButton =
    document.getElementById("change-email");

const restartButton =
    document.getElementById("restart-button");

const sendCodeButton =
    document.getElementById("send-code-button");

const verifyCodeButton =
    document.getElementById("verify-code-button");

const historyContainer =
    document.getElementById("history-container");

const refreshHistoryButton =
    document.getElementById("refresh-history");


let currentEmail = "";

let countdownInterval = null;


// ======================================================
// FUNCIONES DE MENSAJES
// ======================================================

function showMessage(
    element,
    message,
    type
) {
    element.textContent = message;

    element.className =
        `message ${type}`;
}


function hideMessage(element) {
    element.className =
        "message hidden";

    element.textContent = "";
}


// ======================================================
// TEMPORIZADOR
// ======================================================

function startTimer() {

    let remainingSeconds = 600;

    clearInterval(countdownInterval);

    updateTimer(remainingSeconds);

    countdownInterval =
        setInterval(() => {

            remainingSeconds--;

            updateTimer(remainingSeconds);

            if (remainingSeconds <= 0) {

                clearInterval(
                    countdownInterval
                );

                timer.textContent =
                    "El código ha expirado.";

            }

        }, 1000);
}


function updateTimer(seconds) {

    const minutes =
        Math.floor(seconds / 60);

    const remaining =
        seconds % 60;

    timer.textContent =
        `Código válido durante ${minutes}:${remaining
            .toString()
            .padStart(2, "0")}`;
}


// ======================================================
// ENVIAR CÓDIGO
// ======================================================

emailForm.addEventListener(
    "submit",
    async (event) => {

        event.preventDefault();

        hideMessage(emailMessage);

        const email =
            emailInput.value.trim();

        if (!email) {

            showMessage(
                emailMessage,
                "Ingresa un correo electrónico.",
                "error"
            );

            return;
        }

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
                await response.json();

            if (!response.ok) {

                throw new Error(
                    data.message ||
                    "No fue posible enviar el código."
                );
            }

            currentEmail = email;

            emailDisplay.textContent =
                email;

            emailSection.classList.add(
                "hidden"
            );

            codeSection.classList.remove(
                "hidden"
            );

            codeInput.value = "";

            codeInput.focus();

            startTimer();

            await loadHistory();

        }
        catch (error) {

            showMessage(
                emailMessage,
                error.message,
                "error"
            );

        }
        finally {

            sendCodeButton.disabled = false;

            sendCodeButton.textContent =
                "Enviar código";
        }
    }
);


// ======================================================
// VERIFICAR CÓDIGO
// ======================================================

codeForm.addEventListener(
    "submit",
    async (event) => {

        event.preventDefault();

        hideMessage(codeMessage);

        const code =
            codeInput.value.trim();

        if (code.length !== 6) {

            showMessage(
                codeMessage,
                "El código debe tener 6 dígitos.",
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
                await response.json();

            if (!response.ok) {

                throw new Error(
                    data.message ||
                    "Código incorrecto."
                );
            }

            clearInterval(
                countdownInterval
            );

            successEmail.textContent =
                currentEmail;

            codeSection.classList.add(
                "hidden"
            );

            successSection.classList.remove(
                "hidden"
            );

            await loadHistory();

        }
        catch (error) {

            showMessage(
                codeMessage,
                error.message,
                "error"
            );

        }
        finally {

            verifyCodeButton.disabled = false;

            verifyCodeButton.textContent =
                "Verificar código";
        }
    }
);


// ======================================================
// CAMBIAR CORREO
// ======================================================

changeEmailButton.addEventListener(
    "click",
    () => {

        clearInterval(
            countdownInterval
        );

        codeSection.classList.add(
            "hidden"
        );

        emailSection.classList.remove(
            "hidden"
        );

        hideMessage(emailMessage);

        hideMessage(codeMessage);

        emailInput.focus();
    }
);


// ======================================================
// REINICIAR
// ======================================================

restartButton.addEventListener(
    "click",
    () => {

        clearInterval(
            countdownInterval
        );

        successSection.classList.add(
            "hidden"
        );

        emailSection.classList.remove(
            "hidden"
        );

        emailInput.value = "";

        codeInput.value = "";

        hideMessage(emailMessage);

        hideMessage(codeMessage);

        emailInput.focus();

        loadHistory();
    }
);


// ======================================================
// SOLO PERMITIR NÚMEROS
// ======================================================

codeInput.addEventListener(
    "input",
    () => {

        codeInput.value =
            codeInput.value
                .replace(/\D/g, "")
                .slice(0, 6);
    }
);


// ======================================================
// HISTORIAL
// ======================================================

async function loadHistory() {

    try {

        historyContainer.innerHTML = `
            <div class="history-empty">
                Cargando historial...
            </div>
        `;

        const response =
            await fetch(
                "/auth/history"
            );

        if (!response.ok) {

            throw new Error(
                "No se pudo cargar el historial."
            );
        }

        const history =
            await response.json();

        if (!history.length) {

            historyContainer.innerHTML = `
                <div class="history-empty">
                    No existen verificaciones registradas.
                </div>
            `;

            return;
        }

        historyContainer.innerHTML =
            history
                .map(item => {

                    const date =
                        new Date(
                            item.createdAt
                        ).toLocaleString(
                            "es-CO"
                        );

                    const statusClass =
                        item.status
                            .toLowerCase()
                            .normalize("NFD")
                            .replace(
                                /[\u0300-\u036f]/g,
                                ""
                            );

                    return `
                        <div class="history-row">

                            <div class="history-email">
                                ${escapeHtml(item.email)}
                            </div>

                            <div class="history-date">
                                ${date}
                            </div>

                            <div>
                                <span
                                    class="history-status ${statusClass}">
                                    ${escapeHtml(item.status)}
                                </span>
                            </div>

                            <div class="history-attempts">
                                ${item.attempts} intento(s)
                            </div>

                        </div>
                    `;
                })
                .join("");

    }
    catch (error) {

        historyContainer.innerHTML = `
            <div class="history-empty error">
                No fue posible cargar el historial.
            </div>
        `;

        console.error(
            "Error cargando historial:",
            error
        );
    }
}


// ======================================================
// SEGURIDAD BÁSICA PARA MOSTRAR TEXTO
// ======================================================

function escapeHtml(value) {

    const div =
        document.createElement("div");

    div.textContent =
        value ?? "";

    return div.innerHTML;
}


// ======================================================
// BOTÓN ACTUALIZAR HISTORIAL
// ======================================================

refreshHistoryButton.addEventListener(
    "click",
    loadHistory
);


// ======================================================
// CARGAR HISTORIAL AL ABRIR
// ======================================================

loadHistory();