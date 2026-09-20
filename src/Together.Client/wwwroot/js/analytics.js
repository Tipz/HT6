window.togetherAnalytics = (() => {
    const consentKey = "together.analytics.consent";
    let counterId = null;
    let enabled = false;
    let loading = null;

    function getConsent() {
        return window.localStorage.getItem(consentKey);
    }

    function setConsent(value) {
        if (value !== "granted" && value !== "denied") {
            throw new Error("Unsupported analytics consent value.");
        }
        window.localStorage.setItem(consentKey, value);
    }

    function loadTag() {
        if (loading) return loading;
        window.ym = window.ym || function () {
            (window.ym.a = window.ym.a || []).push(arguments);
        };
        window.ym.l = Date.now();
        loading = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.async = true;
            script.src = "https://mc.yandex.ru/metrika/tag.js";
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
        return loading;
    }

    async function enable(id) {
        if (!Number.isSafeInteger(id) || id <= 0 || getConsent() !== "granted") return;
        counterId = id;
        enabled = true;
        await loadTag();
        if (!enabled || typeof window.ym !== "function") return;
        window.ym(counterId, "init", {
            clickmap: false,
            trackLinks: false,
            accurateTrackBounce: false,
            webvisor: false,
            defer: true
        });
    }

    function disable() {
        enabled = false;
    }

    function hit(path) {
        if (enabled && typeof window.ym === "function" && /^\/(?!\/)/.test(path)) {
            window.ym(counterId, "hit", path, { title: "Вместе в путь", referer: "" });
        }
    }

    function goal(name, value) {
        if (!enabled || typeof window.ym !== "function") return;
        if (name === "trip_created" && value == null) {
            window.ym(counterId, "reachGoal", name);
        } else if (name === "login_succeeded" && (value === "password" || value === "yandex")) {
            window.ym(counterId, "reachGoal", name, { method: value });
        }
    }

    return { getConsent, setConsent, enable, disable, hit, goal };
})();
