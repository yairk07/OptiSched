<%@ Page Title="רשימת משתמשים" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="exusers.aspx.cs" Inherits="exusers" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="users-shell">
        <div class="users-hero">
            <h2 class="hero-title"><i class="fas fa-users"></i> רשימת משתמשים</h2>
            <p class="hero-description">ניהול וצפייה בכל המשתמשים הרשומים במערכת</p>
        </div>

        <div class="users-search-section">
            <div class="search-wrapper">
                <i class="fas fa-search search-icon"></i>
                <asp:TextBox ID="txtSearchemail" runat="server" CssClass="search-input" 
                    Placeholder="חפש לפי שם משתמש, שם פרטי, שם משפחה, אימייל, טלפון או עיר..." 
                    AutoPostBack="false" />
            </div>
            <asp:Button ID="btnSearch" runat="server" Text="חפש" OnClick="btnSearch_Click" CssClass="search-button" />
            <asp:Button ID="btnClear" runat="server" Text="נקה" OnClick="btnClear_Click" CssClass="clear-button" />
        </div>

        <asp:Label ID="lblGridMessage" runat="server" CssClass="grid-message" Visible="false" />
        <p class="grid-hint" runat="server" id="pEditHint" visible="false">אפשר לערוך בלחיצה על ערך בטבלה, או דרך כפתור העריכה.</p>

        <div class="table-container">
            <asp:GridView ID="gvUsers" runat="server" 
                AutoGenerateColumns="false"
                CssClass="users-table"
                AllowPaging="true"
                PageSize="20"
                DataKeyNames="id"
                OnPageIndexChanging="gvUsers_PageIndexChanging"
                OnRowDataBound="gvUsers_RowDataBound"
                OnRowEditing="gvUsers_RowEditing"
                OnRowCancelingEdit="gvUsers_RowCancelingEdit"
                OnRowUpdating="gvUsers_RowUpdating"
                PagerStyle-CssClass="pager-style"
                HeaderStyle-CssClass="header-style"
                RowStyle-CssClass="row-style"
                AlternatingRowStyle-CssClass="alternating-row-style">
                <Columns>
                    <asp:TemplateField HeaderText="#" ItemStyle-CssClass="avatar-cell">
                        <ItemTemplate>
                            <div class="table-avatar">
                                <%# GetAvatarLetter(Container) %>
                            </div>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <div class="table-avatar">
                                <%# GetAvatarLetter(Container) %>
                            </div>
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="שם משתמש" ItemStyle-CssClass="user-name-cell">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkUserName" runat="server" CommandName="Edit" CssClass="editable-cell"
                                Enabled='<%# IsOwner() %>'
                                Text='<%# Connect.FixEncoding(Eval("userName") != null ? Eval("userName").ToString() : "") %>' />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtEditUserName" runat="server" CssClass="grid-edit-input"
                                Text='<%# Bind("userName") %>' />
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="שם פרטי">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkFirstName" runat="server" CommandName="Edit" CssClass="editable-cell"
                                Enabled='<%# IsOwner() %>'
                                Text='<%# Connect.FixEncoding(Eval("firstName") != null ? Eval("firstName").ToString() : "") %>' />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtEditFirstName" runat="server" CssClass="grid-edit-input"
                                Text='<%# Bind("firstName") %>' />
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="שם משפחה">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkLastName" runat="server" CommandName="Edit" CssClass="editable-cell"
                                Enabled='<%# IsOwner() %>'
                                Text='<%# Connect.FixEncoding(Eval("lastName") != null ? Eval("lastName").ToString() : "") %>' />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtEditLastName" runat="server" CssClass="grid-edit-input"
                                Text='<%# Bind("lastName") %>' />
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="אימייל" ItemStyle-CssClass="email-cell">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkEmail" runat="server" CommandName="Edit" CssClass="editable-cell"
                                Enabled='<%# IsOwner() %>'
                                Text='<%# Connect.FixEncoding(Eval("email") != null ? Eval("email").ToString() : "") %>' />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtEditEmail" runat="server" CssClass="grid-edit-input"
                                Text='<%# Bind("email") %>' />
                            <asp:RequiredFieldValidator ID="rfvEditEmail" runat="server"
                                ControlToValidate="txtEditEmail" ValidationGroup="GridEditUser"
                                ErrorMessage="לא ניתן לערוך: חובה להזין אימייל"
                                CssClass="grid-field-error" Display="Dynamic" />
                            <asp:RegularExpressionValidator ID="revEditEmail" runat="server"
                                ControlToValidate="txtEditEmail" ValidationGroup="GridEditUser"
                                ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                                ErrorMessage="לא ניתן לערוך: מייל לא תקין"
                                CssClass="grid-field-error" Display="Dynamic" />
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="טלפון">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkPhone" runat="server" CommandName="Edit" CssClass="editable-cell"
                                Enabled='<%# IsOwner() %>'
                                Text='<%# Connect.FixEncoding(Eval("phonenum") != null ? Eval("phonenum").ToString() : "") %>' />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtEditPhone" runat="server" CssClass="grid-edit-input"
                                Text='<%# Bind("phonenum") %>' />
                            <asp:RequiredFieldValidator ID="rfvEditPhone" runat="server"
                                ControlToValidate="txtEditPhone" ValidationGroup="GridEditUser"
                                ErrorMessage="לא ניתן לערוך: חובה להזין טלפון"
                                CssClass="grid-field-error" Display="Dynamic" />
                            <asp:RegularExpressionValidator ID="revEditPhone" runat="server"
                                ControlToValidate="txtEditPhone" ValidationGroup="GridEditUser"
                                ValidationExpression="^0(5\d|[2-489]|7[2-9])-?\d{7}$"
                                ErrorMessage="לא ניתן לערוך: מספר טלפון לא תקין"
                                CssClass="grid-field-error" Display="Dynamic" />
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="עיר">
                        <ItemTemplate>
                            <%# GetCity(Container) %>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <%# GetCity(Container) %>
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="פעולות" ItemStyle-CssClass="actions-cell">
                        <ItemTemplate>
                            <div class="action-buttons">
                                <asp:HyperLink ID="lnkEdit" runat="server"
                                    NavigateUrl='<%# "editUser.aspx?id=" + Eval("id") %>'
                                    CssClass="action-btn edit-btn"
                                    Visible='<%# IsOwner() %>'
                                    ToolTip="ערוך משתמש">
                                    <i class="fas fa-edit"></i>
                                </asp:HyperLink>
                                <asp:HyperLink ID="lnkMoreInfo" runat="server"
                                    NavigateUrl='<%# "exuserdetails.aspx?id=" + Eval("id") %>'
                                    CssClass="action-btn info-btn"
                                    ToolTip="פרטים נוספים">
                                    <i class="fas fa-eye"></i>
                                </asp:HyperLink>
                                <asp:LinkButton ID="btnResetPassword" runat="server"
                                    CommandArgument='<%# Eval("id") %>'
                                    OnClick="btnResetPassword_Click"
                                    OnClientClick="return confirm('האם אתה בטוח שברצונך לאפס את הסיסמה של המשתמש הזה? הסיסמה החדשה תהיה: 123456');"
                                    CssClass="action-btn reset-btn"
                                    Visible='<%# IsOwner() %>'
                                    ToolTip="אפס סיסמה">
                                    <i class="fas fa-lock"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="btnDeleteUser" runat="server"
                                    CommandArgument='<%# Eval("id") %>'
                                    OnClick="btnDeleteUser_Click"
                                    OnClientClick="return confirm('האם אתה בטוח שברצונך למחוק את המשתמש הזה? פעולה זו לא ניתנת לביטול.');"
                                    CssClass="action-btn delete-btn"
                                    Visible='<%# IsOwner() %>'
                                    ToolTip="מחק משתמש">
                                    <i class="fas fa-user-times"></i>
                                </asp:LinkButton>
                            </div>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <div class="action-buttons">
                                <asp:LinkButton ID="btnSaveRow" runat="server" CommandName="Update"
                                    CssClass="action-btn edit-btn" ToolTip="שמור"
                                    ValidationGroup="GridEditUser" CausesValidation="true">
                                    <i class="fas fa-save"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="btnCancelRow" runat="server" CommandName="Cancel"
                                    CssClass="action-btn delete-btn" ToolTip="בטל"
                                    CausesValidation="false">
                                    <i class="fas fa-times"></i>
                                </asp:LinkButton>
                            </div>
                        </EditItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <div class="empty-state">
                        <i class="fas fa-users-slash"></i>
                        <p>לא נמצאו משתמשים</p>
                    </div>
                </EmptyDataTemplate>
            </asp:GridView>
        </div>
    </section>

    <style>
        .editable-cell {
            color: inherit;
            text-decoration: none;
            border-bottom: 1px dashed transparent;
            display: inline-block;
            min-width: 40px;
        }
        .editable-cell:hover {
            border-bottom-color: var(--brand, #c62828);
            color: var(--brand, #c62828);
            cursor: pointer;
        }
        a.editable-cell[disabled],
        a.editable-cell[disabled]:hover {
            cursor: default;
            color: inherit;
            border-bottom-color: transparent;
            pointer-events: none;
        }
        .grid-edit-input {
            width: 100%;
            max-width: 180px;
            padding: 6px 8px;
            border: 1px solid #d1d5db;
            border-radius: 6px;
            font-size: 14px;
        }
        .grid-field-error {
            display: block;
            color: #c5221f;
            font-size: 12px;
            margin-top: 4px;
            max-width: 180px;
            line-height: 1.3;
        }
        .grid-message {
            display: block;
            margin: 0 auto 12px;
            width: min(1400px, 98%);
            padding: 10px 14px;
            border-radius: 8px;
            box-sizing: border-box;
        }
        .grid-message.ok { background: #e6f4ea; color: #137333; }
        .grid-message.err { background: #fce8e6; color: #c5221f; }
        .grid-hint {
            width: min(1400px, 98%);
            margin: 0 auto 10px;
            color: #6b7280;
            font-size: 14px;
            padding: 0 20px;
            box-sizing: border-box;
        }
        .users-shell {
            width: min(1400px, 98%);
            margin: 30px auto 60px;
            padding: 0 20px;
        }

        .users-hero {
            text-align: center;
            margin-bottom: 40px;
        }

        .users-hero .hero-title {
            font-size: 36px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 12px;
        }

        .users-hero .hero-title i {
            color: var(--brand);
        }

        .users-hero .hero-description {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .users-search-section {
            display: flex;
            gap: 12px;
            justify-content: center;
            margin-bottom: 30px;
            max-width: 900px;
            margin-left: auto;
            margin-right: auto;
            align-items: center;
        }

        .search-wrapper {
            position: relative;
            flex: 1;
            max-width: 600px;
        }

        .search-icon {
            position: absolute;
            right: 15px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--text);
            opacity: 0.5;
            pointer-events: none;
        }

        .search-input {
            width: 100%;
            padding: 12px 45px 12px 18px;
            border: 2px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--surface);
            color: var(--text);
            transition: border-color 0.3s ease;
        }

        .search-input:focus {
            outline: none;
            border-color: var(--brand);
        }

        .search-button, .clear-button {
            padding: 12px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            transition: background .2s ease;
            font-size: 15px;
        }

        .search-button:hover {
            background: var(--brand-dark);
        }

        .clear-button {
            background: #6c757d;
        }

        .clear-button:hover {
            background: #5a6268;
        }

        .table-container {
            background: var(--surface);
            border-radius: 12px;
            padding: 20px;
            box-shadow: var(--shadow-md);
            overflow-x: auto;
        }

        .users-table {
            width: 100%;
            border-collapse: collapse;
            direction: rtl;
        }

        .users-table th {
            background: var(--brand);
            color: #fff;
            padding: 15px 12px;
            text-align: right;
            font-weight: 600;
            font-size: 14px;
            border-bottom: 2px solid rgba(255,255,255,0.2);
        }

        .users-table td {
            padding: 15px 12px;
            border-bottom: 1px solid var(--border);
            font-size: 14px;
        }

        .row-style {
            background: var(--surface);
        }

        .alternating-row-style {
            background: rgba(0,0,0,0.02);
        }

        .row-style:hover, .alternating-row-style:hover {
            background: rgba(0,0,0,0.05);
        }

        .table-avatar {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            background: var(--brand);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 16px;
            font-weight: 700;
        }

        .user-name-cell {
            font-weight: 600;
            color: var(--heading);
        }

        .email-cell {
            color: var(--brand);
        }

        .actions-cell {
            text-align: center;
        }

        .action-buttons {
            display: flex;
            gap: 8px;
            justify-content: center;
            align-items: center;
        }

        .action-btn {
            width: 36px;
            height: 36px;
            border: 1px solid var(--border);
            border-radius: 6px;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            transition: all 0.2s ease;
            font-size: 14px;
            text-decoration: none;
            background: var(--surface);
            color: var(--text);
        }

        .edit-btn {
            background: var(--surface);
            color: var(--brand);
            border-color: var(--brand);
        }

        .edit-btn:hover {
            background: var(--brand);
            color: #fff;
            transform: translateY(-2px);
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }

        .info-btn {
            background: var(--surface);
            color: var(--text);
            border-color: var(--border);
        }

        .info-btn:hover {
            background: rgba(0,0,0,0.05);
            color: var(--brand);
            transform: translateY(-2px);
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }

        .reset-btn {
            background: var(--surface);
            color: var(--text);
            border-color: var(--border);
        }

        .reset-btn:hover {
            background: rgba(255,193,7,0.1);
            color: #f57c00;
            border-color: #ffc107;
            transform: translateY(-2px);
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }

        .delete-btn {
            background: var(--surface);
            color: var(--text);
            border-color: var(--border);
        }

        .delete-btn:hover {
            background: rgba(220,53,69,0.1);
            color: #c82333;
            border-color: #dc3545;
            transform: translateY(-2px);
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
        }

        .empty-state {
            text-align: center;
            padding: 60px 20px;
            color: var(--text);
            opacity: 0.6;
        }

        .empty-state i {
            font-size: 64px;
            margin-bottom: 20px;
            display: block;
        }

        .empty-state p {
            font-size: 18px;
        }

        .pager-style {
            padding: 15px;
            text-align: center;
            direction: rtl;
        }

        .pager-style a {
            padding: 8px 12px;
            margin: 0 4px;
            background: var(--brand);
            color: #fff;
            text-decoration: none;
            border-radius: 4px;
        }

        .pager-style a:hover {
            background: var(--brand-dark);
        }

        .pager-style span {
            padding: 8px 12px;
            margin: 0 4px;
            background: var(--brand-dark);
            color: #fff;
            border-radius: 4px;
        }

        @media (max-width: 768px) {
            .users-table {
                font-size: 12px;
            }

            .users-table th,
            .users-table td {
                padding: 10px 8px;
            }

            .action-btn {
                width: 32px;
                height: 32px;
                font-size: 12px;
            }

            .users-search-section {
                flex-direction: column;
            }

            .search-wrapper {
                width: 100%;
            }
        }
    </style>

</asp:Content>
