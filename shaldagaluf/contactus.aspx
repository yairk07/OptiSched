<%@ Page Title="צור קשר" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="contactus.aspx.cs" Inherits="contactus" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="contact-shell">
        <div class="contact-container">
            <div class="contact-header">
                <h2 class="contact-title">צור קשר</h2>
                <p class="contact-subtitle">נשמח לשמוע ממך! שלח לנו הודעה ונחזור אליך בהקדם</p>
            </div>

            <div class="contact-form-container">
                <div class="form-group">
                    <label class="form-label">נושא פנייה</label>
                    <asp:DropDownList ID="DropDownList1" runat="server" CssClass="form-input">
                        <asp:ListItem Text="בחר נושא" Value="" Selected="True"></asp:ListItem>
                        <asp:ListItem Text="בעיה באתר" Value="1"></asp:ListItem>
                        <asp:ListItem Text="פנייה ליוצר" Value="2"></asp:ListItem>
                        <asp:ListItem Text="סתם לכיף" Value="3"></asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">שם פרטי</label>
                        <asp:TextBox ID="TextBox1" runat="server" CssClass="form-input" placeholder="הזן שם פרטי"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">שם משפחה</label>
                        <asp:TextBox ID="TextBox2" runat="server" CssClass="form-input" placeholder="הזן שם משפחה"></asp:TextBox>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">אימייל</label>
                    <asp:TextBox ID="TextBoxEmail" runat="server" CssClass="form-input" TextMode="Email" placeholder="your.email@example.com"></asp:TextBox>
                </div>

                <div class="form-group">
                    <label class="form-label">תוכן הפנייה</label>
                    <asp:TextBox ID="TextBox4" runat="server" CssClass="form-input form-textarea" TextMode="MultiLine" Rows="8" placeholder="כתוב כאן את הודעתך..."></asp:TextBox>
                </div>

                <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

                <div class="form-actions">
                    <asp:Button ID="btnSubmit" runat="server" Text="שלח הודעה" CssClass="btn-submit" />
                </div>
            </div>
        </div>
    </section>

    <style>
        .contact-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .contact-container {
            max-width: 700px;
            margin: 0 auto;
        }

        .contact-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .contact-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .contact-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .contact-form-container {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .form-group {
            margin-bottom: 24px;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }

        .form-label {
            display: block;
            font-weight: 600;
            color: var(--heading);
            margin-bottom: 8px;
            font-size: 15px;
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
            transition: border-color .2s ease, box-shadow .2s ease;
            box-sizing: border-box;
        }

        .form-input:focus {
            outline: none;
            border-color: var(--brand);
            box-shadow: 0 0 0 3px rgba(229, 9, 20, 0.1);
        }

        .form-textarea {
            resize: vertical;
            min-height: 120px;
            font-family: inherit;
        }

        .form-message {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-bottom: 20px;
            text-align: center;
            font-weight: 600;
            min-height: 24px;
        }

        .form-actions {
            margin-top: 32px;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .btn-submit {
            width: 100%;
            padding: 14px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 17px;
            cursor: pointer;
            transition: background .2s ease, transform .15s ease;
            box-shadow: 0 18px 35px rgba(229, 9, 20, 0.35);
        }

        .btn-submit:hover {
            background: var(--brand-dark);
            transform: translateY(-1px);
        }

        .btn-submit:active {
            transform: translateY(1px);
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }
        }
    </style>
</asp:Content>
