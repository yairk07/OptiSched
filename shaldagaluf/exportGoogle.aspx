<%@ Page Title="ייצוא ליומן Google" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="exportGoogle.aspx.cs" Inherits="exportGoogle"
    ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" Culture="he-IL" UICulture="he-IL" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <meta charset="utf-8" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="export-page">
        <h2 class="page-title">ייצוא ליומן Google</h2>
        <p class="export-intro">
            כאן אפשר לייצא את האירועים האישיים שלך ליומן Google בשתי דרכים:
            קובץ/קוד ICS (מומלץ, עובד תמיד) או ייצוא ישיר דרך Google.
        </p>

        <asp:Label ID="lblStatus" runat="server" CssClass="export-status"></asp:Label>

        <div class="export-stats">
            <asp:Literal ID="litStats" runat="server" />
        </div>

        <section class="export-card">
            <h3>1) קוד ICS להדבקה / ייבוא ב-Google</h3>
            <p>
                העתק את הקוד למטה, שמור כקובץ עם סיומת <b>.ics</b>, ואז ב-Google Calendar:
                הגדרות &larr; ייבוא וייצוא &larr; ייבוא.
            </p>
            <asp:TextBox ID="txtIcs" runat="server" TextMode="MultiLine" Rows="14" CssClass="ics-box" ReadOnly="true"></asp:TextBox>
            <div class="export-actions">
                <asp:Button ID="btnRefreshIcs" runat="server" Text="רענון קוד" CssClass="export-btn secondary" OnClick="btnRefreshIcs_Click" />
                <asp:Button ID="btnDownloadIcs" runat="server" Text="הורדת קובץ ICS" CssClass="export-btn primary" OnClick="btnDownloadIcs_Click" />
            </div>
            <p class="hint">טיפ: אפשר גם לסמן את כל הטקסט בתיבה (Ctrl+A) ולהעתיק (Ctrl+C).</p>
        </section>

        <section class="export-card">
            <h3>2) ייצוא ישיר ליומן Google</h3>
            <p>
                דורש הרשאה חד-פעמית ליומן Google. אם מופיעה שגיאת הרשאה — לחץ שוב והתחבר מחדש.
            </p>
            <p class="hint">
                חשוב: לפני ייצוא ישיר יש להפעיל Google Calendar API בפרויקט ב-Cloud Console:<br />
                <a href="https://console.developers.google.com/apis/api/calendar-json.googleapis.com/overview?project=572433642016" target="_blank" rel="noopener">
                    הפעלת Google Calendar API
                </a>
                <br />אחרי ההפעלה חכה דקה-שתיים ונסה שוב. בינתיים אפשר להשתמש בהורדת ICS למעלה.
            </p>
            <div class="export-actions">
                <asp:Button ID="btnExportApi" runat="server" Text="ייצוא ישיר ל-Google Calendar" CssClass="export-btn google" OnClick="btnExportApi_Click" />
                <asp:Button ID="btnReconnect" runat="server" Text="חיבור מחדש ל-Google" CssClass="export-btn secondary" OnClick="btnReconnect_Click" />
            </div>
        </section>

        <section class="export-card">
            <h3>האירועים שייצאו</h3>
            <asp:Repeater ID="rptEvents" runat="server">
                <HeaderTemplate>
                    <ul class="event-list">
                </HeaderTemplate>
                <ItemTemplate>
                    <li>
                        <b><%# Server.HtmlEncode(Convert.ToString(Eval("Title"))) %></b>
                        &mdash;
                        <%# FormatDate(Eval("EventDate")) %>
                        <%# Server.HtmlEncode(Convert.ToString(Eval("EventTime"))) %>
                    </li>
                </ItemTemplate>
                <FooterTemplate>
                    </ul>
                </FooterTemplate>
            </asp:Repeater>
            <asp:Label ID="lblNoEvents" runat="server" Text="לא נמצאו אירועים אישיים לייצוא." Visible="false"></asp:Label>
        </section>

        <p><a href="allEvents.aspx" class="back-link">&larr; חזרה לכל האירועים</a></p>
    </div>

    <style>
        .export-page { width: min(900px, 94%); margin: 30px auto 50px; direction: rtl; }
        .export-intro { color: #555; line-height: 1.5; }
        .export-card {
            background: #fff;
            border: 1px solid #e5e7eb;
            border-radius: 12px;
            padding: 18px 20px;
            margin: 18px 0;
            box-shadow: 0 1px 2px rgba(0,0,0,.04);
        }
        .export-card h3 { margin-top: 0; }
        .ics-box {
            width: 100%;
            font-family: Consolas, monospace;
            font-size: 12px;
            direction: ltr;
            text-align: left;
            white-space: pre;
            border: 1px solid #d1d5db;
            border-radius: 8px;
            padding: 10px;
            box-sizing: border-box;
            background: #f8fafc;
        }
        .export-actions { margin-top: 12px; display: flex; gap: 10px; flex-wrap: wrap; }
        .export-btn {
            border: none;
            border-radius: 8px;
            padding: 10px 16px;
            font-weight: 600;
            cursor: pointer;
        }
        .export-btn.primary { background: #111827; color: #fff; }
        .export-btn.secondary { background: #e5e7eb; color: #111827; }
        .export-btn.google { background: #1a73e8; color: #fff; }
        .export-status {
            display: block;
            margin: 12px 0;
            padding: 10px 12px;
            border-radius: 8px;
        }
        .export-status.ok { background: #e6f4ea; color: #137333; }
        .export-status.err { background: #fce8e6; color: #c5221f; }
        .export-status.info { background: #e8f0fe; color: #174ea6; }
        .event-list { padding-right: 18px; }
        .hint { color: #6b7280; font-size: 13px; }
        .back-link { color: #1a73e8; text-decoration: none; }
        .export-stats { margin: 8px 0 4px; font-weight: 600; }
    </style>
</asp:Content>