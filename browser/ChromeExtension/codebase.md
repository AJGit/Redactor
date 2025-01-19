# background.js

```js
chrome.sidePanel
    .setPanelBehavior({ openPanelOnActionClick: true })
    .catch((error) => console.error(error));

console.log('Background script loaded');
```

# content-script.js

```js
console.log("Content script loaded");

// Default values
let apiEndpoint = "http://localhost:5001/api/Verify";
let defaultThreshold = 0.4;
let defaultStartTag = "{{";
let defaultEndTag = "}}";
let defaultReplacementType = "EntityType";
let defaultLanguage = "en";

// Load configuration
chrome.storage.sync.get(
  {
    apiEndpoint: "http://localhost:5001/api/Verify",
    defaultThreshold: 0.4,
    defaultStartTag: "{{",
    defaultEndTag: "}}",
    defaultReplacementType: "EntityType",
    defaultLanguage: "en",
  },
  (items) => {
    apiEndpoint = items.apiEndpoint;
    defaultThreshold = items.defaultThreshold;
    defaultStartTag = items.defaultStartTag;
    defaultEndTag = items.defaultEndTag;
    defaultReplacementType = items.defaultReplacementType;
    defaultLanguage = items.defaultLanguage;
    console.log("Configuration loaded:", items);
  }
);

// Function to inject the script into the page
function injectScript() {
  const script = document.createElement("script");
  script.src = chrome.runtime.getURL("injected-script.js");
  script.onload = function () {
    this.remove();
  };
  (document.head || document.documentElement).appendChild(script);
}

// Inject the script into the page context
injectScript();

// Listen for messages from the injected script
window.addEventListener("message", async (event) => {
  if (event.source !== window) {
    return;
  }

  const data = event.data;
  if (data && data.type === "PII_CHECK") {
    const id = data.id;
    const content = data.content;

    try {
      const response = await fetch(apiEndpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          content: content,
          url: window.location.href,
          threshold: defaultThreshold,
          startTag: defaultStartTag,
          endTag: defaultEndTag,
          replacementType: defaultReplacementType,
          language: defaultLanguage,
        }),
      });

      const result = await response.json();
      // Send the result back to the injected script
      window.postMessage(
        { 
          type: "PII_CHECK_RESULT", 
          id: id, 
          result: result,
          source: data.source // Pass through the source if it exists
        },
        "*"
      );
    } catch (error) {
      console.error("Error during PII check:", error);
      window.postMessage(
        {
          type: "PII_CHECK_RESULT",
          id: id,
          result: { verified: true, apiError: true },
          source: data.source // Pass through the source if it exists
        },
        "*"
      );
    }
  }
});
```

# icons\icon16.png

This is a binary file of the type: Image

# icons\icon48.png

This is a binary file of the type: Image

# icons\icon128.png

This is a binary file of the type: Image

# injected-script.js

```js
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

```

# manifest.json

```json
{
  "manifest_version": 3,
  "name": "Redactor",
  "version": "1.0",
  "permissions": [
    "activeTab",
    "scripting",
    "sidePanel",
    "storage",
    "tabs",
    "alarms",
    "webNavigation",
    "identity",
    "notifications",
    "identity.email"
  ],
  "host_permissions": [
    "http://localhost:5001/*",
    "http://localhost:5287/*",
    "*://chatgpt.com/*",
    "https://*.chatgpt.com/*",
    "https://*.openai.com/*",
    "https://*.claude.ai/*",
    "https://claude.ai/*",
    "https://*.jasper.ai/*",
    "https://gemini.google.com/*",
    "https://copilot.microsoft.com/*",
    "https://*.perplexity.ai/*",
    "https://*.meta.ai/*"

  ],
  "action": {
    "default_title": "PII Redactor"
  },
  "options_page": "options.html",
  "content_scripts": [{
    "matches": [
      "http://localhost:5287/*",
      "*://*.chatgpt.com/*",
      "*://chatgpt.com/*",
      "https://*.openai.com/*",
      "https://chat.openai.com/*",
      "https://*.claude.ai/*",
      "https://claude.ai/*",
      "https://*.jasper.ai/*",
      "https://gemini.google.com/*",
      "https://copilot.microsoft.com/*",
      "https://*.perplexity.ai/*",
      "https://*.meta.ai/*"
    ],
    "js": ["content-script.js"],
    "run_at": "document_start"
  }],
  "web_accessible_resources": [{
    "resources": ["injected-script.js"],
    "matches": [
      "http://localhost:5287/*",
      "https://*.chatgpt.com/*",
      "https://*.openai.com/*",
      "https://chat.openai.com/*",
      "https://*.claude.ai/*",
      "https://*.jasper.ai/*",
      "https://*.meta.ai/*",
      "https://*.perplexity.ai/*",
      "https://claude.ai/*",
      "https://copilot.microsoft.com/*",
      "https://gemini.google.com/*"
    ]
  }],
  "side_panel": {
    "default_path": "sidepanel.html"
  },
  "icons": {
    "16": "icons/icon16.png",
    "48": "icons/icon48.png",
    "128": "icons/icon128.png"
  },
  "background": {
    "service_worker": "background.js"
  }
}
```

