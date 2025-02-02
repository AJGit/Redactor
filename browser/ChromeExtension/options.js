const endpoint = "http://localhost:5001";
let reviewEndpoint = endpoint + "/api/Review";
let filesEndpoint = endpoint + "/api/Files";
let verifyEndpoint = endpoint + "/api/Verify";
// Default values
const defaults = {
    apiEndpoint: verifyEndpoint,
    reviewEndpoint: reviewEndpoint,
    filesEndpoint: filesEndpoint,
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