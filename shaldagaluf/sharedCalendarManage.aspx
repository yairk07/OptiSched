<%@ Page Title="ניהול טבלה משותפת" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="sharedCalendarManage.aspx.cs" Inherits="sharedCalendarManage" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <meta charset="utf-8" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="manage-shell">
        <asp:Panel ID="pnlContent" runat="server">
            <div class="manage-header">
                <asp:HyperLink ID="lnkBack" runat="server" CssClass="back-link">&laquo; חזרה לטבלה</asp:HyperLink>
                <h2 class="manage-title">ניהול טבלה (מנהל בלבד)</h2>
                <asp:Label ID="lblMessage" runat="server" CssClass="form-message" Visible="false" />
            </div>

            <div class="manage-section">
                <h3>פרטי הטבלה</h3>
                <div class="form-row">
                    <label>שם הטבלה</label>
                    <asp:TextBox ID="txtName" runat="server" CssClass="task-input" />
                </div>
                <div class="form-row">
                    <label>תיאור</label>
                    <asp:TextBox ID="txtDescription" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="3" />
                </div>
                <asp:Button ID="btnSaveInfo" runat="server" Text="שמור פרטים" CssClass="task-button" OnClick="btnSaveInfo_Click" />
            </div>

            <div class="manage-section builtin-fields-section">
                <div class="cf-section-head">
                    <h3>אילו שדות יופיעו בטבלה</h3>
                    <p class="hint">סמן אילו עמודות יוצגו בלוח ובטבלת האירועים. סמן &quot;חובה&quot; לשדות שחייבים למלא בשמירת אירוע. כותרת ותאריך תמיד חובה.</p>
                </div>
                <asp:Repeater ID="rptBuiltinFields" runat="server" OnItemDataBound="rptBuiltinFields_ItemDataBound">
                    <HeaderTemplate>
                        <div class="bf-list">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <article class="bf-card">
                            <div class="bf-card-main">
                                <span class="bf-label"><%# Eval("Label") %></span>
                                <asp:Literal ID="litAlwaysRequired" runat="server" />
                            </div>
                            <div class="bf-toggles">
                                <asp:CheckBox ID="chkVisible" runat="server" Text="הצג" CssClass="bf-check bf-check-visible" />
                                <asp:CheckBox ID="chkRequired" runat="server" Text="חובה" CssClass="bf-check bf-check-required" />
                                <asp:HiddenField ID="hfFieldKey" runat="server" Value='<%# Eval("Key") %>' />
                            </div>
                        </article>
                    </ItemTemplate>
                    <FooterTemplate>
                        </div>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Button ID="btnSaveFields" runat="server" Text="שמור שדות תצוגה" CssClass="task-button" OnClick="btnSaveFields_Click" />
            </div>

            <div class="manage-section custom-fields-section">
                <div class="cf-section-head">
                    <h3>שדות מותאמים</h3>
                    <p class="hint">הוסף שדות נוספים לטופס ולטבלת האירועים — טקסט, רשימה או כפתור. סמן &quot;חובה&quot; לשדות שחייבים למלא בשמירת אירוע.</p>
                </div>

                <asp:Repeater ID="rptCustomFields" runat="server" OnItemCommand="rptCustomFields_ItemCommand" OnItemDataBound="rptCustomFields_ItemDataBound">
                    <HeaderTemplate>
                        <div class="cf-list">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <article class="cf-card">
                            <div class="cf-card-top">
                                <div class="cf-card-title-row">
                                    <span class="cf-badge cf-badge-<%# Eval("FieldType") %>"><%# Eval("TypeLabel") %></span>
                                    <asp:Label ID="lblViewLabel" runat="server" CssClass="cf-label" Text='<%# Eval("FieldLabel") %>' />
                                    <asp:Literal ID="litRequiredBadge" runat="server" />
                                </div>
                                <div class="cf-card-actions">
                                    <asp:LinkButton ID="btnEditField" runat="server" CssClass="cf-action" CommandName="EditField" CommandArgument='<%# Eval("Id") %>'>ערוך</asp:LinkButton>
                                    <asp:LinkButton ID="btnDeleteField" runat="server" CssClass="cf-action cf-action-danger" CommandName="DeleteField" CommandArgument='<%# Eval("Id") %>'
                                        OnClientClick="return confirm('למחוק את השדה המותאם?');">מחק</asp:LinkButton>
                                </div>
                            </div>
                            <asp:Panel ID="pnlViewOptions" runat="server" CssClass="cf-options-preview" Visible="false">
                                <asp:Literal ID="litOptionsPreview" runat="server" />
                            </asp:Panel>
                            <asp:Panel ID="pnlEditField" runat="server" CssClass="cf-edit-panel" Visible="false">
                                <div class="cf-form-grid">
                                    <div class="form-row">
                                        <label>שם השדה</label>
                                        <asp:TextBox ID="txtEditLabel" runat="server" CssClass="task-input" />
                                    </div>
                                    <div class="form-row">
                                        <label>סוג</label>
                                        <asp:DropDownList ID="ddlEditType" runat="server" CssClass="task-input" AutoPostBack="true" OnSelectedIndexChanged="ddlEditType_SelectedIndexChanged">
                                            <asp:ListItem Text="טקסט" Value="text" />
                                            <asp:ListItem Text="רשימה" Value="list" />
                                            <asp:ListItem Text="כפתור" Value="button" />
                                        </asp:DropDownList>
                                    </div>
                                    <div class="form-row cf-required-row">
                                        <asp:CheckBox ID="chkEditRequired" runat="server" Text="שדה חובה" CssClass="cf-required-check" />
                                    </div>
                                    <div class="form-row">
                                        <asp:Label ID="lblEditOptions" runat="server" AssociatedControlID="txtEditOptions" Text="אפשרויות / URL" />
                                        <asp:TextBox ID="txtEditOptions" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="3" />
                                    </div>
                                </div>
                                <div class="cf-edit-actions">
                                    <asp:Button ID="btnUpdateField" runat="server" Text="שמור שינויים" CssClass="task-button" CommandName="UpdateField" CommandArgument='<%# Eval("Id") %>' />
                                    <asp:Button ID="btnCancelEdit" runat="server" Text="ביטול" CssClass="btn-small cf-btn-muted" CommandName="CancelEdit" />
                                </div>
                            </asp:Panel>
                        </article>
                    </ItemTemplate>
                    <FooterTemplate>
                        </div>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Label ID="lblNoCustomFields" runat="server" CssClass="hint cf-empty" Visible="false" Text="עדיין אין שדות מותאמים. הוסף שדה חדש למטה." />

                <div class="cf-add-card">
                    <h4 class="cf-add-title">הוספת שדה חדש</h4>
                    <div class="cf-form-grid">
                        <div class="form-row">
                            <label for="<%= txtNewFieldLabel.ClientID %>">שם השדה</label>
                            <asp:TextBox ID="txtNewFieldLabel" runat="server" CssClass="task-input" placeholder="למשל: מיקום / קישור למפה" />
                        </div>
                        <div class="form-row">
                            <label for="<%= ddlNewFieldType.ClientID %>">סוג השדה</label>
                            <asp:DropDownList ID="ddlNewFieldType" runat="server" CssClass="task-input" AutoPostBack="true" OnSelectedIndexChanged="ddlNewFieldType_SelectedIndexChanged">
                                <asp:ListItem Text="טקסט" Value="text" Selected="True" />
                                <asp:ListItem Text="רשימה" Value="list" />
                                <asp:ListItem Text="כפתור" Value="button" />
                            </asp:DropDownList>
                        </div>
                        <div class="form-row cf-required-row">
                            <asp:CheckBox ID="chkNewFieldRequired" runat="server" Text="שדה חובה" CssClass="cf-required-check" />
                        </div>
                        <asp:Panel ID="pnlNewFieldOptions" runat="server" CssClass="form-row" Visible="false">
                            <asp:Label ID="lblNewFieldOptions" runat="server" AssociatedControlID="txtNewFieldOptions" Text="אפשרויות" />
                            <asp:TextBox ID="txtNewFieldOptions" runat="server" CssClass="task-input" TextMode="MultiLine" Rows="3" />
                            <asp:Literal ID="litNewOptionsHint" runat="server" />
                        </asp:Panel>
                    </div>
                    <asp:Button ID="btnAddCustomField" runat="server" Text="הוסף שדה" CssClass="task-button" OnClick="btnAddCustomField_Click" />
                </div>
            </div>

            <div class="manage-section">
                <h3>חברים וצבעים</h3>
                <asp:Repeater ID="rptMembers" runat="server" OnItemCommand="rptMembers_ItemCommand" OnItemDataBound="rptMembers_ItemDataBound">
                    <HeaderTemplate>
                        <table class="manage-table">
                            <thead>
                                <tr>
                                    <th>משתמש</th>
                                    <th>צבע</th>
                                    <th>הרשאה</th>
                                    <th>פעולות</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td>
                                <span class="color-dot" style='background:<%# Eval("MemberColor") %>'></span>
                                <%# Eval("UserName") %>
                            </td>
                            <td>
                                <asp:TextBox ID="txtColor" runat="server" Text='<%# Eval("MemberColor") %>' CssClass="task-input color-input" TextMode="Color" />
                                <asp:HiddenField ID="hfUserId" runat="server" Value='<%# Eval("UserId") %>' />
                            </td>
                            <td>
                                <asp:DropDownList ID="ddlPerm" runat="server" CssClass="task-input">
                                    <asp:ListItem Text="ראיה בלבד" Value="Read" />
                                    <asp:ListItem Text="עריכה + ראיה" Value="ReadWrite" />
                                </asp:DropDownList>
                                <asp:HiddenField ID="hfPerm" runat="server" Value='<%# Eval("Permission") %>' />
                            </td>
                            <td>
                                <asp:Button ID="btnSaveMember" runat="server" Text="עדכן" CssClass="btn-small" CommandName="SaveMember" CommandArgument='<%# Eval("UserId") %>' />
                                <asp:Button ID="btnRemoveMember" runat="server" Text="הסר" CssClass="btn-small btn-danger"
                                    CommandName="RemoveMember" CommandArgument='<%# Eval("UserId") %>'
                                    OnClientClick="return confirm('להסיר את החבר מהטבלה?');" />
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Label ID="lblNoMembers" runat="server" Text="אין חברים בטבלה" Visible="false" CssClass="hint" />
            </div>

            <div class="manage-section">
                <h3>כל האירועים בטבלה</h3>
                <asp:Repeater ID="rptEvents" runat="server" OnItemCommand="rptEvents_ItemCommand">
                    <HeaderTemplate>
                        <table class="manage-table events-manage">
                            <thead>
                                <tr>
                                    <th>תאריך</th>
                                    <th>שעה</th>
                                    <th>כותרת</th>
                                    <th>קטגוריה</th>
                                    <th>נוצר ע&quot;י</th>
                                    <th>פעולות</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr style='border-right:4px solid <%# Eval("RowColor") %>;'>
                            <td><%# Eval("EventDateDisplay") %></td>
                            <td><%# Eval("EventTime") %></td>
                            <td><strong><%# Eval("Title") %></strong></td>
                            <td><%# Eval("Category") %></td>
                            <td><span style='color:<%# Eval("RowColor") %>'><%# Eval("CreatedByName") %></span></td>
                            <td>
                                <a class="edit-link" href='<%# "sharedCalendarDetails.aspx?id=" + CalendarId + "&editEvent=" + Eval("Id") %>'>ערוך</a>
                                <asp:LinkButton ID="lnkDelete" runat="server" CssClass="delete-link" CommandName="DeleteEvent"
                                    CommandArgument='<%# Eval("Id") %>'
                                    OnClientClick="return confirm('למחוק את האירוע?');">מחק</asp:LinkButton>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
                <asp:Label ID="lblNoEvents" runat="server" Text="אין אירועים בטבלה" Visible="false" CssClass="hint" />
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlDenied" runat="server" Visible="false" CssClass="manage-section">
            <h2>אין הרשאה</h2>
            <p>רק מנהל הטבלה יכול לגשת לדף זה.</p>
            <asp:HyperLink ID="lnkBackDenied" runat="server" NavigateUrl="sharedCalendars.aspx" CssClass="back-link">&laquo; חזרה</asp:HyperLink>
        </asp:Panel>
    </section>

    <style>
        .manage-shell { width: min(1100px, 95%); margin: 32px auto 60px; }
        .manage-header { margin-bottom: 24px; }
        .manage-title { margin: 8px 0 0; font-size: 1.6rem; }
        .manage-section {
            margin-bottom: 28px;
            padding: 18px 20px;
            border: 1px solid rgba(255,255,255,0.12);
            border-radius: 12px;
            background: rgba(0,0,0,0.15);
        }
        .manage-section h3 { margin: 0 0 12px; }
        .form-row { margin-bottom: 12px; display: flex; flex-direction: column; gap: 6px; }
        .hint { opacity: 0.75; margin: 0 0 12px; }
        .manage-table { width: 100%; border-collapse: collapse; }
        .builtin-fields-section { border-color: rgba(229, 9, 20, 0.22); }
        .bf-list { display: flex; flex-direction: column; gap: 10px; margin: 0 0 14px; }
        .bf-card {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            padding: 12px 16px;
            background: var(--surface-alt, rgba(255,255,255,0.04));
            border: 1px solid var(--border, rgba(255,255,255,0.1));
            border-radius: var(--radius-md, 12px);
        }
        .bf-card-main {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 8px;
            min-width: 0;
        }
        .bf-label { font-weight: 600; font-size: 1.02rem; color: var(--heading, #fff); }
        .bf-toggles {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 14px;
        }
        .bf-check { display: inline-flex; align-items: center; gap: 6px; margin: 0; white-space: nowrap; }
        .bf-check label { margin-right: 6px; color: var(--muted, #b5bbc7); cursor: pointer; }
        .bf-check-required label { color: var(--brand, #e50914); font-weight: 600; }
        .bf-check input[disabled] + label,
        .bf-check.bf-locked label { opacity: 0.85; cursor: default; }
        .bf-always-badge {
            font-size: 0.72rem;
            font-weight: 700;
            color: var(--brand, #e50914);
            background: rgba(229, 9, 20, 0.12);
            border: 1px solid rgba(229, 9, 20, 0.35);
            padding: 2px 8px;
            border-radius: 6px;
        }
        .manage-table th, .manage-table td { padding: 10px 8px; border-bottom: 1px solid rgba(255,255,255,0.1); text-align: right; vertical-align: middle; }
        .color-dot { display: inline-block; width: 12px; height: 12px; border-radius: 50%; margin-left: 8px; vertical-align: middle; }
        .color-input { max-width: 64px; padding: 2px; height: 36px; }
        .btn-small { padding: 6px 12px; margin: 0 4px; cursor: pointer; border-radius: 6px; border: none; background: #3498db; color: #fff; }
        .btn-danger { background: #c0392b; }
        .edit-link, .delete-link { margin-left: 10px; }
        .form-message { display: block; margin-top: 8px; color: #2ecc71; }
        .form-message.error { color: #e74c3c; }
        .back-link { text-decoration: none; opacity: 0.85; }
        .task-button { margin-top: 8px; }

        .custom-fields-section { border-color: rgba(229, 9, 20, 0.25); }
        .cf-section-head h3 { margin-bottom: 6px; }
        .cf-list { display: flex; flex-direction: column; gap: 12px; margin-bottom: 18px; }
        .cf-empty { display: block; margin-bottom: 14px; }
        .cf-card {
            background: var(--surface-alt, rgba(255,255,255,0.04));
            border: 1px solid var(--border, rgba(255,255,255,0.1));
            border-radius: var(--radius-md, 12px);
            padding: 14px 16px;
            box-shadow: var(--shadow-sm, none);
        }
        .cf-card-top {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
        }
        .cf-card-title-row {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 8px;
            min-width: 0;
        }
        .cf-label { font-weight: 600; font-size: 1.05rem; color: var(--heading, #fff); }
        .cf-badge {
            display: inline-block;
            font-size: 0.75rem;
            font-weight: 600;
            padding: 3px 10px;
            border-radius: 999px;
            letter-spacing: 0.02em;
            border: 1px solid transparent;
        }
        .cf-badge-text { background: rgba(52, 152, 219, 0.18); color: #7ec8f5; border-color: rgba(52, 152, 219, 0.35); }
        .cf-badge-list { background: rgba(155, 89, 182, 0.18); color: #d2a8ef; border-color: rgba(155, 89, 182, 0.35); }
        .cf-badge-button { background: rgba(229, 9, 20, 0.18); color: #ff8a90; border-color: rgba(229, 9, 20, 0.4); }
        .cf-required-badge {
            font-size: 0.72rem;
            font-weight: 700;
            color: var(--brand, #e50914);
            background: rgba(229, 9, 20, 0.12);
            border: 1px solid rgba(229, 9, 20, 0.35);
            padding: 2px 8px;
            border-radius: 6px;
        }
        .cf-card-actions { display: flex; gap: 8px; }
        .cf-action {
            color: var(--muted, #b5bbc7);
            text-decoration: none;
            font-size: 0.9rem;
            padding: 4px 8px;
            border-radius: 6px;
        }
        .cf-action:hover { color: var(--heading, #fff); background: rgba(255,255,255,0.06); }
        .cf-action-danger:hover { color: #ff8a90; background: rgba(229, 9, 20, 0.12); }
        .cf-options-preview {
            margin-top: 10px;
            font-size: 0.88rem;
            color: var(--muted, #b5bbc7);
            line-height: 1.45;
            word-break: break-word;
        }
        .cf-edit-panel {
            margin-top: 14px;
            padding-top: 14px;
            border-top: 1px solid var(--border, rgba(255,255,255,0.1));
        }
        .cf-form-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 12px 16px;
            margin-bottom: 12px;
        }
        .cf-required-row { justify-content: flex-end; }
        .cf-required-check label { margin-right: 8px; }
        .cf-edit-actions { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
        .cf-btn-muted { background: #555 !important; }
        .cf-add-card {
            margin-top: 8px;
            padding: 16px 18px;
            border-radius: var(--radius-md, 12px);
            border: 1px dashed rgba(229, 9, 20, 0.45);
            background: rgba(229, 9, 20, 0.06);
        }
        .cf-add-title { margin: 0 0 14px; font-size: 1.05rem; color: var(--heading, #fff); }
        .cf-options-hint { display: block; margin-top: 6px; font-size: 0.82rem; opacity: 0.75; }
    </style>
    <script type="text/javascript">
        (function () {
            function syncOptionsPanel(ddl, panel, label, hint) {
                if (!ddl || !panel) return;
                var t = (ddl.value || '').toLowerCase();
                var show = t === 'list' || t === 'button';
                panel.style.display = show ? '' : 'none';
                if (label) {
                    label.textContent = t === 'button' ? 'כתובת URL לכפתור' : 'אפשרויות לרשימה (שורה לכל אפשרות)';
                }
                if (hint) {
                    hint.textContent = t === 'button'
                        ? 'לדוגמה: https://maps.google.com/...'
                        : 'כל אפשרות בשורה נפרדת, או מופרדות בפסיקים';
                }
            }
            window.cfSyncNewType = function () {
                var ddl = document.getElementById('<%= ddlNewFieldType.ClientID %>');
                var panel = document.getElementById('<%= pnlNewFieldOptions.ClientID %>');
                var label = document.getElementById('<%= lblNewFieldOptions.ClientID %>');
                syncOptionsPanel(ddl, panel, label, null);
            };
        })();
    </script>
</asp:Content>
