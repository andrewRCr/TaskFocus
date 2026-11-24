// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

// Use a fresh manifest URL when upgrading registrations created before updateViaCache was set.
self.importScripts('./service-worker-assets.js?update=1');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.m?js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrls = new Set(self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href));

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    if (event.request.method === 'GET' && url.origin === baseUrl.origin) {
        event.respondWith(onFetch(event.request, url));
    }
});

async function onInstall(event) {
    console.info('Service worker: Install');

    // Install a complete, integrity-checked snapshot before the browser can activate it.
    // Let existing tabs finish with their original cache before activating this worker.
    const assets = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)));
    const requests = cache => assets.map(asset => new Request(new URL(asset.url, baseUrl), {
        integrity: asset.hash, cache
    }));
    const cache = await caches.open(cacheName);
    try {
        await cache.addAll(requests('force-cache'));
    } catch {
        await cache.addAll(requests('reload'));
    }
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(request, url) {
    // Keep static assets tied to the active release even when requested with query parameters.
    url.search = '';
    // Deep links use the cached app shell. Manifest URLs such as static HTML use their own entries.
    const cacheUrl = request.mode === 'navigate' && !manifestUrls.has(url.href)
        ? new URL('index.html', baseUrl) : url;
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(cacheUrl);

    return cachedResponse || fetch(request);
}
