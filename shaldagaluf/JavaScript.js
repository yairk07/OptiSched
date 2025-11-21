document.addEventListener("DOMContentLoaded", function () {
    let images = ["pics/gun1.jpg", "pics/gun2.jpg", "pics/knife3.jpg", "pics/knife 4.jpg"];
    let currentIndex = 0;
    let imgElement = document.getElementById("galleryImage");

    function showImage(index) {
        if (imgElement) {
            imgElement.src = images[index];
        }
    }

    const prevButton = document.getElementById("prevBtn");
    if (prevButton) {
        prevButton.addEventListener("click", function (event) {
            event.preventDefault();
            currentIndex = (currentIndex - 1 + images.length) % images.length;
            showImage(currentIndex);
        });
    }

    const nextButton = document.getElementById("nextBtn");
    if (nextButton) {
        nextButton.addEventListener("click", function (event) {
            event.preventDefault();
            currentIndex = (currentIndex + 1) % images.length;
            showImage(currentIndex);
        });
    }

    showImage(currentIndex);
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

// 🎯 רולטה רוסית
function playRoulette() {
    const word1 = document.getElementById("word1").value.trim();
    const word2 = document.getElementById("word2").value.trim();
    const resultDiv = document.getElementById("rouletteResult");
    const highlightSection = document.getElementById("lossSection");
    const highlightLabel = document.getElementById("loserName");

    if (!word1 || !word2) {
        resultDiv.textContent = "אנא הזן שתי משימות.";
        highlightSection.style.display = "none";
        return;
    }

    const winningSlot = Math.floor(Math.random() * 3) + 1;
    const pick1 = Math.floor(Math.random() * 3) + 1;
    const pick2 = Math.floor(Math.random() * 3) + 1;

    const word1Selected = pick1 === winningSlot;
    const word2Selected = pick2 === winningSlot;

    let result = "";

    if (word1Selected && word2Selected) {
        result = "שתי האפשרויות נראות מצוינות – בחר את זו שהכי מתאימה למהלך היום.";
        highlightSection.style.display = "none";
    } else if (word1Selected) {
        result = `${word1} קיבלה עדיפות להמשך היום.`;
        highlightSection.style.display = "block";
        highlightLabel.textContent = `${word1} היא הבחירה`;
    } else if (word2Selected) {
        result = `${word2} קיבלה עדיפות להמשך היום.`;
        highlightSection.style.display = "block";
        highlightLabel.textContent = `${word2} היא הבחירה`;
    } else {
        result = "אין בחירה חד-משמעית, נסה שוב או החליט אינטואיטיבית.";
        highlightSection.style.display = "none";
    }

    resultDiv.textContent = result;
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
