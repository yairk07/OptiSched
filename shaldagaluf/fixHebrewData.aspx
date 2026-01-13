<%@ Page Title="תיקון קידוד עברית" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="fixHebrewData.aspx.cs" Inherits="fixHebrewData" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="head" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="body" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div style="max-width: 800px; margin: 40px auto; padding: 20px;">
        <h2>תיקון קידוד עברית במסד נתונים</h2>
        
        <asp:Panel ID="pnlNotAuthorized" runat="server" Visible="false">
            <div style="background: #fff3cd; border: 1px solid #ffc107; padding: 20px; border-radius: 8px; margin-bottom: 20px;">
                <strong>אין הרשאה</strong>
                <p>רק מנהלים יכולים לגשת לדף זה.</p>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlContent" runat="server">
            <div style="background: white; padding: 30px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                <div style="margin-bottom: 20px;">
                    <label style="display: block; margin-bottom: 8px; font-weight: 600;">בחר טבלה:</label>
                    <asp:DropDownList ID="ddlTable" runat="server" CssClass="form-control" style="width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px;">
                        <asp:ListItem Text="Users" Value="Users"></asp:ListItem>
                        <asp:ListItem Text="CalendarEvents" Value="CalendarEvents"></asp:ListItem>
                        <asp:ListItem Text="SharedCalendarEvents" Value="SharedCalendarEvents"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div style="margin-bottom: 20px;">
                    <asp:Button ID="btnFix" runat="server" Text="תקן קידוד" OnClick="btnFix_Click" 
                        CssClass="btn btn-primary" style="padding: 12px 24px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px;" />
                </div>

                <asp:Panel ID="pnlResults" runat="server" Visible="false">
                    <div style="background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; border-radius: 8px; margin-top: 20px;">
                        <h3 style="margin-top: 0; color: #155724;">תוצאות:</h3>
                        <asp:Label ID="lblResults" runat="server" style="white-space: pre-wrap; font-family: monospace;"></asp:Label>
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </div>
</asp:Content>
