(function () {
    "use strict";

    var STORAGE_KEY = "OptiSchedA11y";
    var FONT_STEPS = ["normal", "large", "xlarge"];

    function defaultPrefs() {
        return {
            font: "normal",
            contrast: false,
            underline: false,
            reduceMotion: false
        };
    }

    function loadPrefs() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return defaultPrefs();
            var parsed = JSON.parse(raw);
            var base = defaultPrefs();
            return {
                font: FONT_STEPS.indexOf(parsed.font) >= 0 ? parsed.font : base.font,
                contrast: !!parsed.contrast,
                underline: !!parsed.underline,
                reduceMotion: !!parsed.reduceMotion
            };
        } catch (e) {
            return defaultPrefs();
        }
    }

    function savePrefs(prefs) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
        } catch (e) { }
    }

    function applyPrefs(prefs) {
        var root = document.documentElement;

        if (prefs.font && prefs.font !== "normal") {
            root.setAttribute("data-a11y-font", prefs.font);
        } else {
            root.removeAttribute("data-a11y-font");
        }

        // גיבוי ל-zoom בדפדפנים שתומכים — כי רוב ה-CSS באתר ב-px
        try {
            if (prefs.font === "large") root.style.zoom = "1.12";
            else if (prefs.font === "xlarge") root.style.zoom = "1.25";
            else root.style.zoom = "";
        } catch (eZoom) { }

        if (prefs.contrast) {
            root.setAttribute("data-a11y-contrast", "high");
        } else {
            root.removeAttribute("data-a11y-contrast");
        }

        if (prefs.underline) {
            root.setAttribute("data-a11y-underline", "true");
        } else {
            root.removeAttribute("data-a11y-underline");
        }

        if (prefs.reduceMotion) {
            root.setAttribute("data-a11y-reduce-motion", "true");
        } else {
            root.removeAttribute("data-a11y-reduce-motion");
        }
    }

    function announce(msg) {
        var live = document.getElementById("a11yLive");
        if (!live) return;
        live.textContent = "";
        setTimeout(function () { live.textContent = msg; }, 20);
    }

    function syncButtons(prefs) {
        var contrastBtn = document.getElementById("a11yContrast");
        var underlineBtn = document.getElementById("a11yUnderline");
        var motionBtn = document.getElementById("a11yMotion");
        var fontUp = document.getElementById("a11yFontUp");
        var fontDown = document.getElementById("a11yFontDown");
        var fontReset = document.getElementById("a11yFontReset");

        if (contrastBtn) {
            contrastBtn.setAttribute("aria-pressed", prefs.contrast ? "true" : "false");
            contrastBtn.classList.toggle("is-active", prefs.contrast);
        }
        if (underlineBtn) {
            underlineBtn.setAttribute("aria-pressed", prefs.underline ? "true" : "false");
            underlineBtn.classList.toggle("is-active", prefs.underline);
        }
        if (motionBtn) {
            motionBtn.setAttribute("aria-pressed", prefs.reduceMotion ? "true" : "false");
            motionBtn.classList.toggle("is-active", prefs.reduceMotion);
        }
        if (fontUp) fontUp.classList.toggle("is-active", prefs.font === "xlarge");
        if (fontDown) fontDown.classList.toggle("is-active", false);
        if (fontReset) fontReset.classList.toggle("is-active", prefs.font === "normal");
        if (fontUp) fontUp.disabled = prefs.font === "xlarge";
        if (fontDown) fontDown.disabled = prefs.font === "normal";
    }

    function setPanelOpen(open) {
        var panel = document.getElementById("a11yPanel");
        var toggle = document.getElementById("a11yToggle");
        if (!panel || !toggle) return;
        if (open) {
            panel.hidden = false;
            panel.classList.add("is-open");
            panel.removeAttribute("hidden");
        } else {
            panel.hidden = true;
            panel.classList.remove("is-open");
            panel.setAttribute("hidden", "hidden");
        }
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
    }

    function isPanelOpen() {
        var panel = document.getElementById("a11yPanel");
        return !!(panel && !panel.hidden && panel.classList.contains("is-open"));
    }

    document.addEventListener("DOMContentLoaded", function () {
        var prefs = loadPrefs();
        applyPrefs(prefs);
        syncButtons(prefs);

        var toggle = document.getElementById("a11yToggle");
        var panel = document.getElementById("a11yPanel");
        if (panel) {
            panel.hidden = true;
            panel.classList.remove("is-open");
            panel.setAttribute("hidden", "hidden");
        }

        if (toggle && panel) {
            toggle.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                setPanelOpen(!isPanelOpen());
            });
        }

        var fontUp = document.getElementById("a11yFontUp");
        var fontDown = document.getElementById("a11yFontDown");
        var fontReset = document.getElementById("a11yFontReset");
        var contrastBtn = document.getElementById("a11yContrast");
        var underlineBtn = document.getElementById("a11yUnderline");
        var motionBtn = document.getElementById("a11yMotion");
        var resetAll = document.getElementById("a11yResetAll");

        if (fontUp) {
            fontUp.addEventListener("click", function (e) {
                e.preventDefault();
                var idx = FONT_STEPS.indexOf(prefs.font);
                if (idx < FONT_STEPS.length - 1) {
                    prefs.font = FONT_STEPS[idx + 1];
                    savePrefs(prefs);
                    applyPrefs(prefs);
                    syncButtons(prefs);
                    announce("גודל הטקסט הוגדל");
                }
            });
        }

        if (fontDown) {
            fontDown.addEventListener("click", function (e) {
                e.preventDefault();
                var idx = FONT_STEPS.indexOf(prefs.font);
                if (idx > 0) {
                    prefs.font = FONT_STEPS[idx - 1];
                    savePrefs(prefs);
                    applyPrefs(prefs);
                    syncButtons(prefs);
                    announce("גודל הטקסט הוקטן");
                }
            });
        }

        if (fontReset) {
            fontReset.addEventListener("click", function (e) {
                e.preventDefault();
                prefs.font = "normal";
                savePrefs(prefs);
                applyPrefs(prefs);
                syncButtons(prefs);
                announce("גודל הטקסט אופס");
            });
        }

        if (contrastBtn) {
            contrastBtn.addEventListener("click", function (e) {
                e.preventDefault();
                prefs.contrast = !prefs.contrast;
                savePrefs(prefs);
                applyPrefs(prefs);
                syncButtons(prefs);
                announce(prefs.contrast ? "ניגודיות גבוהה הופעלה" : "ניגודיות גבוהה כובתה");
            });
        }

        if (underlineBtn) {
            underlineBtn.addEventListener("click", function (e) {
                e.preventDefault();
                prefs.underline = !prefs.underline;
                savePrefs(prefs);
                applyPrefs(prefs);
                syncButtons(prefs);
                announce(prefs.underline ? "הדגשת קישורים הופעלה" : "הדגשת קישורים כובתה");
            });
        }

        if (motionBtn) {
            motionBtn.addEventListener("click", function (e) {
                e.preventDefault();
                prefs.reduceMotion = !prefs.reduceMotion;
                savePrefs(prefs);
                applyPrefs(prefs);
                syncButtons(prefs);
                announce(prefs.reduceMotion ? "אנימציות הושהו" : "אנימציות הופעלו מחדש");
            });
        }

        if (resetAll) {
            resetAll.addEventListener("click", function (e) {
                e.preventDefault();
                prefs = defaultPrefs();
                savePrefs(prefs);
                applyPrefs(prefs);
                syncButtons(prefs);
                announce("כל הגדרות הנגישות אופסו");
            });
        }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && isPanelOpen()) {
                setPanelOpen(false);
                if (toggle) toggle.focus();
            }
        });

        document.addEventListener("click", function (e) {
            if (!isPanelOpen()) return;
            var toolbar = document.getElementById("a11yToolbar");
            if (toolbar && !toolbar.contains(e.target)) {
                setPanelOpen(false);
            }
        });
    });
})();
