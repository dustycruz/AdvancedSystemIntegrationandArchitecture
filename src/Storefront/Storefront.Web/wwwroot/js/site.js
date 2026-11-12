// Storefront behaviour: theme toggle, splash screen, toasts and button spinners.
(function () {
  "use strict";

  const storage = {
    get(key) { try { return localStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { localStorage.setItem(key, value); } catch { /* private mode */ } }
  };

  // Light / dark theme, remembered per browser. The initial theme is applied inline in <head>.
  function applyTheme(theme) {
    document.documentElement.setAttribute("data-bs-theme", theme);
    document.querySelectorAll("[data-theme-icon]").forEach(icon => {
      icon.className = theme === "dark" ? "bi bi-sun-fill" : "bi bi-moon-stars-fill";
    });
    document.querySelectorAll("[data-theme-toggle]").forEach(button => {
      button.setAttribute("aria-label", theme === "dark" ? "Switch to light theme" : "Switch to dark theme");
      button.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
    });
  }

  document.addEventListener("click", event => {
    const toggle = event.target.closest("[data-theme-toggle]");
    if (!toggle) return;
    const next = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
    storage.set("ac-theme", next);
    applyTheme(next);
  });

  // Splash screen: once per browser session.
  function initSplash() {
    const splash = document.getElementById("splash");
    if (!splash) return;
    let seen = false;
    try { seen = sessionStorage.getItem("ac-splash") === "1"; sessionStorage.setItem("ac-splash", "1"); } catch { seen = true; }
    if (seen || window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      splash.hidden = true;
      return;
    }
    splash.hidden = false;
    splash.addEventListener("animationend", e => { if (e.target === splash) splash.hidden = true; });
    setTimeout(() => { splash.hidden = true; }, 2500);
  }

  // Toasts rendered by the server (TempData) or created from script.
  window.acToast = function (message, kind) {
    const stack = document.getElementById("toast-stack");
    if (!stack || !window.bootstrap) return;
    const icons = { success: "bi-check-circle-fill", danger: "bi-exclamation-octagon-fill", info: "bi-info-circle-fill", warning: "bi-exclamation-triangle-fill" };
    const el = document.createElement("div");
    el.className = `toast align-items-center text-bg-${kind || "info"} border-0`;
    el.setAttribute("role", kind === "danger" ? "alert" : "status");
    el.setAttribute("aria-live", kind === "danger" ? "assertive" : "polite");
    el.setAttribute("aria-atomic", "true");
    el.innerHTML = `<div class="d-flex"><div class="toast-body"><i class="bi ${icons[kind] || icons.info} me-2" aria-hidden="true"></i></div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>`;
    el.querySelector(".toast-body").append(document.createTextNode(message));
    stack.append(el);
    const toast = new bootstrap.Toast(el, { delay: kind === "danger" ? 9000 : 5000 });
    el.addEventListener("hidden.bs.toast", () => el.remove());
    toast.show();
  };

  function initToasts() {
    document.querySelectorAll("[data-toast-message]").forEach(node => {
      window.acToast(node.getAttribute("data-toast-message"), node.getAttribute("data-toast-kind"));
      node.remove();
    });
  }

  // Every form button gets a spinner and is disabled while the request is in flight.
  document.addEventListener("submit", event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || event.defaultPrevented) return;
    if (form.hasAttribute("data-confirm") && !window.confirm(form.getAttribute("data-confirm"))) {
      event.preventDefault();
      return;
    }
    const button = event.submitter || form.querySelector("[type=submit]");
    if (button && button.classList.contains("btn")) {
      button.classList.add("is-loading");
      button.setAttribute("aria-busy", "true");
      setTimeout(() => { button.disabled = true; }, 0);
    }
  });

  document.addEventListener("DOMContentLoaded", () => {
    applyTheme(document.documentElement.getAttribute("data-bs-theme") || "light");
    initSplash();
    initToasts();
  });
})();
