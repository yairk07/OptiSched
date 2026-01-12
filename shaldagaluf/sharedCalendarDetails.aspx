<%@ Page Title="טבלה משותפת" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="sharedCalendarDetails.aspx.cs" Inherits="sharedCalendarDetails" Debug="true" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <script src="tasks-text-parser.js"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="shared-calendar-details-shell">
        <div class="shared-calendar-details-container">
            <asp:Panel ID="pnlContent" runat="server">
                <div class="calendar-header">
                    <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="sharedCalendars.aspx" CssClass="back-link">
                        &laquo; חזרה לטבלאות משותפות
                    </asp:HyperLink>
                    <asp:Label ID="calendarTitle" runat="server" CssClass="calendar-title"></asp:Label>
                    <asp:Label ID="calendarDescription" runat="server" CssClass="calendar-description"></asp:Label>
                </div>

                <asp:Panel ID="pnlNotMember" runat="server" Visible="false" CssClass="not-member-panel">
                    <div class="join-section">
                        <h3>הצטרף לטבלה</h3>
                        <p>שלח בקשה להצטרפות לטבלה זו</p>
                        <div class="form-group">
                            <label class="form-label">הודעה (אופציונלי)</label>
                            <asp:TextBox ID="txtJoinMessage" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-input" placeholder="הודעה למנהל הטבלה"></asp:TextBox>
                        </div>
                        <asp:Button ID="btnSendJoinRequest" runat="server" Text="שלח בקשה להצטרפות" OnClick="btnSendJoinRequest_Click" CssClass="btn-join-request" />
                        <asp:Label ID="lblJoinMessage" runat="server" CssClass="form-message"></asp:Label>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlMember" runat="server" Visible="false">
                    <div class="calendar-tabs">
                        <asp:Button ID="btnTabEvents" runat="server" Text="אירועים" OnClick="btnTabEvents_Click" CssClass="tab-button active" />
                        <asp:Button ID="btnTabRequests" runat="server" Text="בקשות הצטרפות" OnClick="btnTabRequests_Click" CssClass="tab-button" Visible="false" />
                    </div>

                    <asp:Panel ID="pnlEvents" runat="server">
                        <section class="calendar-board tasks-board">
                            <div class="calendar-meta tasks-meta">
                                <div class="calendar-meta-line">
                                    <span class="meta-label">כותרת <span class="required">*</span></span>
                                    <asp:TextBox ID="txtEventTitle" runat="server" CssClass="task-input" placeholder="למשל: תרגיל לילה" />
                                </div>
                                <div class="calendar-meta-line">
                                    <span class="meta-label">תאריך <span class="required">*</span></span>
                                    <asp:TextBox ID="txtEventDate" runat="server" TextMode="Date" CssClass="task-input" />
                                </div>
                                <div class="calendar-meta-line">
                                    <span class="meta-label">שעה</span>
                                    <asp:TextBox ID="txtEventTime" runat="server" CssClass="task-input" placeholder="לדוגמה 14:30" />
                                </div>
                                <div class="calendar-meta-line">
                                    <span class="meta-label">קטגוריה</span>
                                    <asp:DropDownList ID="ddlEventCategory" runat="server" CssClass="task-input">
                                        <asp:ListItem Text="אירוע" Value="אירוע" Selected="True"></asp:ListItem>
                                        <asp:ListItem Text="יום הולדת" Value="יום הולדת"></asp:ListItem>
                                        <asp:ListItem Text="פגישה" Value="פגישה"></asp:ListItem>
                                        <asp:ListItem Text="מטלה" Value="מטלה"></asp:ListItem>
                                        <asp:ListItem Text="אחר" Value="אחר"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <div class="calendar-meta-line">
                                    <span class="meta-label">הערות</span>
                                    <asp:TextBox ID="txtEventNotes" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="2" placeholder="פרטים נוספים" />
                                </div>
                                <asp:Button ID="btnSaveEvent" runat="server" Text="שמור אירוע" CssClass="task-button" OnClick="btnSaveEvent_Click" />
                                <asp:Label ID="lblSaveError" runat="server" CssClass="form-error" Visible="false" />

                                <div class="calendar-meta-line" style="margin-top: 30px; padding-top: 30px; border-top: 2px solid rgba(255,255,255,0.1);">
                                    <span class="meta-label">הדבק טקסט להמרה לאירועים</span>
                                    <textarea id="txtPasteText" class="task-input" rows="8" placeholder="הדבק כאן טקסט בעברית עם תאריכים ושעות...&#10;&#10;לוגיקה: תאריכים (DD.MM), שעות (HH:MM-HH:MM), מיקומים (ב-, במושב), כותרת, תיאור. כל שורת תאריך = אירוע חדש.&#10;&#10;דוגמה:&#10;25.12&#10;כותרת האירוע&#10;מיקום&#10;19:00-21:00"></textarea>
                                    <button type="button" id="btnParseText" class="task-button" style="margin-top: 10px;">המר לאירועים</button>
                                </div>

                                <div id="parsedEventsContainer" style="display: none; margin-top: 20px;">
                                    <div class="calendar-meta-line">
                                        <span class="meta-label">אירועים שנוצרו:</span>
                                        <div id="parsedEventsList" style="max-height: 400px; overflow-y: auto; margin-top: 10px;"></div>
                                        <button type="button" id="btnSaveParsedEvents" class="task-button" style="margin-top: 15px;">שמור את כל האירועים</button>
                                        <button type="button" id="btnCancelParsedEvents" class="task-button" style="margin-top: 10px; background: #666;">ביטול</button>
                                    </div>
                                </div>

                            </div>

                            <div class="calendar-surface tasks-surface">
                                <div class="calendar-surface-header">
                                    <div>
                                        <h3>לוח פעילות</h3>
                                        <p class="card-subtitle">בחר תאריך כדי לצפות ולהוסיף אירועים</p>
                                    </div>
                                    <div class="calendar-nav">
                                        <asp:LinkButton ID="btnPrevMonth" runat="server" CssClass="nav-btn" OnClick="btnMonthChange_Click" CommandArgument="prev">&#8249; חודש קודם</asp:LinkButton>
                                        <asp:Label ID="lblCurrentMonth" runat="server" CssClass="month-label" />
                                        <asp:LinkButton ID="btnNextMonth" runat="server" CssClass="nav-btn" OnClick="btnMonthChange_Click" CommandArgument="next">חודש הבא &#8250;</asp:LinkButton>
                                    </div>
                                </div>
                                <div class="calendar-wrapper">
                                    <asp:Calendar ID="calEvents" runat="server"
                                        CssClass="calendar calendar-modern"
                                        ShowTitle="false"
                                        ShowNextPrevMonth="false"
                                        OnSelectionChanged="calEvents_SelectionChanged"
                                        OnDayRender="calEvents_DayRender"
                                        OnVisibleMonthChanged="calEvents_VisibleMonthChanged" />
                                </div>
                                
                                <div class="calendar-events-pane tasks-events-pane" style="margin-top: 24px;">
                                    <div class="calendar-events-header">
                                        <span>אירועים בתאריך הנבחר</span>
                                    </div>
                                    <div class="task-events-container">
                                        <asp:Literal ID="lblEvents" runat="server" />
                                    </div>
                                </div>
                            </div>
                        </section>
                    </asp:Panel>

                    <asp:Panel ID="pnlRequests" runat="server" Visible="false">
                        <div class="requests-section">
                            <h3 class="section-title">בקשות הצטרפות</h3>
                            <asp:Label ID="lblNoRequests" runat="server" Text="אין בקשות הצטרפות ממתינות" CssClass="no-requests-message" Visible="false" />
                            <asp:DataList ID="dlRequests" runat="server" CssClass="requests-list" OnItemDataBound="dlRequests_ItemDataBound">
                                <ItemTemplate>
                                    <div class="request-card">
                                        <div class="request-info">
                                            <strong><%# Eval("UserName") %> (<%# Eval("firstName") %> <%# Eval("lastName") %>)</strong>
                                            <p class="request-message"><%# Eval("Message") != null && !string.IsNullOrEmpty(Eval("Message").ToString()) ? Eval("Message") : "ללא הודעה" %></p>
                                            <small class="request-date">תאריך בקשה: <%# Convert.ToDateTime(Eval("RequestDate")).ToString("dd/MM/yyyy HH:mm") %></small>
                                        </div>
                                        <div class="request-actions">
                                            <div class="permission-selector">
                                                <label class="permission-label">הרשאה:</label>
                                                <asp:DropDownList ID="ddlPermission" runat="server" CssClass="permission-dropdown">
                                                    <asp:ListItem Text="ראיה בלבד" Value="Read" Selected="True"></asp:ListItem>
                                                    <asp:ListItem Text="עריכה + ראיה" Value="ReadWrite"></asp:ListItem>
                                                </asp:DropDownList>
                                            </div>
                                            <div class="request-buttons">
                                                <asp:Button ID="btnApprove" runat="server" Text="אשר" CommandArgument='<%# Eval("RequestId") %>' OnClick="btnApprove_Click" CssClass="btn-approve" />
                                                <asp:Button ID="btnReject" runat="server" Text="דחה" CommandArgument='<%# Eval("RequestId") %>' OnClick="btnReject_Click" CssClass="btn-reject" />
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:DataList>
                        </div>
                    </asp:Panel>
                </asp:Panel>
            </asp:Panel>

            <asp:Panel ID="pnlNotFound" runat="server" Visible="false" CssClass="not-found-panel">
                <h2>הטבלה לא נמצאה</h2>
                <asp:HyperLink ID="lnkBackNotFound" runat="server" NavigateUrl="sharedCalendars.aspx" CssClass="back-link">
                    &laquo; חזרה לטבלאות משותפות
                </asp:HyperLink>
            </asp:Panel>
        </div>
    </section>

    <style>
        .shared-calendar-details-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .shared-calendar-details-container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .back-link {
            display: inline-block;
            margin-bottom: 20px;
            color: var(--brand);
            text-decoration: none;
            font-weight: 600;
        }

        .back-link:hover {
            text-decoration: underline;
        }

        .calendar-header {
            margin-bottom: 30px;
        }

        .calendar-title {
            display: block;
            font-size: 28px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 8px;
        }

        .calendar-description {
            display: block;
            color: var(--text);
            opacity: 0.8;
            font-size: 16px;
            margin-bottom: 20px;
        }

        .not-member-panel {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .join-section {
            text-align: center;
        }

        .calendar-tabs {
            display: flex;
            gap: 12px;
            margin-bottom: 30px;
            border-bottom: 2px solid var(--border);
        }

        .tab-button {
            padding: 12px 24px;
            background: transparent;
            border: none;
            border-bottom: 3px solid transparent;
            color: var(--text);
            font-weight: 600;
            cursor: pointer;
            transition: all .2s ease;
        }

        .tab-button.active {
            color: var(--brand);
            border-bottom-color: var(--brand);
        }

        .add-event-panel {
            background: var(--surface);
            border-radius: 20px;
            padding: 30px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            margin-bottom: 30px;
        }

        .events-actions {
            margin-bottom: 20px;
        }

        .btn-add-event {
            padding: 12px 24px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }

        .calendar-nav {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 12px;
        }

        .nav-btn {
            padding: 8px 16px;
            background: var(--brand);
            color: #fff;
            border-radius: 6px;
            text-decoration: none;
            font-weight: 600;
            font-size: 14px;
            transition: background .2s ease;
        }

        .nav-btn:hover {
            background: var(--brand-dark);
            text-decoration: none;
        }

        .month-label {
            font-size: 18px;
            font-weight: 700;
            color: var(--heading);
        }

        .calendar .day-cell {
            position: relative;
        }

        .calendar .day-number {
            font-weight: 600;
            font-size: 16px;
            margin-bottom: 4px;
            color: var(--heading);
        }

        .calendar .day-events {
            display: flex;
            flex-direction: column;
            gap: 3px;
            margin-top: 4px;
        }

        .event-badge {
            font-size: 12px;
            padding: 4px 8px;
            border-radius: 4px;
            cursor: pointer;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            display: block;
            text-decoration: none;
            font-weight: 500;
            line-height: 1.3;
            min-height: 20px;
            background: var(--brand);
            color: #fff;
        }

        .event-badge:hover {
            opacity: 0.9;
            transform: scale(1.02);
            text-decoration: none;
        }

        .no-events-message {
            display: block;
            text-align: center;
            padding: 40px;
            color: var(--text);
            opacity: 0.6;
            font-size: 16px;
        }

        .requests-section {
            background: var(--surface);
            border-radius: 20px;
            padding: 30px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .section-title {
            font-size: 24px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 24px;
            padding-bottom: 12px;
            border-bottom: 2px solid var(--border);
        }

        .requests-list {
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        .request-card {
            background: var(--bg);
            border-radius: 12px;
            padding: 20px;
            box-shadow: var(--shadow-sm);
            border: 1px solid var(--border);
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: 20px;
            transition: transform .2s ease, box-shadow .2s ease;
        }

        .request-card:hover {
            transform: translateY(-2px);
            box-shadow: var(--shadow-md);
        }

        .request-info {
            flex: 1;
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .request-info strong {
            font-size: 16px;
            color: var(--heading);
            font-weight: 700;
        }

        .request-message {
            color: var(--text);
            opacity: 0.8;
            font-size: 14px;
            margin: 0;
            line-height: 1.5;
        }

        .request-date {
            color: var(--text);
            opacity: 0.6;
            font-size: 12px;
        }

        .request-actions {
            display: flex;
            flex-direction: column;
            gap: 12px;
            align-items: flex-end;
            min-width: 200px;
        }

        .permission-selector {
            display: flex;
            align-items: center;
            gap: 8px;
            width: 100%;
        }

        .permission-label {
            font-weight: 600;
            color: var(--text);
            font-size: 13px;
            white-space: nowrap;
        }

        .permission-dropdown {
            padding: 6px 12px;
            border: 1px solid var(--border);
            border-radius: 6px;
            background: var(--bg);
            color: var(--text);
            font-size: 14px;
            flex: 1;
        }

        .request-buttons {
            display: flex;
            gap: 8px;
            width: 100%;
        }

        .btn-approve, .btn-reject {
            padding: 10px 20px;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            font-size: 14px;
            transition: all .2s ease;
            flex: 1;
        }

        .btn-approve {
            background: var(--success);
            color: #fff;
        }

        .btn-approve:hover {
            background: #27ae60;
            transform: translateY(-1px);
        }

        .btn-reject {
            background: #ff6b6b;
            color: #fff;
        }

        .btn-reject:hover {
            background: #e55a5a;
            transform: translateY(-1px);
        }

        .no-requests-message {
            text-align: center;
            padding: 40px 20px;
            color: var(--text);
            opacity: 0.6;
            font-size: 16px;
            background: var(--bg);
            border-radius: 12px;
            border: 1px dashed var(--border);
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 20px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        .form-label {
            display: block;
            font-weight: 600;
            color: var(--heading);
            margin-bottom: 8px;
            font-size: 15px;
        }

        .required {
            color: var(--brand);
        }

        .form-input {
            width: 100%;
            padding: 12px 16px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--bg);
            color: var(--text);
            box-sizing: border-box;
        }

        .form-actions {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
        }

        .btn-save, .btn-cancel, .btn-join-request {
            padding: 12px 24px;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }

        .btn-save, .btn-join-request {
            background: var(--brand);
            color: #fff;
        }

        .btn-cancel {
            background: var(--surface);
            color: var(--text);
            border: 1px solid var(--border);
        }

        .form-message {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-top: 16px;
            text-align: center;
            font-weight: 600;
        }

        .form-error {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-top: 16px;
            text-align: center;
            font-weight: 600;
            color: #ff6b6b;
            background: rgba(255, 107, 107, 0.1);
        }

        .text-converter-section {
            background: var(--surface);
            border-radius: 12px;
            padding: 20px;
            border: 1px solid var(--border);
        }

        .text-converter-section h3 {
            font-size: 18px;
            font-weight: 600;
            margin-bottom: 15px;
        }

        #parsedEventsList {
            background: rgba(0,0,0,0.02);
            border-radius: 8px;
            padding: 15px;
        }

        .parsed-event-item {
            background: var(--bg);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 12px;
            margin-bottom: 10px;
        }

        .parsed-event-item strong {
            color: var(--brand);
            display: block;
            margin-bottom: 5px;
        }

        .parsed-event-item span {
            font-size: 13px;
            color: var(--text);
            opacity: 0.8;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .request-card {
                flex-direction: column;
                align-items: flex-start;
            }
        }
    </style>

    <script>
        (function() {
            const pasteTextarea = document.getElementById('txtPasteText');
            const parseBtn = document.getElementById('btnParseText');
            const parsedEventsContainer = document.getElementById('parsedEventsContainer');
            const parsedEventsList = document.getElementById('parsedEventsList');
            const saveBtn = document.getElementById('btnSaveParsedEvents');
            const cancelBtn = document.getElementById('btnCancelParsedEvents');
            let parsedEvents = [];

            if (parseBtn && pasteTextarea) {
                parseBtn.addEventListener('click', function() {
                    const text = pasteTextarea.value.trim();
                    if (!text) {
                        alert('אנא הדבק טקסט להמרה');
                        return;
                    }

                    if (typeof window.TextEventParser === 'undefined') {
                        alert('שגיאה: ספריית המרת הטקסט לא נטענה. אנא רענן את הדף.');
                        return;
                    }

                    parsedEvents = window.TextEventParser.parseText(text);
                    
                    if (parsedEvents.length === 0) {
                        alert('לא נמצאו אירועים בטקסט. אנא ודא שהטקסט מכיל תאריכים בפורמט: יום א 25.12');
                        return;
                    }

                    displayParsedEvents(parsedEvents);
                });
            }

            function displayParsedEvents(events) {
                if (!parsedEventsList || !parsedEventsContainer) return;
                
                parsedEventsList.innerHTML = '';
                
                events.forEach(function(event, index) {
                    const eventDiv = document.createElement('div');
                    eventDiv.className = 'parsed-event-item';
                    
                    let html = '<strong>' + (event.title || 'אירוע ללא כותרת') + '</strong>';
                    html += '<span>תאריך: ' + (event.date || 'לא צוין') + '</span>';
                    if (event.startTime || event.endTime) {
                        html += '<br><span>שעה: ';
                        if (event.startTime && event.endTime) {
                            html += event.startTime + ' - ' + event.endTime;
                        } else if (event.startTime) {
                            html += event.startTime;
                        }
                        html += '</span>';
                    }
                    if (event.location) {
                        html += '<br><span>מיקום: ' + event.location + '</span>';
                    }
                    if (event.description) {
                        html += '<br><span>תיאור: ' + event.description + '</span>';
                    }
                    
                    eventDiv.innerHTML = html;
                    parsedEventsList.appendChild(eventDiv);
                });
                
                parsedEventsContainer.style.display = 'block';
            }

            if (saveBtn) {
                saveBtn.addEventListener('click', function() {
                    if (parsedEvents.length === 0) {
                        alert('אין אירועים לשמירה');
                        return;
                    }

                    const form = document.createElement('form');
                    form.method = 'POST';
                    form.action = window.location.href;

                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = 'parsedEventsJson';
                    input.value = JSON.stringify(parsedEvents);
                    form.appendChild(input);

                    const viewState = document.querySelector('input[name="__VIEWSTATE"]');
                    if (viewState) {
                        const viewStateInput = document.createElement('input');
                        viewStateInput.type = 'hidden';
                        viewStateInput.name = '__VIEWSTATE';
                        viewStateInput.value = viewState.value;
                        form.appendChild(viewStateInput);
                    }

                    const eventValidation = document.querySelector('input[name="__EVENTVALIDATION"]');
                    if (eventValidation) {
                        const eventValidationInput = document.createElement('input');
                        eventValidationInput.type = 'hidden';
                        eventValidationInput.name = '__EVENTVALIDATION';
                        eventValidationInput.value = eventValidation.value;
                        form.appendChild(eventValidationInput);
                    }

                    document.body.appendChild(form);
                    form.submit();
                });
            }

            if (cancelBtn) {
                cancelBtn.addEventListener('click', function() {
                    if (parsedEventsContainer) {
                        parsedEventsContainer.style.display = 'none';
                    }
                    if (pasteTextarea) {
                        pasteTextarea.value = '';
                    }
                    parsedEvents = [];
                });
            }
        })();
    </script>
</asp:Content>
