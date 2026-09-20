// A versioned snapshot keeps changes to variants and selection atomic.
// No await inside an IDB transaction: it must remain active until the put.
function open() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('together-trips', 1);
        request.onupgradeneeded = event => {
            if (event.oldVersion === 0) request.result.createObjectStore('workspace');
        };
        request.onerror = () => reject(new Error('read'));
        request.onblocked = () => reject(new Error('blocked'));
        request.onsuccess = () => {
            const db = request.result;
            db.onversionchange = () => db.close();
            resolve(db);
        };
    });
}
export async function read() {
    const db = await open();
    return new Promise((resolve, reject) => {
        let value;
        const tx = db.transaction('workspace', 'readonly');
        const request = tx.objectStore('workspace').get('current');
        request.onsuccess = () => { value = request.result; };
        tx.oncomplete = () => { db.close(); typeof value === 'undefined' ? resolve(null) : typeof value === 'string' ? resolve(value) : reject(new Error('read')); };
        tx.onabort = tx.onerror = () => { db.close(); reject(new Error('read')); };
    });
}
export async function write(expectedRevision, expectedTripRevision, tripJson, comparisonOnly = false) {
    const trip = JSON.parse(tripJson);
    if (trip.revision !== expectedTripRevision + 1) return 'invalid';
    const db = await open();
    return new Promise(resolve => {
        let result = 'write';
        const tx = db.transaction('workspace', 'readwrite');
        const store = tx.objectStore('workspace');
        const request = store.get('current');
        request.onsuccess = () => {
            let current;
            try { current = request.result === undefined ? { revision: 0, schemaVersion: 1, trips: [] } : JSON.parse(request.result); }
            catch { result = 'read'; tx.abort(); return; }
            if (!current || current.schemaVersion !== 1 || !Number.isSafeInteger(current.revision) || !Array.isArray(current.trips)) { result = 'read'; tx.abort(); return; }
            if (current.revision !== expectedRevision) { result = 'conflict'; tx.abort(); return; }
            const index = current.trips.findIndex(t => t.id === trip.id);
            if ((index < 0 ? 0 : current.trips[index].revision) !== expectedTripRevision) { result = 'conflict'; tx.abort(); return; }
            if (comparisonOnly) {
                if (index < 0) { result = 'conflict'; tx.abort(); return; }
                current.trips[index] = { ...current.trips[index], selectedCriteria: trip.selectedCriteria, selectedVariantIds: trip.selectedVariantIds, revision: trip.revision, updatedAt: trip.updatedAt };
            } else if (index < 0) current.trips.push(trip); else current.trips[index] = trip;
            current.revision++;
            store.put(JSON.stringify(current), 'current');
        };
        tx.oncomplete = () => { db.close(); resolve('ok'); };
        tx.onabort = tx.onerror = () => { db.close(); resolve(result); };
    });
}
