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
        apiEndpoint: apiEndpoint,
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