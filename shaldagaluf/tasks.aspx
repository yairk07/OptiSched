<%@ Page Title="Task Calendar" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="tasks.aspx.cs" Inherits="tasks" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

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
                    <span class="meta-label">קטגוריה</span>
                    <asp:DropDownList ID="ddlCategory" runat="server" CssClass="task-input">
                        <asp:ListItem Text="אירוע" Value="אירוע" Selected="True"></asp:ListItem>
                        <asp:ListItem Text="יום הולדת" Value="יום הולדת"></asp:ListItem>
                        <asp:ListItem Text="פגישה" Value="פגישה"></asp:ListItem>
                        <asp:ListItem Text="מטלה" Value="מטלה"></asp:ListItem>
                        <asp:ListItem Text="אחר" Value="אחר"></asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="calendar-meta-line">
                    <span class="meta-label">הערות</span>
                    <asp:TextBox ID="txtNote" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="2" placeholder="פרטים נוספים" />
                </div>
                <asp:Button ID="btnAddEvent" runat="server" Text="שמור אירוע" CssClass="task-button" OnClick="AddEvent" />

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

    <script src="tasks-text-parser.js"></script>
    <script>
        (function() {
            const pasteTextarea = document.getElementById('txtPasteText');
            const parseBtn = document.getElementById('btnParseText');
            const parsedEventsContainer = document.getElementById('parsedEventsContainer');
            const parsedEventsList = document.getElementById('parsedEventsList');
            const saveBtn = document.getElementById('btnSaveParsedEvents');
            const cancelBtn = document.getElementById('btnCancelParsedEvents');
            let parsedEvents = [];

            if (parseBtn) {
                parseBtn.addEventListener('click', function() {
                    const text = pasteTextarea.value.trim();
                    if (!text) {
                        alert('אנא הדבק טקסט להמרה');
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
                parsedEventsList.innerHTML = '';
                
                events.forEach((event, index) => {
                    const eventDiv = document.createElement('div');
                    eventDiv.className = 'parsed-event-item';
                    eventDiv.id = `parsed-event-${index}`;
                    eventDiv.style.cssText = 'background: rgba(255,255,255,0.05); padding: 15px; margin-bottom: 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.1); position: relative;';
                    
                    const dateStr = new Date(event.date + 'T00:00:00').toLocaleDateString('he-IL');
                    const timeStr = event.startTime && event.endTime ? `${event.startTime} - ${event.endTime}` : (event.startTime || '');
                    
                    let content = `
                        <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 10px;">
                            <div style="flex: 1;">
                                <div style="font-weight: bold; color: #3DC5F0; margin-bottom: 8px;">📅 ${dateStr}`;
                    if (timeStr) {
                        content += ` ⏰ ${timeStr}`;
                    }
                    content += `</div>
                                <div style="font-weight: 600; margin-bottom: 8px; font-size: 1.1em;">${event.title || '(ללא כותרת)'}</div>`;
                    
                    if (event.location) {
                        content += `<div style="color: #ffd700; margin-bottom: 5px;">📍 ${event.location}</div>`;
                    }
                    
                    if (event.description) {
                        content += `<div style="color: #b5bbc7; font-size: 0.9em; margin-top: 5px;">${event.description.replace(/\n/g, '<br>')}</div>`;
                    }
                    
                    content += `</div>
                            <div style="display: flex; gap: 5px;">
                                <button type="button" class="edit-event-btn" data-index="${index}" style="background: #3DC5F0; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer; font-size: 12px;">ערוך</button>
                                <button type="button" class="delete-event-btn" data-index="${index}" style="background: #F24E47; color: white; border: none; padding: 5px 10px; border-radius: 4px; cursor: pointer; font-size: 12px;">מחק</button>
                            </div>
                        </div>
                    `;
                    
                    eventDiv.innerHTML = content;
                    parsedEventsList.appendChild(eventDiv);
                });

                attachEventHandlers();
                parsedEventsContainer.style.display = 'block';
            }

            function attachEventHandlers() {
                const editButtons = parsedEventsList.querySelectorAll('.edit-event-btn');
                const deleteButtons = parsedEventsList.querySelectorAll('.delete-event-btn');

                editButtons.forEach(btn => {
                    btn.addEventListener('click', function() {
                        const index = parseInt(this.getAttribute('data-index'));
                        editEvent(index);
                    });
                });

                deleteButtons.forEach(btn => {
                    btn.addEventListener('click', function() {
                        const index = parseInt(this.getAttribute('data-index'));
                        deleteEvent(index);
                    });
                });
            }

            function editEvent(index) {
                if (index < 0 || index >= parsedEvents.length) return;

                const event = parsedEvents[index];
                const eventDiv = document.getElementById(`parsed-event-${index}`);
                if (!eventDiv) return;

                const dateStr = event.date;
                const [year, month, day] = dateStr.split('-');
                const formattedDate = `${day}.${month}.${year}`;
                const timeStr = event.startTime && event.endTime ? `${event.startTime}-${event.endTime}` : (event.startTime || '');

                const editForm = `
                    <div style="background: rgba(0,0,0,0.3); padding: 15px; border-radius: 8px; border: 2px solid #3DC5F0;">
                        <div style="margin-bottom: 10px;">
                            <label style="display: block; margin-bottom: 5px; font-weight: 600;">תאריך (DD.MM.YYYY):</label>
                            <input type="text" id="edit-date-${index}" value="${formattedDate}" class="casino-input" style="width: 100%; padding: 8px;">
                        </div>
                        <div style="margin-bottom: 10px;">
                            <label style="display: block; margin-bottom: 5px; font-weight: 600;">שעה (HH:MM-HH:MM או HH:MM):</label>
                            <input type="text" id="edit-time-${index}" value="${timeStr}" class="casino-input" style="width: 100%; padding: 8px;">
                        </div>
                        <div style="margin-bottom: 10px;">
                            <label style="display: block; margin-bottom: 5px; font-weight: 600;">כותרת:</label>
                            <input type="text" id="edit-title-${index}" value="${event.title || ''}" class="casino-input" style="width: 100%; padding: 8px;">
                        </div>
                        <div style="margin-bottom: 10px;">
                            <label style="display: block; margin-bottom: 5px; font-weight: 600;">מיקום:</label>
                            <input type="text" id="edit-location-${index}" value="${event.location || ''}" class="casino-input" style="width: 100%; padding: 8px;">
                        </div>
                        <div style="margin-bottom: 10px;">
                            <label style="display: block; margin-bottom: 5px; font-weight: 600;">תיאור:</label>
                            <textarea id="edit-description-${index}" class="casino-input" style="width: 100%; padding: 8px; min-height: 60px;">${event.description || ''}</textarea>
                        </div>
                        <div style="display: flex; gap: 10px;">
                            <button type="button" class="save-edit-btn" data-index="${index}" style="background: #34AE6D; color: white; border: none; padding: 8px 15px; border-radius: 4px; cursor: pointer;">שמור</button>
                            <button type="button" class="cancel-edit-btn" data-index="${index}" style="background: #666; color: white; border: none; padding: 8px 15px; border-radius: 4px; cursor: pointer;">ביטול</button>
                        </div>
                    </div>
                `;

                eventDiv.innerHTML = editForm;

                const saveBtn = eventDiv.querySelector('.save-edit-btn');
                const cancelBtn = eventDiv.querySelector('.cancel-edit-btn');

                if (saveBtn) {
                    saveBtn.addEventListener('click', function() {
                        saveEditedEvent(index);
                    });
                }

                if (cancelBtn) {
                    cancelBtn.addEventListener('click', function() {
                        displayParsedEvents(parsedEvents);
                    });
                }
            }

            function saveEditedEvent(index) {
                if (index < 0 || index >= parsedEvents.length) return;

                const dateInput = document.getElementById(`edit-date-${index}`);
                const timeInput = document.getElementById(`edit-time-${index}`);
                const titleInput = document.getElementById(`edit-title-${index}`);
                const locationInput = document.getElementById(`edit-location-${index}`);
                const descriptionInput = document.getElementById(`edit-description-${index}`);

                if (!dateInput || !titleInput) return;

                const dateStr = dateInput.value.trim();
                const timeStr = timeInput.value.trim();
                const title = titleInput.value.trim();
                const location = locationInput.value.trim();
                const description = descriptionInput.value.trim();

                if (!title) {
                    alert('כותרת היא שדה חובה');
                    return;
                }

                const dateMatch = dateStr.match(/(\d{1,2})[\.\/\-](\d{1,2})(?:[\.\/\-](\d{2,4}))?/);
                if (!dateMatch) {
                    alert('תאריך לא תקין. השתמש בפורמט DD.MM.YYYY');
                    return;
                }

                let day = parseInt(dateMatch[1], 10);
                let month = parseInt(dateMatch[2], 10);
                let year = dateMatch[3] ? parseInt(dateMatch[3], 10) : new Date().getFullYear();

                if (year < 100) {
                    year = 2000 + year;
                }

                if (day > 31 || month > 12) {
                    const temp = day;
                    day = month;
                    month = temp;
                }

                const monthStr = String(month).padStart(2, '0');
                const dayStr = String(day).padStart(2, '0');
                const formattedDate = `${year}-${monthStr}-${dayStr}`;

                let startTime = '';
                let endTime = '';

                if (timeStr) {
                    const timeRange = window.TextEventParser.parseTimeRange(timeStr);
                    if (timeRange) {
                        startTime = timeRange.start;
                        endTime = timeRange.end;
                    } else if (/^\d{1,2}[\.:]?\d{0,2}$/.test(timeStr.replace(/\s/g, ''))) {
                        const timeMatch = timeStr.match(/(\d{1,2})[\.:]?(\d{0,2})/);
                        if (timeMatch) {
                            const hour = parseInt(timeMatch[1], 10);
                            const min = timeMatch[2] ? parseInt(timeMatch[2], 10) : 0;
                            startTime = `${String(hour).padStart(2, '0')}:${String(min).padStart(2, '0')}`;
                        }
                    }
                }

                parsedEvents[index] = {
                    date: formattedDate,
                    startTime: startTime,
                    endTime: endTime,
                    title: title,
                    location: location,
                    description: description
                };

                displayParsedEvents(parsedEvents);
            }

            function deleteEvent(index) {
                if (index < 0 || index >= parsedEvents.length) return;

                if (confirm('האם אתה בטוח שברצונך למחוק את האירוע הזה?')) {
                    parsedEvents.splice(index, 1);
                    displayParsedEvents(parsedEvents);
                }
            }

            if (saveBtn && !saveBtn.hasAttribute('data-listener-attached')) {
                saveBtn.setAttribute('data-listener-attached', 'true');
                saveBtn.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();

                    if (parsedEvents.length === 0) {
                        alert('אין אירועים לשמירה');
                        return false;
                    }

                    if (saveBtn.disabled) {
                        return false;
                    }

                    saveBtn.disabled = true;
                    saveBtn.textContent = 'שומר...';

                    try {
                        const eventsJson = JSON.stringify(parsedEvents);
                        const form = document.querySelector('form[method="post"]') || document.forms[0];
                        
                        if (!form) {
                            alert('שגיאה: לא נמצא טופס');
                            saveBtn.disabled = false;
                            saveBtn.textContent = 'שמור את כל האירועים';
                            return false;
                        }

                        const existingInput = form.querySelector('input[name="parsedEventsJson"]');
                        if (existingInput) {
                            existingInput.remove();
                        }

                        const eventsInput = document.createElement('input');
                        eventsInput.type = 'hidden';
                        eventsInput.name = 'parsedEventsJson';
                        eventsInput.value = eventsJson;
                        form.appendChild(eventsInput);

                        setTimeout(function() {
                            form.submit();
                        }, 100);
                    } catch (error) {
                        alert('שגיאה: ' + error.message);
                        saveBtn.disabled = false;
                        saveBtn.textContent = 'שמור את כל האירועים';
                    }
                    
                    return false;
                });
            }

            if (cancelBtn) {
                cancelBtn.addEventListener('click', function() {
                    parsedEvents = [];
                    parsedEventsContainer.style.display = 'none';
                    parsedEventsList.innerHTML = '';
                    pasteTextarea.value = '';
                });
            }
        })();
    </script>
</asp:Content>
