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
    const zoneField = document.getElementById("hfShabbatZone");
    const zoneValue = zoneField && zoneField.value ? zoneField.value : "telaviv";
    fetchZmanimData(dateValue, zoneValue);
}

function fetchZmanimData(dateValue, zoneValue) {
    const params = new URLSearchParams({ date: dateValue, zone: zoneValue || "telaviv" });
    fetch(`zmanimProxy.ashx?${params.toString()}`, { method: "GET", credentials: "same-origin" })
        .then(response => {
            if (!response.ok) {
                throw new Error("Failed");
            }
            return response.json();
        })
        .then(data => applyZmanimData(data))
        .catch(() => { });
}

function applyZmanimData(data) {
    if (!data) {
        return;
    }
    if (data.hebrewDate) {
        setElementText("lblHebrewDate", data.hebrewDate);
    }
    if (data.parasha) {
        setElementText("lblParsha", data.parasha);
    }
    if (data.holiday) {
        setElementText("lblHoliday", data.holiday);
    }
    if (data.place) {
        setElementText("lblShabbatPlace", data.place);
    }
    const candleValue = data.candleLighting || formatTimeValue(data.kenisatShabbat22);
    const havdalahValue = data.havdalah || formatTimeValue(data.yetziatShabbat);
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
    const match = String(value).match(/T(\d{2}):(\d{2})/);
    if (match) {
        return match[1] + ":" + match[2];
    }
    const parsed = new Date(value);
    if (isNaN(parsed)) {
        return "";
    }
    return parsed.toLocaleTimeString("he-IL", { hour: "2-digit", minute: "2-digit", timeZone: "Asia/Jerusalem" });
}
