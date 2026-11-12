// Live saga timeline on the Order Details page: polls the order every 2 seconds until it is
// Confirmed or Cancelled and re-renders the steps. Compensation steps are shown in red.
(function () {
  "use strict";

  const root = document.querySelector("[data-order-timeline]");
  if (!root) return;

  const statusUrl = root.getAttribute("data-status-url");
  const list = root.querySelector(".timeline");
  const pill = document.querySelector("[data-order-status]");
  const banner = document.querySelector("[data-order-banner]");
  const live = document.querySelector("[data-order-live]");
  let lastCount = -1;

  const labels = {
    Pending: "Order placed",
    StockReserved: "Stock reserved",
    PaymentCharged: "Payment charged",
    Compensating: "Compensating",
    Confirmed: "Order confirmed",
    Cancelled: "Order cancelled"
  };
  const icons = {
    Pending: "bi-receipt", StockReserved: "bi-box-seam", PaymentCharged: "bi-credit-card",
    Compensating: "bi-arrow-counterclockwise", Confirmed: "bi-check2-circle", Cancelled: "bi-x-octagon"
  };

  function isCompensation(step) {
    return step.status === "Compensating" || step.status === "Cancelled";
  }

  function render(order) {
    if (order.history.length === lastCount && !order.isFinal) return;
    lastCount = order.history.length;

    list.replaceChildren(...order.history.map((step, index) => {
      const current = index === order.history.length - 1;
      const li = document.createElement("li");
      li.className = "timeline-step" + (isCompensation(step) ? " compensation" : " done") + (current && !order.isFinal ? " current" : "");
      const dot = document.createElement("span");
      dot.className = "timeline-dot";
      dot.innerHTML = `<i class="bi ${icons[step.status] || "bi-circle"}" aria-hidden="true"></i>`;
      const title = document.createElement("div");
      title.className = "timeline-title";
      title.textContent = labels[step.status] || step.status;
      const reason = document.createElement("div");
      reason.className = "small";
      reason.textContent = step.reason || "";
      const time = document.createElement("time");
      time.className = "timeline-time";
      time.dateTime = step.occurredAt;
      time.textContent = new Date(step.occurredAt).toLocaleString();
      li.append(dot, title, reason, time);
      return li;
    }));

    if (pill) {
      pill.className = `status-pill status-${order.status}`;
      pill.textContent = labels[order.status] || order.status;
    }
    if (live) {
      live.textContent = order.isFinal ? `Order ${order.status.toLowerCase()}.` : `Order is ${labels[order.status] || order.status}.`;
    }
    if (order.isFinal && banner) {
      banner.hidden = false;
      banner.className = "alert " + (order.status === "Confirmed" ? "alert-success" : "alert-danger");
      banner.textContent = order.status === "Confirmed"
        ? "Your order is confirmed. Thank you for shopping with us!"
        : `This order was cancelled. ${order.cancellationReason || ""}`;
      if (window.acToast) window.acToast(banner.textContent, order.status === "Confirmed" ? "success" : "danger");
    }
    const spinner = root.querySelector("[data-polling]");
    if (spinner) spinner.hidden = order.isFinal;
  }

  async function poll() {
    try {
      const response = await fetch(statusUrl, { headers: { "Accept": "application/json" }, cache: "no-store" });
      if (response.status === 401) { window.location.href = "/Account/Login?returnUrl=" + encodeURIComponent(location.pathname); return; }
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const order = await response.json();
      render(order);
      if (!order.isFinal) setTimeout(poll, 2000);
    } catch (error) {
      setTimeout(poll, 4000);
    }
  }

  if (root.getAttribute("data-final") !== "true") {
    setTimeout(poll, 2000);
  }
})();
