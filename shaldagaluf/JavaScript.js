document.addEventListener("DOMContentLoaded", function () {
    initializeCalendarData();
});

// ווידג'ט מזג אוויר
!function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (!d.getElementById(id)) {
        js = d.createElement(s);
        js.id = id;
        js.src = 'https://weatherwidget.io/js/widget.min.js';
        fjs.parentNode.insertBefore(js, fjs);
    }
}(document, 'script', 'weatherwidget-io-js');

// חזרה לראש הדף
window.onscroll = function () {
    var scrollToTopBtn = document.getElementById("scrollToTopBtn");
    if (scrollToTopBtn) {
        if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
            scrollToTopBtn.style.display = "block";
        } else {
            scrollToTopBtn.style.display = "none";
        }
    }
};

function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function navigateToURL(url) {
    window.location.href = url;
}

function initializeCalendarData() {
    const dateField = document.getElementById("hfSelectedDate");
    if (!dateField) {
        return;
    }
    const dateValue = dateField.value || new Date().toISOString().split("T")[0];
    fetchZmanimData(dateValue);
}

function fetchZmanimData(dateValue) {
    const params = new URLSearchParams({ date: dateValue });
    fetch(`zmanimProxy.ashx?${params.toString()}`, { method: "GET", credentials: "same-origin" })
        .then(response => {
            if (!response.ok) {
                throw new Error("Failed");
            }
            return response.json();
        })
        .then(data => applyZmanimData(data, dateValue))
        .catch(() => { });
}

function applyZmanimData(data, dateValue) {
    if (data && data.parasha) {
        setElementText("lblParsha", data.parasha);
    }
    const targetDate = new Date(dateValue);
    if (isNaN(targetDate)) {
        return;
    }
    const day = targetDate.getDay();
    const candleValue = day === 5 ? formatTimeValue(data ? data.kenisatShabbat22 : null) : "—";
    const havdalahValue = day === 6 ? formatTimeValue(data ? data.yetziatShabbat : null) : "—";
    setElementText("lblCandleLighting", candleValue || "—");
    setElementText("lblHavdalah", havdalahValue || "—");
}

function setElementText(id, value) {
    const element = document.getElementById(id);
    if (element) {
        element.textContent = value || "—";
    }
}

function formatTimeValue(value) {
    if (!value) {
        return "";
    }
    const parsed = new Date(value);
    if (isNaN(parsed)) {
        return "";
    }
    const adjusted = new Date(parsed.getTime() - 2 * 60 * 60 * 1000);
    return adjusted.toLocaleTimeString("he-IL", { hour: "2-digit", minute: "2-digit" });
}
