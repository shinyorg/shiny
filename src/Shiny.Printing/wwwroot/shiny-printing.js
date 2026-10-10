// Shiny.Printing.Blazor - drives the browser print dialog for PDF / image / HTML content.
// Each helper renders the content into a hidden same-document iframe and calls window.print() on it.

function printFrame(configure) {
    return new Promise((resolve) => {
        const iframe = document.createElement("iframe");
        iframe.style.position = "fixed";
        iframe.style.right = "0";
        iframe.style.bottom = "0";
        iframe.style.width = "0";
        iframe.style.height = "0";
        iframe.style.border = "0";

        let settled = false;
        const cleanup = () => {
            // Give the print dialog time to grab the frame before we remove it.
            setTimeout(() => iframe.remove(), 1000);
            if (!settled) {
                settled = true;
                resolve();
            }
        };

        iframe.onload = () => {
            try {
                const win = iframe.contentWindow;
                win.focus();
                win.print();
            } finally {
                cleanup();
            }
        };

        document.body.appendChild(iframe);
        configure(iframe);
    });
}

function dataUrl(base64, mime) {
    return `data:${mime};base64,${base64}`;
}

export function printPdf(base64) {
    return printFrame((iframe) => {
        iframe.src = dataUrl(base64, "application/pdf");
    });
}

export function printImage(base64, mime) {
    return printFrame((iframe) => {
        const doc = iframe.contentDocument;
        doc.open();
        doc.write(`<html><head><style>@page{margin:0}body{margin:0}img{width:100%}</style></head><body><img src="${dataUrl(base64, mime)}"></body></html>`);
        doc.close();
    });
}

export function printHtml(html) {
    return printFrame((iframe) => {
        const doc = iframe.contentDocument;
        doc.open();
        doc.write(html);
        doc.close();
    });
}

export function printUrl(url) {
    return printFrame((iframe) => {
        iframe.src = url;
    });
}
