(function () {
    function getAuthHeader() {
        const keys = ["authorized", "swagger_auth", "swagger_authorization"];

        for (const key of keys) {
            const raw = localStorage.getItem(key);
            if (!raw) continue;

            try {
                const data = JSON.parse(raw);
                const stack = [data];

                while (stack.length) {
                    const item = stack.pop();
                    if (!item) continue;

                    if (typeof item === "string") {
                        if (item.startsWith("Bearer ")) return item;
                        if (item.split(".").length === 3) return `Bearer ${item}`;
                        continue;
                    }

                    if (typeof item === "object") {
                        for (const value of Object.values(item)) {
                            stack.push(value);
                        }
                    }
                }
            } catch {
                // ignore invalid auth cache entries
            }
        }

        return null;
    }

    function getPath(opblock) {
        return opblock.querySelector(".opblock-summary-path")?.textContent?.trim() || "";
    }

    function getMethod(opblock) {
        return opblock.querySelector(".opblock-summary-method")?.textContent?.trim()?.toUpperCase() || "";
    }

    function setBody(opblock, payload) {
        const textarea = opblock.querySelector("textarea.body-param__text, .body-param textarea");
        if (!textarea) return;

        textarea.value = JSON.stringify(payload, null, 2);
        textarea.dispatchEvent(new Event("input", { bubbles: true }));
        textarea.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function getParam(opblock, name) {
        const row = opblock.querySelector(`tr[data-param-name=\"${name}\"]`);
        if (!row) return null;
        const input = row.querySelector("input, textarea, select");
        return input?.value?.trim() || null;
    }

    async function fetchJson(url) {
        const headers = {};
        const auth = getAuthHeader();
        if (auth) headers["Authorization"] = auth;

        const response = await fetch(url, { headers });
        if (!response.ok) return null;
        return await response.json();
    }

    async function prefill(opblock) {
        const method = getMethod(opblock);
        const path = getPath(opblock);

        if (method !== "PUT") return;

        if (path === "/api/users/me") {
            const me = await fetchJson("/api/users/me");
            if (!me) return;
            setBody(opblock, {
                name: me.name ?? "",
                phone: me.phone ?? ""
            });
            return;
        }

        if (path === "/api/users/{id}/role") {
            const id = getParam(opblock, "id");
            if (!id) return;
            const user = await fetchJson(`/api/users/${encodeURIComponent(id)}`);
            if (!user) return;
            setBody(opblock, { roleName: user.role ?? "Customer" });
            return;
        }

        if (path === "/api/users/by-email/role") {
            const email = getParam(opblock, "email");
            if (!email) return;
            const user = await fetchJson(`/api/users/by-email?email=${encodeURIComponent(email)}`);
            if (!user) return;
            setBody(opblock, { roleName: user.role ?? "Customer" });
        }
    }

    document.addEventListener("click", function (e) {
        const btn = e.target.closest(".try-out__btn");
        if (!btn) return;

        const opblock = e.target.closest(".opblock");
        if (!opblock) return;

        setTimeout(() => {
            prefill(opblock).catch(() => { });
        }, 150);
    });
})();
