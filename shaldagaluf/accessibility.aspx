<%@ Page Title="הצהרת נגישות" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="accessibility.aspx.cs" Inherits="accessibility" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="terms-shell a11y-statement" aria-labelledby="a11yStatementTitle">
        <div class="terms-container">
            <div class="terms-header">
                <h1 id="a11yStatementTitle" class="terms-title">הצהרת נגישות</h1>
                <p class="terms-date"><strong>תאריך עדכון אחרון:</strong> 15 בנובמבר 2026</p>
            </div>

            <div class="terms-content">
                <p class="terms-intro">
                    אתר OptiSched פועל להתאמת האתר לדרישות נגישות לפי
                    <strong>תקן ישראלי 5568</strong> ולתקנות שוויון זכויות לאנשים עם מוגבלות (התאמות נגישות לשירותי אינטרנט),
                    בהתאם לעקרונות <strong>WCAG 2.0 ברמת AA</strong>.
                    הצהרה זו מתארת בכנות את ההתאמות שבוצעו באתר — ואינה מהווה תעודת הסמכה או אישור חיצוני.
                </p>

                <div class="terms-section">
                    <h2 class="terms-section-title">1. מחויבות לנגישות</h2>
                    <p>
                        אנו שואפים לאפשר לכלל המשתמשים, לרבות אנשים עם מוגבלות, שימוש עצמאי, נוח ושוויוני באתר —
                        באמצעות מקלדת, תוכנות הקראה, הגדלת טקסט, ניגודיות גבוהה והתאמות נוספות.
                    </p>
                    <p>
                        הנגישות מיושמת באופן שוטף כחלק מפיתוח האתר. ייתכנו חלקים שעדיין בתהליך שיפור;
                        נשמח לקבל דיווחים כדי לתקן במהירות.
                    </p>
                </div>

                <div class="terms-section">
                    <h2 class="terms-section-title">2. התאמות שבוצעו באתר</h2>
                    <ul class="a11y-list">
                        <li>שפת האתר מוגדרת כעברית (<code>lang="he"</code>) וכיוון RTL.</li>
                        <li>קישור »דלג לתוכן הראשי« בראש כל עמוד.</li>
                        <li>מבנה סמנטי עם אזורי landmark (כותרת, ניווט, תוכן ראשי, כותרת תחתונה).</li>
                        <li>סרגל כלי נגישות בכל העמודים: הגדלת/הקטנת גופן, איפוס גודל, ניגודיות גבוהה, הדגשת קישורים בקו תחתון, והשהיית אנימציות.</li>
                        <li>העדפות נגישות נשמרות בדפדפן (localStorage) בין ביקורים.</li>
                        <li>סגנונות מיקוד גלויים (<code>:focus-visible</code>) לניווט במקלדת.</li>
                        <li>הודעות שגיאה בעמודי התחברות עם הודעה חיה לקוראי מסך (<code>role="alert"</code>).</li>
                        <li>הצהרת נגישות זו, עם קישור מהכותרת התחתונה ומסרגל הנגישות.</li>
                    </ul>
                </div>

                <div class="terms-section">
                    <h2 class="terms-section-title">3. כלי הנגישות</h2>
                    <p>
                        בפינת המסך מופיע כפתור »נגישות«. בלחיצה נפתח סרגל שמאפשר:
                    </p>
                    <ul class="a11y-list">
                        <li>הגדלת טקסט / הקטנת טקסט / איפוס לגודל רגיל</li>
                        <li>מצב ניגודיות גבוהה</li>
                        <li>הדגשת קישורים בקו תחתון</li>
                        <li>השהיית אנימציות ומעברים</li>
                    </ul>
                    <p>
                        ההגדרות נשמרות אוטומטית בדפדפן שלך. ניתן לאפס אותן דרך כפתור האיפוס בסרגל.
                    </p>
                </div>

                <div class="terms-section">
                    <h2 class="terms-section-title">4. מגבלות ידועות</h2>
                    <p>למרות המאמצים, ייתכנו מגבלות בחלקים מסוימים של האתר:</p>
                    <ul class="a11y-list">
                        <li>רכיבי לוח שנה מורכבים (בחירת תאריכים, תצוגות חודשיות) עשויים לדרוש שיפורי ניווט במקלדת נוספים.</li>
                        <li>תוכן או וידג'טים של צד שלישי (למשל וידג'ט מזג אוויר) אינם בשליטתנו המלאה.</li>
                        <li>מסמכים או קבצים שיועלו בעתיד עשויים שלא להיות נגישים במלואם עד להתאמתם.</li>
                        <li>דפים ישנים או מסכי ניהול פנימיים עשויים לכלול טפסים שטרם סומנו במלואם בתוויות.</li>
                    </ul>
                    <p>אנו ממשיכים לשפר את הנגישות בהתאם למשוב ולעדכוני התקן.</p>
                </div>

                <div class="terms-section">
                    <h2 class="terms-section-title">5. רכז/ת נגישות ודיווח על בעיות</h2>
                    <p>
                        לפניות בנושא נגישות האתר, דיווח על תקלות או בקשה להתאמה סבירה:
                    </p>
                    <ul class="a11y-list">
                        <li><strong>רכז/ת נגישות:</strong> צוות OptiSched</li>
                        <li><strong>דרך יצירת קשר:</strong> באמצעות עמוד
                            <a href="contactus.aspx">צור קשר</a>
                            — נא לציין בנושא או בתוכן ההודעה את המילה »נגישות«, ולתאר את העמוד, סוג הבעיה ואת הדפדפן/המכשיר.</li>
                        <li><strong>זמן מענה משוער:</strong> עד 5 ימי עסקים</li>
                    </ul>
                    <p>
                        אם לא קיבלת מענה מספק, ניתן לפנות גם לנציבות שוויון זכויות לאנשים עם מוגבלות,
                        בהתאם להוראות הדין.
                    </p>
                </div>

                <div class="terms-section">
                    <h2 class="terms-section-title">6. הבהרה משפטית</h2>
                    <p>
                        הצהרה זו מתארת את מצב הנגישות כפי שהוא במועד העדכון לעיל.
                        היא אינה מהווה אישור הסמכה של גוף חיצוני, ואינה מבטיחה תאימות מלאה בכל רכיב באתר בכל זמן נתון.
                        נמשיך לעדכן את ההצהרה וההתאמות בהתאם לשיפורים שיבוצעו.
                    </p>
                </div>

                <div class="terms-footer">
                    <p>
                        <a href="contactus.aspx">דיווח על בעיית נגישות</a>
                        &nbsp;·&nbsp;
                        <a href="termofservice.aspx">תנאי שירות</a>
                    </p>
                </div>
            </div>
        </div>
    </section>

    <style>
        .a11y-statement .a11y-list {
            margin: 0 0 16px;
            padding-inline-start: 1.4em;
            color: var(--text);
            font-size: 15px;
            line-height: 1.8;
        }

        .a11y-statement .a11y-list li {
            margin-bottom: 8px;
        }

        .a11y-statement .terms-content a {
            color: var(--brand);
            font-weight: 600;
            text-decoration: underline;
        }

        .a11y-statement .terms-title {
            font-size: 32px;
        }

        .a11y-statement h2.terms-section-title {
            text-align: right;
            margin-bottom: 16px;
        }

        .a11y-statement code {
            font-size: 0.92em;
            padding: 0 4px;
            border-radius: 4px;
            background: rgba(127, 127, 127, 0.15);
        }
    </style>
</asp:Content>
