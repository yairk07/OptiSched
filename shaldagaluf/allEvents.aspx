<%@ Page Title="כל האירועים" Language="C#" 
    MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" 
    CodeFile="allEvents.aspx.cs" 
    Inherits="allEvents" 
    ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="page-header">
        <h2 class="page-title">כל האירועים בכל המשתמשים</h2>
        <div class="view-toggle">
            <asp:Button ID="btnTableView" runat="server" Text="תצוגת טבלה" CssClass="view-btn active" OnClick="btnViewToggle_Click" CommandArgument="table" />
            <asp:Button ID="btnCalendarView" runat="server" Text="תצוגת לוח שנה" CssClass="view-btn" OnClick="btnViewToggle_Click" CommandArgument="calendar" />
        </div>
    </div>

    <div class="search-section">
        <div class="search-row">
            <asp:TextBox ID="txtSearch" runat="server" CssClass="search-box" 
                         Placeholder="חפש לפי כותרת / שם משתמש / הערות"></asp:TextBox>
            <asp:DropDownList ID="ddlCategoryFilter" runat="server" CssClass="category-filter" AutoPostBack="true" OnSelectedIndexChanged="ddlCategoryFilter_SelectedIndexChanged">
                <asp:ListItem Text="כל הקטגוריות" Value="" Selected="True"></asp:ListItem>
                <asp:ListItem Text="אירוע" Value="אירוע"></asp:ListItem>
                <asp:ListItem Text="יום הולדת" Value="יום הולדת"></asp:ListItem>
                <asp:ListItem Text="פגישה" Value="פגישה"></asp:ListItem>
                <asp:ListItem Text="מטלה" Value="מטלה"></asp:ListItem>
                <asp:ListItem Text="אחר" Value="אחר"></asp:ListItem>
            </asp:DropDownList>
            <asp:Button ID="btnSearch" runat="server" Text="חפש" CssClass="search-btn"
                        OnClick="btnSearch_Click" />
        </div>
    </div>

    <asp:Label ID="lblResult" runat="server" CssClass="search-result"></asp:Label>

    <asp:Panel ID="pnlTableView" runat="server" CssClass="view-panel">
        <div class="events-table-container">
            <asp:DataList ID="dlEvents" runat="server" 
                      RepeatLayout="Table" 
                      RepeatDirection="Vertical"
                      CssClass="events-table"
                      OnItemDataBound="dlEvents_ItemDataBound">
                <HeaderTemplate>
                    <table class="events-table">
                        <thead>
                            <tr>
                                <th>כותרת</th>
                                <th>משתמש</th>
                                <th>תאריך</th>
                                <th>שעה</th>
                                <th>קטגוריה</th>
                                <th>הערות</th>
                                <th>פעולות</th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                            <tr>
                                <td><asp:Literal ID="litTitle" runat="server" /></td>
                                <td><asp:Literal ID="litUserName" runat="server" /></td>
                                <td><asp:Literal ID="litEventDate" runat="server" /></td>
                                <td><asp:Literal ID="litEventTime" runat="server" /></td>
                                <td><asp:Literal ID="litCategory" runat="server" /></td>
                                <td><asp:Literal ID="litNotes" runat="server" /></td>
                                <td><asp:HyperLink ID="lnkEdit" runat="server" CssClass="edit-link" Text="ערוך" /></td>
                            </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                </FooterTemplate>
            </asp:DataList>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlCalendarView" runat="server" CssClass="view-panel">
        <div class="calendar-view-container">
            <div class="calendar-nav">
                <asp:LinkButton ID="btnPrevMonth" runat="server" CssClass="nav-btn" OnClick="btnMonthChange_Click" CommandArgument="prev">&#8249; חודש קודם</asp:LinkButton>
                <asp:Label ID="lblCurrentMonth" runat="server" CssClass="month-label" />
                <asp:LinkButton ID="btnNextMonth" runat="server" CssClass="nav-btn" OnClick="btnMonthChange_Click" CommandArgument="next">חודש הבא &#8250;</asp:LinkButton>
            </div>
            <asp:Calendar ID="calEvents" runat="server" 
                          CssClass="events-calendar"
                          OnDayRender="calEvents_DayRender"
                          OnVisibleMonthChanged="calEvents_VisibleMonthChanged"
                          ShowTitle="false"
                          ShowNextPrevMonth="false"
                          SelectionMode="None" />
            <asp:Label ID="lblCalendarMessage" runat="server" CssClass="calendar-message" />
            <div class="calendar-legend">
                <div class="legend-item">
                    <span class="legend-color personal"></span>
                    <span>אירוע אישי</span>
                </div>
                <div class="legend-item">
                    <span class="legend-color shared"></span>
                    <span>אירוע משותף</span>
                </div>
            </div>
        </div>
    </asp:Panel>

    <style>
        .search-box {
            width: 40%;
            padding: 10px 14px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 14px;
            direction: rtl;
            background: var(--surface);
            color: var(--text);
        }

        .search-btn {
            padding: 10px 20px;
            background: var(--brand);
            color: white;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            margin-right: 8px;
            font-weight: 600;
            transition: background .2s ease;
        }

        .search-btn:hover {
            background: var(--brand-dark);
        }

        .search-result {
            display: block;
            margin: 15px auto;
            width: 70%;
            text-align: center;
            color: var(--text);
            font-size: 15px;
        }

        .page-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            width: min(1500px, 95%);
            margin: 30px auto 20px;
            padding: 0 20px;
        }

        .page-title {
            font-size: 28px;
            font-weight: 700;
            color: var(--heading);
            margin: 0;
        }

        .view-toggle {
            display: flex;
            gap: 8px;
        }

        .view-btn {
            padding: 10px 20px;
            background: var(--surface);
            color: var(--text);
            border: 2px solid var(--border);
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            transition: all .2s ease;
        }

        .view-btn:hover {
            background: rgba(229, 9, 20, 0.05);
            border-color: var(--brand);
        }

        .view-btn.active {
            background: var(--brand);
            color: #fff;
            border-color: var(--brand);
        }

        .search-section {
            width: 70%;
            margin: 20px auto;
            text-align: center;
        }

        .search-row {
            display: flex;
            gap: 12px;
            align-items: center;
            justify-content: center;
            flex-wrap: wrap;
        }

        .category-filter {
            padding: 10px 14px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 14px;
            direction: rtl;
            background: var(--surface);
            color: var(--text);
            cursor: pointer;
            min-width: 150px;
        }

        .category-filter:focus {
            outline: none;
            border-color: var(--brand);
            box-shadow: 0 0 0 3px rgba(229, 9, 20, 0.1);
        }

        .view-panel {
            width: min(1500px, 95%);
            margin: 30px auto 60px;
        }

        .calendar-view-container {
            background: var(--surface);
            border-radius: 20px;
            padding: 30px;
            box-shadow: var(--shadow-md);
        }

        .calendar-nav {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 30px;
        }

        .nav-btn {
            padding: 10px 20px;
            background: var(--brand);
            color: #fff;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            transition: background .2s ease;
        }

        .nav-btn:hover {
            background: var(--brand-dark);
            text-decoration: none;
        }

        .month-label {
            font-size: 24px;
            font-weight: 700;
            color: var(--heading);
        }

        .events-calendar {
            width: 100%;
            border: none;
            font-family: inherit;
        }

        .events-calendar table {
            width: 100%;
            border-collapse: collapse;
        }

        .events-calendar th {
            background: var(--brand);
            color: #fff;
            padding: 12px;
            text-align: center;
            font-weight: 600;
            border: 1px solid rgba(255,255,255,.2);
        }

        .events-calendar td {
            border: 1px solid var(--border);
            padding: 0;
            vertical-align: top;
            height: 100px;
            width: 14.28%;
        }

        .events-calendar .day-cell {
            height: 100%;
            padding: 8px;
            position: relative;
        }

        .events-calendar .day-number {
            font-weight: 600;
            font-size: 16px;
            margin-bottom: 4px;
        }

        .events-calendar .day-events {
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
        }

        .event-badge.personal {
            background: #e50914;
            color: #fff;
        }

        .event-badge.shared {
            background: #0066cc;
            color: #fff;
        }

        .event-badge:hover {
            opacity: 0.9;
            transform: scale(1.02);
        }

        .calendar-legend {
            display: flex;
            justify-content: center;
            gap: 30px;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid var(--border);
        }

        .legend-item {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .legend-color {
            width: 20px;
            height: 20px;
            border-radius: 4px;
        }

        .legend-color.personal {
            background: #e50914;
        }

        .legend-color.shared {
            background: #0066cc;
        }

        .calendar-message {
            display: block;
            text-align: center;
            margin: 20px 0;
            font-size: 14px;
            font-weight: 600;
        }

        @media (max-width: 768px) {
            .page-header {
                flex-direction: column;
                gap: 20px;
            }

            .view-toggle {
                width: 100%;
                justify-content: center;
            }

            .calendar-nav {
                flex-direction: column;
                gap: 15px;
            }
        }
    </style>

</asp:Content>