# options.html

```html
<!DOCTYPE html>
<html>
<head>
  <title>Redactor Options</title>
  <style>
    body {
      padding: 20px;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
      background-color: #f8fafc;
      color: #0f172a;
    }
    .container {
      max-width: 800px;
      margin: 0 auto;
    }
    .form-section {
      background: white;
      padding: 24px;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
      margin-bottom: 24px;
    }
    .form-section h3 {
      margin-top: 0;
      margin-bottom: 16px;
      color: #334155;
      font-size: 1.25rem;
    }
    .form-group {
      margin-bottom: 24px;
    }
    .form-group:last-child {
      margin-bottom: 0;
    }
    label {
      display: block;
      margin-bottom: 8px;
      font-weight: 500;
      color: #334155;
    }
    input[type="text"], select {
      width: 100%;
      padding: 10px;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      font-size: 14px;
      transition: all 0.2s;
      box-sizing: border-box;
    }
    input[type="text"]:focus, select:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
    }
    .range-group {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    input[type="range"] {
      flex-grow: 1;
    }
    .range-value {
      min-width: 48px;
      padding: 4px 8px;
      background: #f1f5f9;
      border-radius: 4px;
      text-align: center;
      font-size: 14px;
    }
    .button-group {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
    }
    button {
      padding: 10px 20px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }
    .btn-save {
      background-color: #3b82f6;
      color: white;
    }
    .btn-save:hover {
      background-color: #2563eb;
    }
    .btn-reset {
      background-color: #f1f5f9;
      color: #64748b;
      border: 1px solid #cbd5e1;
    }
    .btn-reset:hover {
      background-color: #e2e8f0;
      border-color: #94a3b8;
    }
    .status {
      margin-top: 16px;
      padding: 12px;
      border-radius: 6px;
      display: none;
      text-align: center;
    }
    .success {
      background-color: #dcfce7;
      color: #166534;
    }
  </style>
</head>
<body>
  <div class="container">
    <h2><span style='color: red;'>Red</span>actor Settings</h2>
    
    <div class="form-section">
      <h3>Detection Settings</h3>
      <div class="form-group">
        <label for="defaultThreshold">Default Confidence Threshold:</label>
        <div class="range-group">
          <input type="range" id="defaultThreshold" min="0" max="1" step="0.05" value="0.4">
          <span class="range-value" id="thresholdValue">40%</span>
        </div>
      </div>
      <div class="form-group">
        <label for="defaultReplacementType">Default Replacement Type:</label>
        <select id="defaultReplacementType">
          <option value="EntityType">Entity Type</option>
          <option value="Original">Original</option>
          <option value="Fake">Fake</option>
          <option value="Obfuscated">Obfuscated</option>
        </select>
      </div>
      <div class="form-group">
        <label for="defaultStartTag">Start Tag:</label>
        <input type="text" id="defaultStartTag" placeholder="{{">
      </div>
      <div class="form-group">
        <label for="defaultEndTag">End Tag:</label>
        <input type="text" id="defaultEndTag" placeholder="}}">
      </div>
      <div class="form-group">
        <label for="defaultLanguage">Language:</label>
        <input type="text" id="defaultLanguage" placeholder="en">
      </div>
    </div>

    <div class="form-section">
      <h3>API Endpoints</h3>
      <div class="form-group">
        <label for="apiEndpoint">Verify Endpoint:</label>
        <input type="text" id="apiEndpoint" placeholder="http://localhost:5001/api/Verify">
      </div>
      <div class="form-group">
        <label for="reviewEndpoint">Review Endpoint:</label>
        <input type="text" id="reviewEndpoint" placeholder="http://localhost:5001/api/Review">
      </div>
      <div class="form-group">
        <label for="filesEndpoint">Files Endpoint:</label>
        <input type="text" id="filesEndpoint" placeholder="http://localhost:5001/api/Files">
      </div>
    </div>

    <div class="button-group">
      <button id="reset" class="btn-reset">Reset to Defaults</button>
      <button id="save" class="btn-save">Save Settings</button>
    </div>
    
    <div id="status" class="status"></div>
  </div>
  <script src="options.js"></script>
</body>
</html>
```

