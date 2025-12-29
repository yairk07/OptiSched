<%@ Page Title="תנאי שירות" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="termofservice.aspx.cs" Inherits="termsofservice" ResponseEncoding="utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="terms-shell">
        <div class="terms-container">
            <div class="terms-header">
                <h2 class="terms-title">תנאי שירות</h2>
                <p class="terms-date"><strong>תאריך תחילה:</strong> 27 במרץ 2025</p>
            </div>

            <div class="terms-content">
                <p class="terms-intro">ברוך הבא לשירות OptiSched. תנאי השירות הללו מהווים את ההסכם המשפטי בינך לבין החברה. בשימושך בשירות, אתה מסכים לתנאים ולמגבלות הבאות. אנו ממליצים לך לקרוא אותם בעיון לפני השימוש בשירות שלנו.</p>

                <div class="terms-section">
                    <h3 class="terms-section-title">1. שימוש כללי</h3>
                    <p>בעת השימוש בשירות OptiSched, אתה מסכים להיות אחראי באופן מלא לניהול הפגישות והאירועים שלך דרך השירות. כל השימוש בשירות הוא על אחריותך האישית.</p>
                    <p>אנו משתדלים להבטיח שהשירות יהיה זמין ונגיש בכל עת, אך אין אנו אחראים למקרים של אובדן נתונים, בעיות טכניות, או כל נזק אחר שיכול להיגרם כתוצאה מהשימוש בשירות.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">2. הגבלת אחריות</h3>
                    <p>לא נישא באחריות לכל נזק ישיר או עקיף שיכול להיגרם כתוצאה מהשימוש בשירות, כולל אך לא מוגבל לאי נוחות, אובדן זמן, או כל נזק אחר שיכול להיגרם בשל תקלות טכניות או בעיות הקשורות בשירות.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">3. פרטיות והגנה על מידע</h3>
                    <p>אנו מחויבים לשמור על פרטיות המידע שלך. כל המידע שייאסף במהלך השימוש בשירות ישמר בהתאם למדיניות הפרטיות שלנו, שתוכל למצוא בהמשך.</p>
                    <p>המידע שלך לא יימכר או יימסר לצדדים שלישיים, אלא אם כן דרישה כזו תבוא ממוסדות חוקיים, במקרה של חשד לפעילות לא חוקית.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">4. זכויות יוצרים וקניין רוחני</h3>
                    <p>כל התוכן המופיע בשירות OptiSched, כולל תוכנה, גרפיקה, ומסמכים אחרים, מוגן בזכויות יוצרים ובזכויות קניין רוחני אחרות. אין להעתיק, לשכפל, להפיץ או למכור את התוכן מבלי לקבל את הסכמתנו מראש.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">5. שינוי תנאים</h3>
                    <p>החברה שומרת לעצמה את הזכות לשנות את תנאי השירות מעת לעת. כל שינוי בתנאים יפורסם באתר ויכנס לתוקף מיד לאחר פרסומו.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">6. סיום השימוש בשירות</h3>
                    <p>החברה שומרת לעצמה את הזכות להפסיק את גישתך לשירות במקרה של הפרת תנאי השימוש או פעילות שיכולה לפגוע בשירות או במשתמשים אחרים.</p>
                    <p>אם תבחר להפסיק את השימוש בשירות, כל המידע שלך יימחק בהתאם למדיניות שלנו.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">7. מדיניות ביטול והחזרים</h3>
                    <p>בהתאם למדיניות החברה, לא יינתן החזר כספי בגין שירותים שנרכשו, אלא אם כן יש עילה לחיוב מחדש בהתאם לחוק.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">8. סמכות שיפוט</h3>
                    <p>תנאי שירות אלו כפופים לחוקי מדינת ישראל, וכל מחלוקת שתתעורר בעקבות השימוש בשירות תתברר בבתי המשפט המוסמכים בתל אביב-יפו.</p>
                </div>

                <div class="terms-section">
                    <h3 class="terms-section-title">9. תנאים נוספים</h3>
                    <p>אם מצאת כי אחד מהסעיפים בתנאי השירות אינו תקף או לא ניתן לאכיפה, הדבר לא ישפיע על תקפות יתר הסעיפים, אשר יישארו בתוקף מלא.</p>
                </div>

                <div class="terms-footer">
                    <p>כמתך לשימוש בשירות OptiSched, אתה מאשר כי קראת את תנאי השירות ומסכים להם במלואם.</p>
                </div>
            </div>
        </div>
    </section>

    <style>
        .terms-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .terms-container {
            max-width: 900px;
            margin: 0 auto;
        }

        .terms-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .terms-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .terms-date {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
            margin: 0;
        }

        .terms-content {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .terms-intro {
            font-size: 17px;
            line-height: 1.8;
            color: var(--text);
            margin-bottom: 32px;
            padding-bottom: 32px;
            border-bottom: 1px solid var(--border);
        }

        .terms-section {
            margin-bottom: 32px;
        }

        .terms-section:last-of-type {
            margin-bottom: 0;
        }

        .terms-section-title {
            font-size: 22px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 16px;
            padding-bottom: 8px;
            border-bottom: 2px solid var(--brand);
        }

        .terms-section p {
            font-size: 15px;
            line-height: 1.8;
            color: var(--text);
            margin-bottom: 16px;
        }

        .terms-section p:last-child {
            margin-bottom: 0;
        }

        .terms-footer {
            margin-top: 40px;
            padding-top: 32px;
            border-top: 1px solid var(--border);
            text-align: center;
        }

        .terms-footer p {
            font-size: 16px;
            font-weight: 600;
            color: var(--heading);
            margin: 0;
        }

        @media (max-width: 768px) {
            .terms-content {
                padding: 24px;
            }

            .terms-title {
                font-size: 28px;
            }

            .terms-section-title {
                font-size: 20px;
            }
        }
    </style>
</asp:Content>
