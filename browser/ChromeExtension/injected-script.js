(() => {
    console.log("Injected script running");

    let loadingOverlay = null;

    // Function to create and get loading overlay
    function getLoadingOverlay() {
        if (loadingOverlay) {
            return loadingOverlay;
        }

        // Create and append loading overlay to document
        loadingOverlay = document.createElement("div");
        loadingOverlay.style.cssText = `
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.5);
    backdrop-filter: blur(4px);
    display: none;
    align-items: center;
    justify-content: center;
    z-index: 10000;
  `;

        const loadingContent = document.createElement("div");
        loadingContent.style.cssText = `
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
  `;

        const loadingText = document.createElement("div");
        loadingText.textContent = "Verifying content";
        loadingText.style.cssText = `
    color: white;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    font-size: 1.125rem;
    font-weight: 500;
  `;

        const spinner = document.createElement("div");
        spinner.style.cssText = `
    width: 50px;
    height: 50px;
    border: 5px solid #f3f3f3;
    border-top: 5px solid #3498db;
    border-radius: 50%;
    animation: spin 1s linear infinite;
  `;

        const style = document.createElement("style");
        style.textContent = `
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `;

        document.head.appendChild(style);
        loadingContent.appendChild(loadingText);
        loadingContent.appendChild(spinner);
        loadingOverlay.appendChild(loadingContent);

        return loadingOverlay;
    }

    // Function to show/hide loading overlay
    const toggleLoading = (show) => {
        console.log("toggleLoading:", show);

        if (!document.body) {
            console.log("Document body not available yet");
            return;
        }

        if (show) {
            const overlay = getLoadingOverlay();
            if (!overlay.parentElement) {
                document.body.appendChild(overlay);
            }
            overlay.style.display = "flex";
        } else if (loadingOverlay) {
            loadingOverlay.style.display = "none";
        }
    };

    // Function to generate unique IDs
    function generateId() {
        return Math.random().toString(36).substr(2, 9);
    }

    // Map to keep track of pending requests
    const pendingRequests = {};

    // Listen for messages from the content script
    window.addEventListener("message", function (event) {
        if (event.source !== window) {
            return;
        }

        const data = event.data;
        if (data && data.type === "PII_CHECK_RESULT") {
            const id = data.id;
            const result = data.result;
            const resolve = pendingRequests[id];
            if (resolve) {
                resolve(result);
                delete pendingRequests[id];
            }
        }
    });

    // Create XMLHttpRequest proxy for Gemini
    const XHR = XMLHttpRequest.prototype;
    const open = XHR.open;
    const send = XHR.send;

    XHR.open = function (method, url) {
        this._url = url;
        return open.apply(this, arguments);
    };

    // Add Gemini-specific message handling
    XHR.send = async function (postData) {
        // Check if this is a Gemini request
        if (
            this._url.includes(
                "/_/BardChatUi/data/assistant.lamda.BardFrontendService/StreamGenerate"
            )
        ) {
            console.log("Intercepted Gemini request");

            try {
                // Parse the form data
                const params = new URLSearchParams(postData);
                const reqData = params.get("f.req");

                if (reqData) {
                    const parsedReqData = JSON.parse(reqData);
                    console.log("First parse:", parsedReqData);

                    // The message is in a JSON string at index 1
                    if (
                        parsedReqData &&
                        Array.isArray(parsedReqData) &&
                        parsedReqData[1]
                    ) {
                        // Parse the inner JSON string
                        const innerData = JSON.parse(parsedReqData[1]);
                        console.log("Second parse:", innerData);

                        // Now the message should be in innerData[0][0]
                        if (Array.isArray(innerData) && Array.isArray(innerData[0])) {
                            const messageContent = innerData[0][0];
                            console.log("Message content:", messageContent);

                            if (messageContent) {
                                try {
                                    // Wait for PII check result
                                    const result = await checkPII(messageContent, "gemini");
                                    console.log("***PII check result:***", result);

                                    if (result.verified) {
                                        if (result.apiError) {
                                            const dialogResult = await showApiErrorDialog();
                                            if (dialogResult.action === "cancel") {
                                                // Send a safe message instead of canceling completely
                                                const safeMessage =
                                                    "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'";
                                                const updatedInnerData = innerData;
                                                updatedInnerData[0][0] = safeMessage;
                                                parsedReqData[1] = JSON.stringify(updatedInnerData);
                                                params.set("f.req", JSON.stringify(parsedReqData));
                                                postData = params.toString();
                                            }
                                        } else {
                                            // Add the original content to the result before showing the dialog
                                            result.originalContent = messageContent;

                                            const dialogResult = await showWarningDialog(result);
                                            if (dialogResult.action === "cancel") {
                                                // Send a safe message instead of canceling completely
                                                const safeMessage =
                                                    "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'";
                                                const updatedInnerData = innerData;
                                                updatedInnerData[0][0] = safeMessage;
                                                parsedReqData[1] = JSON.stringify(updatedInnerData);
                                                params.set("f.req", JSON.stringify(parsedReqData));
                                                postData = params.toString();
                                            } else if (dialogResult.action === "obfuscate") {
                                                // Replace the message content with obfuscated version
                                                const updatedInnerData = innerData;
                                                updatedInnerData[0][0] = dialogResult.content;
                                                parsedReqData[1] = JSON.stringify(updatedInnerData);
                                                params.set("f.req", JSON.stringify(parsedReqData));
                                                postData = params.toString();
                                            }
                                        }
                                    }
                                } catch (error) {
                                    console.error("Error in PII check:", error);
                                }
                            }
                        }
                    }
                }
            } catch (error) {
                console.error("Error processing Gemini request:", error);
            }
        }

        // Proceed with the send
        return send.call(this, postData);
    };

    // Function to check PII
    function checkPII(content, source = null) {
        console.log("Checking PII:", content);
        toggleLoading(true);
        return new Promise((resolve) => {
            const id = generateId();
            // pendingRequests[id] = resolve;
            pendingRequests[id] = (result) => {
                toggleLoading(false);
                resolve(result);
            };
            window.postMessage(
                { type: "PII_CHECK", id: id, content: content, source: source },
                "*"
            );
        });
    }
    // Function to show warning dialog
    function showWarningDialog(verificationResult) {
        return new Promise((resolve) => {
            const modalContainer = document.createElement("div");
            modalContainer.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
      `;

            const modalContent = document.createElement("div");
            modalContent.style.cssText = `
        background: #FFFFFF;
        color: #000000;
        padding: 1.5rem;
        border-radius: 0.5rem;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        max-width: 36rem;
        width: 90%;
        max-height: 80vh;
        overflow-y: auto;
      `;

            // Create warning header
            const header = document.createElement("h3");
            header.style.cssText = `
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 1rem;
        color: #000000;
      `;
            header.textContent = "Warning";

            // Create warning message
            const message = document.createElement("p");
            message.style.cssText = `
        margin-bottom: 1.5rem;
        color: #000000;
        line-height: 1.5;
      `;
            message.textContent =
                "Sensitive content detected. Please review the detected entities below:";

            // Create review message
            const review = document.createElement("p");
            review.textContent = verificationResult.review;

            // Create detected entities list
            const listContainer = document.createElement("div");
            if (
                verificationResult.replacements &&
                verificationResult.replacements.length > 0
            ) {
                listContainer.style.cssText = `margin-bottom: 1.5rem;`;

                const listHeader = document.createElement("h4");
                listHeader.style.cssText = `
          font-size: 1rem;
          font-weight: 600;
          margin-bottom: 0.75rem;
          color: #000000;
        `;
                listHeader.textContent = "Detected Sensitive Information:";

                const list = document.createElement("ul");
                list.style.cssText = `
          list-style-type: none;
          padding: 0;
          margin: 0;
          display: flex;
          flex-wrap: wrap;
          gap: 0.5rem;
        `;

                verificationResult.replacements.forEach((replacement) => {
                    const item = document.createElement("li");
                    item.style.cssText = `
            background: #f1f5f9;
            padding: 0.5rem 1rem;
            border-radius: 0.375rem;
            font-size: 0.875rem;
            color: #1e293b;
          `;
                    item.textContent = verificationResult.originalContent.substring(
                        replacement.start,
                        replacement.end
                    );
                    list.appendChild(item);
                });

                listContainer.appendChild(listHeader);
                listContainer.appendChild(list);
            }

            // Create buttons container
            const buttonsContainer = document.createElement("div");
            buttonsContainer.style.cssText = `
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1rem;
      `;

            // Create buttons
            const buttons = [
                {
                    id: "cancelBtn",
                    text: "Cancel",
                    style: "background: #e5e7eb; color: #000000;",
                },
                {
                    id: "obfuscateBtn",
                    text: "Obfuscate",
                    style: "background: #3b82f6; color: #ffffff;",
                },
                {
                    id: "continueBtn",
                    text: "Continue Anyway",
                    style: "background: #ef4444; color: #ffffff;",
                },
            ].map(({ id, text, style }) => {
                const button = document.createElement("button");
                button.style.cssText = `
          padding: 0.5rem 1rem;
          border: none;
          border-radius: 0.375rem;
          font-weight: 500;
          cursor: pointer;
          transition: background-color 0.2s;
          ${style}
        `;
                button.textContent = text;
                button.addEventListener("click", () => {
                    document.body.removeChild(modalContainer);
                    resolve({
                        action:
                            id === "cancelBtn"
                                ? "cancel"
                                : id === "obfuscateBtn"
                                    ? "obfuscate"
                                    : "continue",
                        content: verificationResult.content,
                    });
                });
                return button;
            });

            // Assemble the modal
            buttons.forEach((button) => buttonsContainer.appendChild(button));
            modalContent.appendChild(header);
            modalContent.appendChild(message);
            modalContent.appendChild(review);
            modalContent.appendChild(listContainer);
            modalContent.appendChild(buttonsContainer);
            modalContainer.appendChild(modalContent);
            document.body.appendChild(modalContainer);
        });
    }

    // Function to show API error dialog
    function showApiErrorDialog() {
        return new Promise((resolve) => {
            const modalContainer = document.createElement("div");
            modalContainer.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 10000;
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
      `;

            const modalContent = document.createElement("div");
            modalContent.style.cssText = `
        background: #FFFFFF;
        color: #000000;
        padding: 1.5rem;
        border-radius: 0.5rem;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        max-width: 36rem;
        width: 90%;
      `;

            // Create header container
            const headerContainer = document.createElement("div");
            headerContainer.style.cssText = `
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
      `;

            // Create icon container
            const iconContainer = document.createElement("div");
            iconContainer.style.cssText = `
        background: #fee2e2;
        padding: 0.5rem;
        border-radius: 50%;
      `;

            // Create SVG icon
            const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
            svg.setAttribute("width", "24");
            svg.setAttribute("height", "24");
            svg.setAttribute("viewBox", "0 0 24 24");
            svg.setAttribute("fill", "none");
            svg.setAttribute("stroke", "#dc2626");
            svg.setAttribute("stroke-width", "2");

            const path = document.createElementNS(
                "http://www.w3.org/2000/svg",
                "path"
            );
            path.setAttribute(
                "d",
                "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            );
            svg.appendChild(path);
            iconContainer.appendChild(svg);

            // Create header
            const header = document.createElement("h3");
            header.style.cssText = `
        font-size: 1.25rem;
        font-weight: 600;
        margin: 0;
        color: #000000;
      `;
            header.textContent = "Warning: Redactor Service Unavailable";

            // Create message
            const message = document.createElement("p");
            message.style.cssText = `
        margin-bottom: 1.5rem;
        color: #000000;
        line-height: 1.5;
      `;
            message.textContent =
                "The Redactor service is currently unavailable. Any sensitive information in your message will not be checked. Please proceed with caution.";

            // Create buttons container
            const buttonsContainer = document.createElement("div");
            buttonsContainer.style.cssText = `
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
      `;

            // Create buttons
            const buttons = [
                {
                    id: "cancelBtn",
                    text: "Cancel",
                    style: "background: #e5e7eb; color: #000000;",
                },
                {
                    id: "continueBtn",
                    text: "Continue Anyway",
                    style: "background: #ef4444; color: #ffffff;",
                },
            ].map(({ id, text, style }) => {
                const button = document.createElement("button");
                button.style.cssText = `
          padding: 0.5rem 1rem;
          border: none;
          border-radius: 0.375rem;
          font-weight: 500;
          cursor: pointer;
          transition: background-color 0.2s;
          ${style}
        `;
                button.textContent = text;
                button.addEventListener("click", () => {
                    document.body.removeChild(modalContainer);
                    resolve({ action: id === "cancelBtn" ? "cancel" : "continue" });
                });

                // Add hover effects
                button.addEventListener("mouseover", () => {
                    button.style.backgroundColor =
                        id === "cancelBtn" ? "#d1d5db" : "#dc2626";
                });
                button.addEventListener("mouseout", () => {
                    button.style.backgroundColor =
                        id === "cancelBtn" ? "#e5e7eb" : "#ef4444";
                });

                return button;
            });

            // Assemble the modal
            headerContainer.appendChild(iconContainer);
            headerContainer.appendChild(header);
            buttons.forEach((button) => buttonsContainer.appendChild(button));
            modalContent.appendChild(headerContainer);
            modalContent.appendChild(message);
            modalContent.appendChild(buttonsContainer);
            modalContainer.appendChild(modalContent);
            document.body.appendChild(modalContainer);
        });
    }

    // Form submission handling
    async function handleFormSubmit(event) {
        console.log("Form submit intercepted");
        event.preventDefault();
        const form = event.target;
        const formData = new FormData(form);
        const jsonData = {};
        const ignoreFields = [
            "__RequestVerificationToken",
            "CSRF-Token",
            "_csrf",
            "authenticity_token",
            // Add any other fields you want to ignore
        ];
        formData.forEach((value, key) => {
            if (typeof value === "string" && !ignoreFields.includes(key)) {
                jsonData[key] = value;
            }
        });
        try {
            const result = await checkPII(JSON.stringify(jsonData));
            console.log("PII Result:", result);
            if (result.apiError) {
                console.error("Error during PII check");
                const dialogResult = await showApiErrorDialog();
                if (dialogResult.action === "cancel") {
                    console.log("User cancelled form submission");
                    return;
                }
            } else if (result.verified) {
                result.originalContent = JSON.stringify(jsonData);
                const dialogResult = await showWarningDialog(result);
                if (dialogResult.action === "cancel") {
                    console.log("User cancelled form submission");
                    return;
                } else if (dialogResult.action === "obfuscate") {
                    // Replace form values with obfuscated content
                    const obfuscatedData = JSON.parse(dialogResult.content);
                    for (let key in obfuscatedData) {
                        const input = form.querySelector(`[name="${key}"]`);
                        if (input) {
                            input.value = obfuscatedData[key];
                        }
                    }
                }
            }

            // Submit the form
            submitFormDirectly(form);
        } catch (error) {
            console.error("Error during form submission:", error);
        }
    }

    function submitFormDirectly(form) {
        const submitEvent = new Event("submit", {
            bubbles: true,
            cancelable: true,
        });
        form.removeEventListener("submit", handleFormSubmit);
        form._submit = form.submit;
        form.submit = () => {
            form.dispatchEvent(submitEvent);
        };
        form._submit();
        form.submit = form._submit;
        delete form._submit;
    }

    // Attach form listeners
    function attachFormListeners() {
        const forms = document.querySelectorAll("form");
        forms.forEach((form) => {
            if (!form.hasAttribute("data-pii-listener")) {
                form.setAttribute("data-pii-listener", "true");
                form.addEventListener("submit", handleFormSubmit);
            }
        });
    }

    // Setup mutation observer for dynamically added forms
    function setupMutationObserver() {
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.addedNodes.length) {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeName === "FORM") {
                            attachFormListeners();
                        }
                    });
                }
            });
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true,
        });
    }

    // Override window.fetch
    const originalFetch = window.fetch;
    window.fetch = async function (resource, init) {
        console.log("Fetch intercepted:", resource);

        // Check for ChatGPT endpoints
        if (
            resource.includes("/backend-anon/conversation") ||
            resource.includes("/backend-api/conversation")
        ) {
            console.log("ChatGPT conversation intercepted");

            try {
                const body = init?.body ? JSON.parse(init.body) : {};
                console.log("Request body:", body);

                // Check for messages
                if (body.messages?.length > 0) {
                    const message = body.messages[body.messages.length - 1];
                    if (message.content?.parts?.length > 0) {
                        const content = message.content.parts.join(" ");
                        console.log("Message to verify:", content);

                        const result = await checkPII(content);
                        console.log("Verification result:", result);
                        if (result.apiError) {
                            console.error("Error during PII check");
                            const dialogResult = await showApiErrorDialog();
                            if (dialogResult.action === "cancel") {
                                // Send a safe message instead of throwing an error
                                const parsedBody = JSON.parse(init.body);
                                parsedBody.messages[
                                    parsedBody.messages.length - 1
                                ].content.parts = [
                                        "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'",
                                    ];
                                init.body = JSON.stringify(parsedBody);
                            }
                        } else if (result.verified) {
                            result.originalContent = content;
                            const dialogResult = await showWarningDialog(result);
                            if (dialogResult.action === "cancel") {
                                // Send a safe message instead of throwing an error
                                const parsedBody = JSON.parse(init.body);
                                parsedBody.messages[
                                    parsedBody.messages.length - 1
                                ].content.parts = [
                                        "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'",
                                    ];
                                init.body = JSON.stringify(parsedBody);
                            } else if (dialogResult.action === "obfuscate") {
                                // Replace the message content with obfuscated version
                                const parsedBody = JSON.parse(init.body);
                                parsedBody.messages[
                                    parsedBody.messages.length - 1
                                ].content.parts = [dialogResult.content];
                                console.log("Obfuscated Message:", parsedBody);
                                init.body = JSON.stringify(parsedBody);
                            }
                        }
                    }
                }
            } catch (error) {
                console.error("Error during verification:", error);
            }
        }

        return originalFetch.apply(this, arguments);
    };

    // Override WebSocket
    const OriginalWebSocket = window.WebSocket;
    window.WebSocket = function (...args) {
        const ws = new OriginalWebSocket(...args);
        console.log("WebSocket created:", args[0]);

        const originalSend = ws.send;
        ws.send = async function (data) {
            console.log("WebSocket send intercepted:", data);
            try {
                let messageData = typeof data === "string" ? JSON.parse(data) : data;
                console.log("Parsed message:", messageData);

                if (messageData.messages && Array.isArray(messageData.messages)) {
                    const message = messageData.messages[messageData.messages.length - 1];
                    if (message.content?.parts?.length > 0) {
                        const content = message.content.parts.join(" ");
                        console.log("WebSocket message to verify:", content);

                        const result = await checkPII(content);
                        if (result.apiError) {
                            console.error("Error during PII check");
                            const dialogResult = await showApiErrorDialog();
                            if (dialogResult.action === "cancel") {
                                messageData.messages[
                                    messageData.messages.length - 1
                                ].content.parts = [
                                        "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'",
                                    ];
                                data = JSON.stringify(messageData);
                            }
                        } else if (result.verified) {
                            result.originalContent = content;
                            const dialogResult = await showWarningDialog(result);
                            if (dialogResult.action === "cancel") {
                                messageData.messages[
                                    messageData.messages.length - 1
                                ].content.parts = [
                                        "Respond with this text: 'I cannot process this request due to potential sensitive information. Please try rephrasing your message without including any personal or confidential data.'",
                                    ];
                                data = JSON.stringify(messageData);
                            } else if (dialogResult.action === "obfuscate") {
                                // Replace the message content with obfuscated version
                                messageData.messages[
                                    messageData.messages.length - 1
                                ].content.parts = [dialogResult.content];
                                console.log("Obfuscated WebSocket message:", messageData);
                                data = JSON.stringify(messageData);
                            }
                        }
                    }
                }
            } catch (error) {
                console.error("Error in WebSocket send:", error);
            }

            return originalSend.call(this, data);
        };

        return ws;
    };

    Object.assign(window.WebSocket, OriginalWebSocket);
    window.WebSocket.prototype = OriginalWebSocket.prototype;

    // Initialize form handling
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => {
            attachFormListeners();
            setupMutationObserver();
        });
    } else {
        attachFormListeners();
        setupMutationObserver();
    }
})();
