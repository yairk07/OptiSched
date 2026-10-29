<%@ Page Title="" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="home.aspx.cs" Inherits="home" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <!-- ה־CSS הראשי כבר נטען מה-Master דרך StyleSheet.css -->
    <script src="JavaScript.js" defer></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <asp:Panel ID="pnlAbout" runat="server" CssClass="about-section home-shell">
        <asp:HiddenField ID="hfSelectedDate" runat="server" ClientIDMode="Static" />

        <section class="home-hero">
            <div class="hero-text">
                <span class="hero-eyebrow">מרכז הניהול שלך</span>
                <asp:Label ID="lblWelcome" runat="server" CssClass="welcome-label hero-title" />
                <p class="hero-description">
                    נטר פגישות קרובות, תכנן משימות חדשות והישאר מסודר עם לוח שנה אחיד לכל הצוות – הכל ממסך אחד שמייעל את הזרימה שלך בכל מכשיר.
                </p>
                <div class="hero-actions">
                    <a class="btn-pill primary" href="tasks.aspx">טופס משימות</a>
                    <a class="btn-pill ghost" href="allEvents.aspx">כל האירועים</a>
                    <a class="btn-pill ghost" href="exportGoogle.aspx">ייצוא ליומן Google</a>
                </div>
            </div>

            <div class="hero-summary">
                <div class="stat-chip">
                    <span class="chip-label">תאריך נבחר</span>
                    <asp:Label ID="lblSelectedDate" runat="server" CssClass="chip-value" Text="בחרו יום בלוח כדי להתחיל" />
                </div>
                <div class="stat-chip">
                    <span class="chip-label">זמן אמת</span>
                    <span class="chip-value muted">האירועים מתעדכנים אוטומטית לאחר בחירת תאריך</span>
                </div>
            </div>
        </section>

        <section class="home-grid">
            <article class="home-card calendar-card">
                <div class="calendar-board">
                    <div class="calendar-meta">
                        <div class="calendar-meta-date">
                            <asp:Label ID="lblMetaDay" runat="server" CssClass="calendar-day-number" Text="27" />
                            <div>
                                <asp:Label ID="lblMetaWeekday" runat="server" CssClass="calendar-weekday" Text="יום חמישי" />
                                <asp:Label ID="lblMetaFullDate" runat="server" CssClass="calendar-full-date" Text="27 אפריל 2025" />
                            </div>
                        </div>

                        <div class="calendar-hebrew-meta">
                            <div class="calendar-meta-line">
                                <span class="meta-label">תאריך עברי</span>
                                <asp:Label ID="lblHebrewDate" runat="server" CssClass="meta-value" />
                            </div>
                            <div class="calendar-meta-line">
                                <span class="meta-label">פרשת השבוע</span>
                                <asp:Label ID="lblParsha" runat="server" CssClass="meta-value" ClientIDMode="Static" />
                            </div>
                            <div class="calendar-meta-line">
                                <span class="meta-label">אירועים/חגים</span>
                                <asp:Label ID="lblHoliday" runat="server" CssClass="meta-value" />
                            </div>
                        </div>

                        <div class="calendar-zmanim">
                            <div class="calendar-meta-line">
                                <span class="meta-label">כניסת שבת</span>
                                <asp:Label ID="lblCandleLighting" runat="server" CssClass="meta-value highlight" ClientIDMode="Static" />
                            </div>
                            <div class="calendar-meta-line">
                                <span class="meta-label">צאת שבת</span>
                                <asp:Label ID="lblHavdalah" runat="server" CssClass="meta-value highlight" ClientIDMode="Static" />
                            </div>
                        </div>

                        <div class="calendar-events-pane">
                            <div class="calendar-events-header">
                                <span>אירועים נבחרים</span>
                                <a class="link-inline" href="allEvents.aspx">לרשימת האירועים</a>
                            </div>
                            <div class="calendar-events-scroll">
                                <div class="calendar-events">
                                    <asp:Literal ID="lblEvents" runat="server" />
                                </div>
                            </div>
                        </div>

                        <a class="calendar-cta" href="tasks.aspx">יצירת אירוע חדש</a>
                    </div>

                    <div class="calendar-surface">
                        <div class="calendar-surface-header">
                            <div>
                                <h3>לוח פעילות</h3>
                                <p class="card-subtitle">בחר תאריך כדי לצפות בכל מה שמתוכנן</p>
                            </div>
                            <div class="calendar-surface-controls">
                                <div class="calendar-year-block">
                                    <div class="calendar-year-controls">
                                        <asp:LinkButton ID="btnPrevYear" runat="server" CssClass="calendar-year-btn" OnClick="ChangeYear_Click" CommandArgument="-1">&#8249;</asp:LinkButton>
                                        <asp:Label ID="lblYear" runat="server" CssClass="calendar-year-label" />
                                        <asp:LinkButton ID="btnNextYear" runat="server" CssClass="calendar-year-btn" OnClick="ChangeYear_Click" CommandArgument="1">&#8250;</asp:LinkButton>
                                    </div>
                                    <div class="calendar-mode-toggle">
                                        <span class="meta-label">תצוגה</span>
                                        <asp:RadioButtonList ID="rblCalendarMode" runat="server"
                                            CssClass="calendar-mode-options"
                                            RepeatLayout="Flow"
                                            RepeatDirection="Horizontal"
                                            AutoPostBack="true"
                                            OnSelectedIndexChanged="rblCalendarMode_SelectedIndexChanged">
                                            <asp:ListItem Text="לועזי" Value="G" Selected="True" />
                                            <asp:ListItem Text="עברי" Value="H" />
                                        </asp:RadioButtonList>
                                    </div>
                                </div>
                                <div class="calendar-months-nav">
                                    <asp:Repeater ID="rptMonths" runat="server" OnItemCommand="Month_Command">
                                        <ItemTemplate>
                                            <asp:LinkButton runat="server"
                                                CommandName="ChangeMonth"
                                                CommandArgument='<%# Eval("Value") %>'
                                                CssClass='<%# Eval("CssClass") %>'
                                                Text='<%# Eval("Label") %>' />
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                            <a class="link-inline" href="editEvent.aspx">ניהול אירועים</a>
                        </div>
                        <div class="calendar-wrapper">
                            <asp:Calendar ID="calendar" runat="server"
                                  Width="100%" Height="300px"
                                  CssClass="calendar calendar-modern"
                                  ShowTitle="false"
                                  ShowNextPrevMonth="false"
                                  OnVisibleMonthChanged="calendar_VisibleMonthChanged"
                                  OnSelectionChanged="calendar_SelectionChanged"
                                  OnDayRender="calendar_DayRender"
                                  DayHeaderStyle-CssClass="calendar-dayheader"
                                  NextPrevStyle-CssClass="calendar-nav"
                                  TitleStyle-CssClass="calendar-title"
                                  DayStyle-CssClass="calendar-day"
                                  OtherMonthDayStyle-CssClass="calendar-day other-month"
                                  WeekendDayStyle-CssClass="calendar-day weekend"
                                  SelectedDayStyle-CssClass="calendar-day selected" />
                        </div>
                    </div>
                </div>
            </article>
        </section>

    </asp:Panel>
</asp:Content>
