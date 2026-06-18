async function sendDataOrQueue(url, data) {
    if (navigator.onLine) {
        // Online – send immediately
        try {
            await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
        } catch (err) {
            console.error('Failed to send while online:', err);
            await queueRequest({ url, body: data });
            await registerSync();
        }
    } else {
        // Offline – queue it
        await queueRequest({ url, body: data });
        await registerSync();
    }
}

async function registerSync() {
    if ('serviceWorker' in navigator && 'SyncManager' in window) {
        const reg = await navigator.serviceWorker.ready;
        try {
            await reg.sync.register('sync-queued-requests');
        } catch (err) {
            console.error('Sync registration failed:', err);
        }
    }
}

let deferredPrompt;
const installBtn = document.getElementById('installBtn');

// Only show button if browser supports install prompt
window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault(); // prevent auto-prompt
    deferredPrompt = e;
    installBtn.classList.remove('d-none');
});

installBtn?.addEventListener('click', async () => {
    if (!deferredPrompt) return;

    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;

    if (outcome === 'accepted') {
        installBtn.classList.add('d-none');
        deferredPrompt = null;
    }
});

// Optional: Hide install button if already running in standalone
window.addEventListener('DOMContentLoaded', () => {
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches ||
        window.navigator.standalone === true;
    if (isStandalone && installBtn) {
        installBtn.classList.add('d-none');
    }
});