# options.js

```js
// Default values
const defaults = {
  apiEndpoint: 'http://localhost:5001/api/Verify',
  reviewEndpoint: 'http://localhost:5001/api/Review',
  filesEndpoint: 'http://localhost:5001/api/Files',
  defaultThreshold: '0.4',
  defaultReplacementType: 'EntityType',
  defaultStartTag: '{{',
  defaultEndTag: '}}',
  defaultLanguage: 'en'
};

// Update threshold value display
document.getElementById('defaultThreshold').addEventListener('input', (e) => {
  document.getElementById('thresholdValue').textContent = 
    `${(e.target.value * 100).toFixed(0)}%`;
});

// Saves options to chrome.storage
function saveOptions() {
  const apiEndpoint = document.getElementById('apiEndpoint').value;
  const reviewEndpoint = document.getElementById('reviewEndpoint').value;
  const filesEndpoint = document.getElementById('filesEndpoint').value;
  const defaultThreshold = document.getElementById('defaultThreshold').value;
  const defaultReplacementType = document.getElementById('defaultReplacementType').value;
  const defaultStartTag = document.getElementById('defaultStartTag').value;
  const defaultEndTag = document.getElementById('defaultEndTag').value;
  const defaultLanguage = document.getElementById('defaultLanguage').value;

  chrome.storage.sync.set(
    {
      apiEndpoint: apiEndpoint || defaults.apiEndpoint,
      reviewEndpoint: reviewEndpoint || defaults.reviewEndpoint,
      filesEndpoint: filesEndpoint || defaults.filesEndpoint,
      defaultThreshold: defaultThreshold || defaults.defaultThreshold,
      defaultReplacementType: defaultReplacementType || defaults.defaultReplacementType,
      defaultStartTag: defaultStartTag || defaults.defaultStartTag,
      defaultEndTag: defaultEndTag || defaults.defaultEndTag,
      defaultLanguage: defaultLanguage || defaults.defaultLanguage
    },
    () => {
      showStatus('Settings saved successfully');
    }
  );
}

// Reset options to defaults
function resetOptions() {
  // Set form values to defaults
  document.getElementById('apiEndpoint').value = defaults.apiEndpoint;
  document.getElementById('reviewEndpoint').value = defaults.reviewEndpoint;
  document.getElementById('filesEndpoint').value = defaults.filesEndpoint;
  document.getElementById('defaultThreshold').value = defaults.defaultThreshold;
  document.getElementById('defaultReplacementType').value = defaults.defaultReplacementType;
  document.getElementById('defaultStartTag').value = defaults.defaultStartTag;
  document.getElementById('defaultEndTag').value = defaults.defaultEndTag;
  document.getElementById('defaultLanguage').value = defaults.defaultLanguage;
  
  // Update the threshold display
  document.getElementById('thresholdValue').textContent = 
    `${(defaults.defaultThreshold * 100).toFixed(0)}%`;

  // Save to storage
  chrome.storage.sync.set(defaults, () => {
    showStatus('Settings reset to defaults');
  });
}

// Show status message
function showStatus(message) {
  const status = document.getElementById('status');
  status.textContent = message;
  status.style.display = 'block';
  status.className = 'status success';
  setTimeout(() => {
    status.style.display = 'none';
  }, 2000);
}

// Restores select box and checkbox state using the preferences
// stored in chrome.storage.
function restoreOptions() {
  chrome.storage.sync.get(defaults, (items) => {
    document.getElementById('apiEndpoint').value = items.apiEndpoint;
    document.getElementById('reviewEndpoint').value = items.reviewEndpoint;
    document.getElementById('filesEndpoint').value = items.filesEndpoint;
    document.getElementById('defaultThreshold').value = items.defaultThreshold;
    document.getElementById('defaultReplacementType').value = items.defaultReplacementType;
    document.getElementById('defaultStartTag').value = items.defaultStartTag;
    document.getElementById('defaultEndTag').value = items.defaultEndTag;
    document.getElementById('defaultLanguage').value = items.defaultLanguage;
    
    // Update the threshold display
    document.getElementById('thresholdValue').textContent = 
      `${(items.defaultThreshold * 100).toFixed(0)}%`;
  });
}

document.addEventListener('DOMContentLoaded', restoreOptions);
document.getElementById('save').addEventListener('click', saveOptions);
document.getElementById('reset').addEventListener('click', () => {
  if (confirm('Are you sure you want to reset all settings to their defaults?')) {
    resetOptions();
  }
});
```

