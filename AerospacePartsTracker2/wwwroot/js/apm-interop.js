// wwwroot/js/apm-interop.js
// PDF export via browser print (no server-side DinkToPdf dependency needed for Blazor Server)
// For a true PDF with QuestPDF, call a Blazor endpoint instead and stream the bytes.

window.apmExportPdf = function () {
    // Add print-specific styles temporarily
    const style = document.createElement('style');
    style.id = 'apm-print-style';
    style.textContent = `
        @media print {
            body { background: white !important; color: black !important; }
            .apm-header-controls, .apm-history-btn { display: none !important; }
            .apm-card { break-inside: avoid; border: 1px solid #ccc !important; background: white !important; }
            .apm-result-label, .apm-result-value { color: black !important; }
            .apm-chart-wrap { break-inside: avoid; }
        }
    `;
    document.head.appendChild(style);
    window.print();
    document.head.removeChild(style);
};