let isSubscribed = false;

export function subscribe(callback) {
    if (isSubscribed) {
        return;
    }

    isSubscribed = true;
    window.addEventListener("focus", () => callback("resumed"));
    window.addEventListener("blur", () => callback("inactive"));
    document.addEventListener("visibilitychange", () => {
        callback(document.visibilityState === "hidden" ? "hidden" : "resumed");
    });

    if (document.visibilityState === "hidden") {
        callback("hidden");
    } else {
        callback(document.hasFocus() ? "resumed" : "inactive");
    }
}