# readme.md

```md
# Update the codebase.md
\`\`\`json
npx ai-digest
\`\`\`
https://github.com/khromov/ai-digest

# Endpoints:

## ChatGpt:
https://chat.openai.com/backend-api/conversation
https://chat.openai.com/backend-anon/conversation

## Perplexity:
https://www.perplexity.ai/

## Gemini:

## Anthropic:

```

# sidepanel.html

```html
<!DOCTYPE html>
<html>
  <head>
    <meta charset="UTF-8" />
    <title>Personally Identifiable Information</title>
    <style>
      :root {
        --primary-color: #0f172a;
        --secondary-color: #334155;
        --accent-color: #3b82f6;
        --background-color: #f8fafc;
        --card-background: #ffffff;
        --border-color: #e2e8f0;
        --shadow-color: rgba(0, 0, 0, 0.05);
      }

      body {
        font-family: system-ui, -apple-system, sans-serif;
        margin: 0;
        padding: 16px;
        background-color: var(--background-color);
        color: var(--primary-color);
      }

      .tab-navigation {
        display: flex;
        margin-bottom: 24px;
      }

      .tab-button {
        flex: 1;
        padding: 12px;
        background: none;
        border: none;
        border-bottom: 2px solid var(--border-color);
        color: var(--secondary-color);
        font-size: 14px;
        cursor: pointer;
        transition: all 0.2s;
      }

      .tab-button.active {
        border-bottom-color: var(--accent-color);
        color: var(--accent-color);
        font-weight: 500;
      }

      .tab-content {
        display: none;
      }

      .tab-content.active {
        display: block;
      }

      .card {
        background: var(--card-background);
        border-radius: 12px;
        box-shadow: 0 1px 3px var(--shadow-color);
        padding: 20px;
        margin-bottom: 20px;
      }

      .form-group {
        margin-bottom: 16px;
      }

      .form-label {
        display: block;
        margin-bottom: 8px;
        font-weight: 500;
        font-size: 14px;
        color: var(--secondary-color);
      }

      .form-control {
        width: 100%;
        padding: 10px;
        border: 1px solid var(--border-color);
        border-radius: 8px;
        font-size: 14px;
        transition: all 0.2s;
        box-sizing: border-box;
      }

      textarea.form-control {
        min-height: 120px;
        line-height: 1.5;
        resize: vertical;
      }

      .form-control:focus {
        outline: none;
        border-color: var(--accent-color);
        box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
      }

      .file-drop-area {
        border: 2px dashed var(--border-color);
        border-radius: 8px;
        padding: 24px;
        text-align: center;
        cursor: pointer;
        transition: all 0.2s;
      }

      .file-drop-area.dragover {
        border-color: var(--accent-color);
        background-color: rgba(59, 130, 246, 0.05);
      }

      .btn {
        padding: 10px 20px;
        border: none;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;
      }

      .btn-primary {
        background-color: var(--accent-color);
        color: white;
      }

      .btn-primary:hover {
        background-color: #2563eb;
      }

      .results {
        margin-top: 20px;
      }

      .review-text {
        color: var(--secondary-color);
        font-size: 14px;
        margin-bottom: 12px;
      }

      .content-preview {
        background-color: var(--background-color);
        padding: 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        overflow-x: auto;
      }

      .table-container {
        overflow-x: auto;
      }

      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }

      th,
      td {
        padding: 12px;
        text-align: left;
        border-bottom: 1px solid var(--border-color);
      }

      th {
        background-color: var(--background-color);
        font-weight: 500;
        color: var(--secondary-color);
      }

      .hidden {
        display: none;
      }

      .copy-btn {
        padding: 6px 12px;
        background-color: var(--background-color);
        border: 1px solid var(--border-color);
        border-radius: 6px;
        font-size: 12px;
        color: var(--secondary-color);
        cursor: pointer;
        transition: all 0.2s;
      }

      .copy-btn:hover {
        background-color: #f1f5f9;
        border-color: var(--secondary-color);
      }

      .content-preview {
        background-color: var(--background-color);
        padding: 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        overflow-x: auto;
        white-space: pre-wrap;
        line-height: 1.6;
        font-size: 14px;
      }

      .content-preview span {
        font-weight: 500;
        padding: 2px 0;
      }

      /* Add some spacing for lists within the preview */
      .content-preview ul,
      .content-preview ol {
        margin: 8px 0;
        padding-left: 24px;
      }

      .content-preview li {
        margin: 4px 0;
      }

      .button-group {
        display: flex;
        gap: 8px;
        margin-top: 16px;
      }

      .btn-secondary {
        background-color: #e2e8f0;
        color: var(--secondary-color);
      }

      .btn-secondary:hover {
        background-color: #cbd5e1;
      }

      .file-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 8px 12px;
        background-color: var(--background-color);
        border-radius: 6px;
        margin-bottom: 8px;
        font-size: 14px;
      }

      .file-item:last-child {
        margin-bottom: 0;
      }

      .remove-file {
        color: #ef4444;
        cursor: pointer;
        font-weight: bold;
        padding: 0 8px;
      }

      .file-drop-area {
        border: 2px dashed var(--border-color);
        border-radius: 8px;
        padding: 24px;
        text-align: center;
        cursor: pointer;
        transition: all 0.2s;
        margin-bottom: 16px;
        color: var(--secondary-color);
        font-size: 14px;
      }

      .file-drop-area.dragover {
        border-color: var(--accent-color);
        background-color: rgba(59, 130, 246, 0.05);
      }

      .file-list {
        margin: 16px 0;
      }

      .range-group {
        display: flex;
        align-items: center;
        gap: 16px;
      }
      input[type="range"] {
        flex-grow: 1;
      }
      .range-value {
        min-width: 48px;
        padding: 4px 8px;
        background: #f1f5f9;
        border-radius: 4px;
        text-align: center;
        font-size: 14px;
      }
      .file-results {
        background-color: #f8fafc;
        padding: 1rem;
        border-radius: 0.5rem;
      }

      .file-card {
        background: white;
        border-radius: 0.75rem;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        margin-bottom: 1rem;
        overflow: hidden;
      }

      .file-card-header {
        padding: 1rem;
        border-bottom: 1px solid #e5e7eb;
      }

      .file-card-title {
        font-size: 1.125rem;
        font-weight: 600;
        color: #111827;
        margin-bottom: 0.5rem;
      }

      .file-card-review {
        font-size: 0.875rem;
        color: #6b7280;
      }

      .file-card-content {
        padding: 1rem;
      }

      .page-section {
        margin-bottom: 1rem;
      }

      .page-section:last-child {
        margin-bottom: 0;
      }

      .page-title {
        font-size: 0.875rem;
        font-weight: 500;
        color: #374151;
        margin-bottom: 0.5rem;
      }

      .issues-container {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .issue-tag {
        display: inline-flex;
        align-items: center;
        padding: 0.375rem 0.75rem;
        background-color: #f3f4f6;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 500;
        color: #374151;
        transition: background-color 0.2s;
      }

      .issue-tag:hover {
        background-color: #e5e7eb;
      }

      .issue-type {
        color: #6b7280;
        margin-left: 0.25rem;
      }
    </style>
  </head>
  <body>
    <div class="tab-navigation">
      <button class="tab-button active" data-tab="text">Text Review</button>
      <button class="tab-button" data-tab="files">File Review</button>
    </div>

    <div id="textTab" class="tab-content active">
      <div class="card">
        <div class="form-group">
          <label class="form-label">Review Text</label>
          <textarea
            id="reviewText"
            class="form-control"
            placeholder="Enter text to review..."
          ></textarea>
        </div>

        <div class="form-group">
          <label for="threshold">Confidence Threshold:</label>
          <div class="range-group">
            <input
              type="range"
              id="threshold"
              min="0"
              max="1"
              step="0.05"
              value="0.4"
            />
            <span class="range-value" id="thresholdValue">40%</span>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">Replacement Type</label>
          <select id="replacementType" class="form-control">
            <option value="EntityType">Entity Type</option>
            <option value="Original">Original</option>
            <option value="Fake">Fake</option>
            <option value="Obfuscated">Obfuscated</option>
          </select>
        </div>

        <div class="button-group">
          <button id="reviewButton" class="btn btn-primary">
            Review Content
          </button>
          <button id="clearTextButton" class="btn btn-secondary">Clear</button>
        </div>
      </div>

      <div id="textResultsContainer" class="card hidden">
        <div
          style="
            display: flex;
            justify-content: space-between;
            align-items: center;
          "
        >
          <h3 style="margin: 0">Results</h3>
          <button id="textCopyButton" class="copy-btn">Copy Text</button>
        </div>
        <div class="results">
          <div id="textReviewMessage" class="review-text"></div>
          <div id="textContentPreview" class="content-preview"></div>
          <div id="textReplacementsContainer" class="hidden">
            <h4 style="margin-top: 0">Detected Entities</h4>
            <div class="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Confidence</th>
                    <th>Text</th>
                  </tr>
                </thead>
                <tbody id="textReplacementsBody"></tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div id="filesTab" class="tab-content">
      <div class="card">
        <div id="dragArea" class="file-drop-area">
          Drag & drop files here or click to select
        </div>
        <input type="file" id="fileUpload" multiple class="hidden" />
        <div id="fileList" class="file-list hidden"></div>
        <div class="button-group">
          <button id="fileButton" class="btn btn-primary" disabled>
            Review Files
          </button>
          <button id="clearFilesButton" class="btn btn-secondary">Clear</button>
        </div>
      </div>

      <div id="fileResultsContainer" class="card hidden">
        <div
          style="
            display: flex;
            justify-content: space-between;
            align-items: center;
          "
        >
          <h3 style="margin: 0">Results</h3>
          <button id="fileCopyButton" class="copy-btn">Copy Text</button>
        </div>
        <div class="results">
          <div id="fileReviewMessage" class="review-text"></div>
          <div id="fileContentPreview" class="content-preview"></div>
          <div id="fileReplacementsContainer" class="hidden">
            <h4 style="margin-top: 0">Detected Entities</h4>
            <div class="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Confidence %</th>
                    <th>Text</th>
                  </tr>
                </thead>
                <tbody id="fileReplacementsBody"></tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>

    <script src="sidepanel.js"></script>
  </body>
</html>

```

