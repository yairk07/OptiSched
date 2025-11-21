<%@ Page Title="כל האירועים" Language="C#" 
    MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" 
    CodeFile="allEvents.aspx.cs" 
    Inherits="allEvents" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <h2 style="text-align:center;">כל האירועים בכל המשתמשים</h2>
    <div style="width:70%; margin:20px auto; text-align:center;">
    <asp:TextBox ID="txtSearch" runat="server" CssClass="search-box" 
                 Placeholder="חפש לפי כותרת / שם משתמש / הערות"></asp:TextBox>
    <asp:Button ID="btnSearch" runat="server" Text="חפש" CssClass="search-btn"
                OnClick="btnSearch_Click" />
</div>

<asp:Label ID="lblResult" runat="server" CssClass="search-result"></asp:Label>

    <div class="events-table-container">
        <asp:DataList ID="dlEvents" runat="server" 
                  RepeatLayout="Table" 
                  RepeatDirection="Vertical"
                  CssClass="events-table">
            <HeaderTemplate>
                <table class="events-table">
                    <thead>
                        <tr>
                            <th>כותרת</th>
                            <th>משתמש</th>
                            <th>תאריך</th>
                            <th>שעה</th>
                            <th>הערות</th>
                            <th>פעולות</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                        <tr>
                            <td><%# Eval("Title") %></td>
                            <td><%# Eval("UserName") %> (#<%# Eval("UserId") %>)</td>
                            <td><%# Eval("EventDate", "{0:dd/MM/yyyy}") %></td>
                            <td><%# Eval("EventTime") %></td>
                            <td><%# Eval("Notes") %></td>
                            <td><a href='editEvent.aspx?id=<%# Eval("Id") %>' class="edit-link">ערוך</a></td>
                        </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:DataList>
    </div>

    <style>
        .events-table-container {
            width: min(1500px, 95%);
            margin: 30px auto 60px;
            overflow-x: auto;
        }

        .events-table {
            width: 100%;
            border-collapse: collapse;
            background: var(--surface);
            border-radius: 12px;
            overflow: hidden;
            box-shadow: var(--shadow-md);
        }

        .events-table thead {
            background: var(--brand);
            color: #fff;
        }

        .events-table th {
            padding: 16px;
            text-align: right;
            font-weight: 600;
            font-size: 15px;
            border-bottom: 2px solid rgba(255,255,255,.2);
        }

        .events-table td {
            padding: 14px 16px;
            text-align: right;
            border-bottom: 1px solid var(--border);
            color: var(--text);
        }

        .events-table tbody tr:hover {
            background: rgba(229, 9, 20, 0.05);
        }

        .events-table tbody tr:last-child td {
            border-bottom: none;
        }

        .edit-link {
            background: var(--brand);
            color: #fff;
            padding: 6px 14px;
            border-radius: 6px;
            font-weight: 600;
            text-decoration: none;
            transition: background .2s ease;
            display: inline-block;
        }

        .edit-link:hover {
            background: var(--brand-dark);
            text-decoration: none;
        }

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
    </style>

</asp:Content>
