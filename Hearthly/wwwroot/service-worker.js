importScripts('/js/idb-queue.js');

const OFFLINE_URL = '/Shared/Offline';
const CACHE_NAME = 'hearthly-cache-v1';
const ASSETS_TO_CACHE = [
    '/',
    '/css/bootstrap.min.css',
    '/css/site.css',
    '/js/site.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/favicon.ico',
    '/manifest.json',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    OFFLINE_URL
];

// Install event – cache static assets and offline fallback
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(ASSETS_TO_CACHE))
            .then(() => self.skipWaiting())
    );
});

// Activate event – claim control
self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// Fetch event – try cache first, fallback to network, then offline page
self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            return cachedResponse || fetch(event.request).catch(async () => {
                const cache = await caches.open(CACHE_NAME);
                return cache.match(OFFLINE_URL);
            });
        })
    );
});

// Background Sync: queued requests
self.addEventListener('sync', event => {
    if (event.tag === 'sync-queued-requests') {
        event.waitUntil(
            (async () => {
                const queued = await getAllQueuedRequests();
                for (const item of queued) {
                    try {
                        await fetch(item.url, {
                            method: item.method || 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(item.body)
                        });
                    } catch (err) {
                        console.error('Sync failed:', err);
                        return; // stop retrying this round
                    }
                }
                await clearQueue();
            })()
        );
    }
});