# sidepanel.js

```js
let reviewEndpoint = "http://localhost:5001/api/review";
let filesEndpoint = "http://localhost:5001/api/files";
let defaultThreshold = "0.4";
let defaultReplacementType = "EntityType";
let defaultStartTag = "{{";
let defaultEndTag = "}}";
let defaultLanguage = "en";

// Load configuration
chrome.storage.sync.get(
  {
    reviewEndpoint: "http://localhost:5001/api/review",
    filesEndpoint: "http://localhost:5001/api/files",
    defaultThreshold: "0.4",
    defaultReplacementType: "EntityType",
    defaultStartTag: "{{",
    defaultEndTag: "}}",
    defaultLanguage: "en",
  },
  (items) => {
    reviewEndpoint = items.reviewEndpoint;
    filesEndpoint = items.filesEndpoint;
    defaultThreshold = items.defaultThreshold;
    defaultReplacementType = items.defaultReplacementType;
    defaultStartTag = items.defaultStartTag;
    defaultEndTag = items.defaultEndTag;
    defaultLanguage = items.defaultLanguage;

    // Initialize form controls with default values
    document.getElementById("threshold").value = defaultThreshold;
    document.getElementById("thresholdValue").textContent = `${(
      defaultThreshold * 100
    ).toFixed(0)}%`;
    document.getElementById("replacementType").value = defaultReplacementType;

    console.log("Configuration loaded:", items);
  }
);

document.addEventListener("DOMContentLoaded", function () {
  // Tab handling
  const tabButtons = document.querySelectorAll(".tab-button");
  const tabContents = document.querySelectorAll(".tab-content");

  tabButtons.forEach((button) => {
    button.addEventListener("click", () => {
      const tabName = button.dataset.tab;

      // Update active states
      tabButtons.forEach((btn) => btn.classList.remove("active"));
      tabContents.forEach((content) => content.classList.remove("active"));

      button.classList.add("active");
      document.getElementById(`${tabName}Tab`).classList.add("active");
    });
  });

  // Elements
  const dragArea = document.getElementById("dragArea");
  const fileInput = document.getElementById("fileUpload");
  const fileList = document.getElementById("fileList");
  const fileButton = document.getElementById("fileButton");
  const reviewButton = document.getElementById("reviewButton");
  const textCopyButton = document.getElementById("textCopyButton");
  const fileCopyButton = document.getElementById("fileCopyButton");
  const threshold = document.getElementById("threshold");
  const thresholdValue = document.getElementById("thresholdValue");
  const clearButton = document.getElementById("clearTextButton");
  const clearFilesButton = document.getElementById("clearFilesButton");

  let files = new Set();
  let textResults = null;
  let fileResults = null;

  // Update threshold value display
  threshold.addEventListener("input", (e) => {
    thresholdValue.textContent = `${(e.target.value * 100).toFixed(0)}%`;
  });

  // Drag and drop handlers
  dragArea.addEventListener("dragover", (e) => {
    e.preventDefault();
    dragArea.classList.add("dragover");
  });

  dragArea.addEventListener("dragleave", () => {
    dragArea.classList.remove("dragover");
  });

  dragArea.addEventListener("drop", (e) => {
    e.preventDefault();
    dragArea.classList.remove("dragover");
    handleFiles(e.dataTransfer.files);
  });

  dragArea.addEventListener("click", () => {
    fileInput.click();
  });

  fileInput.addEventListener("change", (e) => {
    handleFiles(e.target.files);
  });
  clearButton.addEventListener("click", clearText);
  clearFilesButton.addEventListener("click", clearFiles);

  function handleFiles(newFiles) {
    Array.from(newFiles).forEach((file) => {
      files.add(file);
    });
    updateFileList();
  }

  function updateFileList() {
    fileList.innerHTML = "";
    if (files.size > 0) {
      fileList.classList.remove("hidden");
      files.forEach((file) => {
        const fileItem = document.createElement("div");
        fileItem.className = "file-item";
        fileItem.innerHTML = `
                <span>${file.name}</span>
                <span class="remove-file" title="Remove file" data-name="${file.name}">×</span>
            `;
        fileList.appendChild(fileItem);
      });
      fileButton.disabled = false;
    } else {
      fileList.classList.add("hidden");
      fileButton.disabled = true;
    }
  }

  fileList.addEventListener("click", (e) => {
    if (e.target.classList.contains("remove-file")) {
      const fileName = e.target.dataset.name;
      files.forEach((file) => {
        if (file.name === fileName) {
          files.delete(file);
        }
      });
      updateFileList();
    }
  });

  reviewButton.addEventListener("click", submitTextReview);
  fileButton.addEventListener("click", submitFileReview);
  textCopyButton.addEventListener("click", () => copyResults("text"));
  fileCopyButton.addEventListener("click", () => copyResults("file"));

  function clearText() {
    // Clear text area
    document.getElementById("reviewText").value = "";

    // Reset values to defaults
    document.getElementById("threshold").value = defaultThreshold;
    document.getElementById("thresholdValue").textContent = `${(
      defaultThreshold * 100
    ).toFixed(0)}%`;
    document.getElementById("replacementType").value = defaultReplacementType;

    // Hide results
    document.getElementById("textResultsContainer").classList.add("hidden");

    // Clear results data
    textResults = null;
  }

  function clearFiles() {
    // Clear file list
    files.clear();
    updateFileList();

    // Hide results
    document.getElementById("fileResultsContainer").classList.add("hidden");

    // Clear results data
    fileResults = null;
  }

  async function submitTextReview() {
    const text = document.getElementById("reviewText").value;
    const thresholdValue = parseFloat(
      document.getElementById("threshold").value
    );
    const replacementType = document.getElementById("replacementType").value;

    if (!text) {
      showResults({ review: "Please enter some text to review" }, "text");
      return;
    }

    try {
      const response = await fetch(reviewEndpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          content: text,
          threshold: thresholdValue,
          replacementType: replacementType,
          startTag: defaultStartTag,
          endTag: defaultEndTag,
          language: defaultLanguage,
        }),
      });

      const data = await response.json();
      textResults = data;
      showResults(data, "text");
    } catch (error) {
      console.error("Error:", error);
      showResults({ review: "Error: Unable to process review" }, "text");
    }
  }

  async function submitFileReview() {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append("files", file);
    });

    try {
      console.log("Files end point:", filesEndpoint);
      const response = await fetch(filesEndpoint, {
        method: "POST",
        body: formData,
      });

      const data = await response.json();
      fileResults = data;
      // showResults(data, "file");
      if (data.reviews) {
        showFileResults(data);
      } else {
        showResults({ review: "Error: Invalid file review response" }, "file");
      }


      // Clear file list after successful upload
      files.clear();
      updateFileList();
    } catch (error) {
      console.error("Error:", error);
      showResults({ review: "Error: Unable to process files" }, "file");
    }
  }

  function showFileResults(data) {
    if (!data.reviews || !Array.isArray(data.reviews)) return;
  
    const container = document.getElementById('fileResultsContainer');
    const contentPreview = document.getElementById('fileContentPreview');
    container.classList.remove('hidden');
    
    contentPreview.innerHTML = '';
    contentPreview.className = 'file-results';
  
    data.reviews.forEach(review => {
      const fileCard = document.createElement('div');
      fileCard.className = 'file-card';
  
      // Header section
      const header = document.createElement('div');
      header.className = 'file-card-header';
      header.innerHTML = `
        <h3 class="file-card-title">${review.fileName}</h3>
        <p class="file-card-review">${review.review}</p>
      `;
  
      // Content section
      const content = document.createElement('div');
      content.className = 'file-card-content';
  
      review.analysis.pages.forEach(page => {
        const pageSection = document.createElement('div');
        pageSection.className = 'page-section';
        pageSection.innerHTML = `
          <h4 class="page-title">Page ${page.pageNumber}</h4>
          <div class="issues-container">
            ${page.issues.map(issue => `
              <span 
                class="issue-tag"
                title="Score: ${(issue.score * 100).toFixed()}%"
              >
                ${issue.value}
                <span class="issue-type">(${issue.type})</span>
              </span>
            `).join('')}
          </div>
        `;
        content.appendChild(pageSection);
      });
  
      fileCard.appendChild(header);
      fileCard.appendChild(content);
      contentPreview.appendChild(fileCard);
    });
  }


  function showResults(data, type) {
    if (type === "text" && !textResults) return;
    if (type === "file" && !fileResults) return;

    const container = document.getElementById(`${type}ResultsContainer`);
    const reviewMessage = document.getElementById(`${type}ReviewMessage`);
    const contentPreview = document.getElementById(`${type}ContentPreview`);
    const replacementsContainer = document.getElementById(
      `${type}ReplacementsContainer`
    );
    const replacementsBody = document.getElementById(`${type}ReplacementsBody`);
    const originalText = document.getElementById("reviewText").value;

    container.classList.remove("hidden");
    reviewMessage.textContent = data.review;

    if (data.text) {
      const startTagEscaped = defaultStartTag.replace(
        /[.*+?^${}()|[\]\\]/g,
        "\\$&"
      );
      const endTagEscaped = defaultEndTag.replace(
        /[.*+?^${}()|[\]\\]/g,
        "\\$&"
      );
      const formattedText = data.text
        .replace(/\n/g, "<br/>")
        .replace(new RegExp(startTagEscaped, "g"), '<span style="color: red">')
        .replace(new RegExp(endTagEscaped, "g"), "</span>");

      contentPreview.innerHTML = formattedText;
      contentPreview.classList.remove("hidden");
    } else {
      contentPreview.classList.add("hidden");
    }

    if (data.replacements && data.replacements.length > 0) {
      replacementsContainer.classList.remove("hidden");
      replacementsBody.innerHTML = data.replacements
        .map(
          (replacement) => `
                <tr>
                    <td>${replacement.entity_type}</td>
                    <td>${(replacement.score * 100).toFixed(0)}%</td>
                    <td>${originalText.substring(
                      replacement.start,
                      replacement.end
                    )}</td>
                </tr>
            `
        )
        .join("");
    } else {
      replacementsContainer.classList.add("hidden");
    }
  }

  async function copyResults(type) {
    const results = type === "text" ? textResults : fileResults;
    if (results?.text) {
      try {
        const formattedText = results.text;
        //   .replace(/\{\{/g, "") // Replace {{ with ''
        //   .replace(/\}\}/g, ""); // Replace }} with ''

        await navigator.clipboard.writeText(formattedText);
        const copyButton = document.getElementById(`${type}CopyButton`);
        const originalText = copyButton.textContent;
        copyButton.textContent = "Copied!";
        setTimeout(() => {
          copyButton.textContent = originalText;
        }, 2000);
      } catch (err) {
        console.error("Failed to copy:", err);
      }
    }
  }
});

```

