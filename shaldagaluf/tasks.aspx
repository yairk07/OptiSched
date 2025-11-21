<%@ Page Title="Task Calendar" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="tasks.aspx.cs" Inherits="tasks" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <asp:Panel ID="pnlTasks" runat="server" CssClass="tasks-shell home-shell">
        <section class="tasks-hero">
            <div class="tasks-hero-text">
                <span class="hero-eyebrow">ניהול אירועים</span>
                <h2>תזמון זריז לכל המשימות</h2>
                <p>בחר תאריך, הוסף משימה וראה מיד את כל מה שמתוכנן. לוח השנה מעוצב בדיוק כמו בדף הבית כדי לשמור על חוויה אחידה.</p>
            </div>
            <div class="tasks-hero-meta">
                <div class="stat-chip">
                    <span class="chip-label">תאריך נבחר</span>
                    <asp:Label ID="lblSelectedDate" runat="server" CssClass="chip-value selected-date-label" />
                </div>
                <div class="stat-chip">
                    <span class="chip-label">הדרכה</span>
                    <span class="chip-value muted">כותרת היא שדה חובה, שאר הפרטים אופציונליים</span>
                </div>
            </div>
        </section>

        <section class="calendar-board tasks-board">
            <div class="calendar-meta tasks-meta">
                <div class="calendar-meta-line">
                    <span class="meta-label">כותרת</span>
                    <asp:TextBox ID="txtTitle" runat="server" CssClass="task-input" placeholder="למשל: תרגיל לילה" />
                </div>
                <div class="calendar-meta-line">
                    <span class="meta-label">שעה</span>
                    <asp:TextBox ID="txtTime" runat="server" CssClass="task-input" placeholder="לדוגמה 14:30" />
                </div>
                <div class="calendar-meta-line">
                    <span class="meta-label">הערות</span>
                    <asp:TextBox ID="txtNote" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="2" placeholder="פרטים נוספים" />
                </div>
                <asp:Button ID="btnAddEvent" runat="server" Text="שמור אירוע" CssClass="task-button" OnClick="AddEvent" />

                <div class="calendar-events-pane tasks-events-pane">
                    <div class="calendar-events-header">
                        <span>אירועים בתאריך הנבחר</span>
                    </div>
                    <div class="task-events-container">
                        <asp:Literal ID="lblEvents" runat="server" />
                    </div>
                </div>
            </div>

            <div class="calendar-surface tasks-surface">
                <div class="calendar-surface-header">
                    <div>
                        <h3>לוח פעילות</h3>
                        <p class="card-subtitle">בחר תאריך כדי לצפות ולהוסיף אירועים</p>
                    </div>
                </div>
                <div class="calendar-wrapper">
                    <asp:Calendar ID="calendar" runat="server"
                        CssClass="calendar calendar-modern"
                        ShowTitle="false"
                        ShowNextPrevMonth="false"
                        OnSelectionChanged="calendar_SelectionChanged"
                        OnDayRender="calendar_DayRender" />
                </div>
            </div>
        </section>
    </asp:Panel>
</asp:Content>
