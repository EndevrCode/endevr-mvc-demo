// idb-queue.js — loaded via importScripts in service-worker.js AND as a classic
// page script. Must NOT use ES module export/import syntax.

const _dbName    = 'NestledQueueDB';
const _storeName = 'requests';

function _openQueueDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(_dbName, 1);
        request.onupgradeneeded = event => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(_storeName)) {
                db.createObjectStore(_storeName, { autoIncrement: true });
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror  = () => reject(request.error);
    });
}

async function queueRequest(data) {
    const db = await _openQueueDB();
    return new Promise((resolve, reject) => {
        const tx    = db.transaction(_storeName, 'readwrite');
        const store = tx.objectStore(_storeName);
        const req   = store.add(data);
        req.onsuccess = () => resolve();
        req.onerror   = () => reject(req.error);
    });
}

async function getAllQueuedRequests() {
    const db = await _openQueueDB();
    return new Promise((resolve, reject) => {
        const tx    = db.transaction(_storeName, 'readonly');
        const store = tx.objectStore(_storeName);
        const req   = store.getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror   = () => reject(req.error);
    });
}

async function clearQueue() {
    const db = await _openQueueDB();
    return new Promise((resolve, reject) => {
        const tx    = db.transaction(_storeName, 'readwrite');
        const store = tx.objectStore(_storeName);
        const req   = store.clear();
        req.onsuccess = () => resolve();
        req.onerror   = () => reject(req.error);
    });
}
