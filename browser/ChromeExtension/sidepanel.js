const endpoint = "http://localhost:5001";
let reviewEndpoint = reviewEndpoint;
let filesEndpoint = filesEndpoint;
let defaultThreshold = "0.4";
let defaultReplacementType = "EntityType";
let defaultStartTag = "{{";
let defaultEndTag = "}}";
let defaultLanguage = "en";

// Load configuration
chrome.storage.sync.get(
    {
        reviewEndpoint: reviewEndpoint,
        filesEndpoint: filesEndpoint,
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

    // Add loading overlay variable
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
        loadingText.textContent = "Processing...";
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
        document.body.appendChild(loadingOverlay);

        return loadingOverlay;
    }

    // Function to show/hide loading overlay
    function toggleLoading(show) {
        const overlay = getLoadingOverlay();
        overlay.style.display = show ? "flex" : "none";
    }

    // Elements
    const dragArea = document.getElementById("dragArea");
    const fileInput = document.getElementById("fileUpload");
    const fileList = document.getElementById("fileList");
    const fileButton = document.getElementById("fileButton");
    const reviewButton = document.getElementById("reviewButton");
    const textCopyButton = document.getElementById("textCopyButton");
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
            toggleLoading(true); // Add this line
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
        } finally {
            toggleLoading(false); // Add this line
        }
    }

    function showFileResults(data) {
        if (!data.reviews || !Array.isArray(data.reviews)) return;

        const container = document.getElementById("fileResultsContainer");
        const contentPreview = document.getElementById("fileContentPreview");
        container.classList.remove("hidden");

        contentPreview.innerHTML = "";
        contentPreview.className = "file-results";

        data.reviews.forEach((review) => {
            const fileCard = document.createElement("div");
            fileCard.className = "file-card";

            // Header section
            const header = document.createElement("div");
            header.className = "file-card-header";
            header.innerHTML = `
        <h3 class="file-card-title">${review.fileName}</h3>
        <p class="file-card-review">${review.review}</p>
      `;

            // Content section
            const content = document.createElement("div");
            content.className = "file-card-content";

            review.analysis.pages.forEach((page) => {
                const pageSection = document.createElement("div");
                pageSection.className = "page-section";
                pageSection.innerHTML = `
          <h4 class="page-title">Page ${page.pageNumber}</h4>
          <div class="issues-container">
            ${page.issues
                        .map(
                            (issue) => `
              <span 
                class="issue-tag"
                title="Score: ${(issue.score * 100).toFixed()}%"
              >
                ${issue.value}
                <span class="issue-type">(${issue.type})</span>
              </span>
            `
                        )
                        .join("")}
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
