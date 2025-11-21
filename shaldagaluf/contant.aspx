<%@ Page Title="תוכן" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="contant.aspx.cs" Inherits="Default3" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <link href="StyleSheet.css" rel="stylesheet" />

    <style>
        .contant-wrapper {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 40px;
            border-radius: 20px;
            background: var(--surface);
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            text-align: center;
        }

        .legacy-logo {
            margin-bottom: 30px;
        }

        .legacy-logo img {
            max-width: 220px;
            border-radius: 16px;
            box-shadow: 0 12px 35px rgba(0, 0, 0, 0.35);
        }

        .contant-wrapper h2 {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 16px;
        }

        .contant-wrapper > p {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
            margin-bottom: 30px;
        }

        .contant-main-img {
            max-width: 400px;
            width: 100%;
            border-radius: 16px;
            box-shadow: var(--shadow-md);
            margin: 30px 0;
        }

        .cards-container {
            display: grid !important;
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: 24px;
            margin-top: 40px;
            width: 100%;
        }

        .info-card {
            background: var(--surface);
            padding: 20px;
            border-radius: 16px;
            border: 1px solid var(--border);
            box-shadow: var(--shadow-sm);
            text-align: center;
            cursor: pointer;
            transition: transform .2s ease, box-shadow .2s ease;
        }

        .info-card:hover {
            transform: translateY(-6px);
            box-shadow: var(--shadow-lg);
        }

        .info-card img {
            width: 100%;
            border-radius: 12px;
            margin-bottom: 16px;
            aspect-ratio: 16/9;
            object-fit: cover;
        }

        .info-card p {
            color: var(--text);
            font-size: 15px;
            margin: 0;
            font-weight: 500;
        }
    </style>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="contant-wrapper">

        <div class="legacy-logo">
            <img src="pics/sigma.png" alt="הלוגו הקודם של OptiSched" />
        </div>

        <h2>ברוכים הבאים</h2>
        <p>כאן תוכלו למצוא מידע נוסף, תמונות וקישורים שימושיים.</p>

        <img src="pics/הורדה.jpeg" class="contant-main-img" />

        <div style="margin-top: 20px;">
            <a class="weatherwidget-io"
               href="https://forecast7.com/he/31d0534d85/israel/"
               data-label_1="ISRAEL"
               data-label_2="WEATHER"
               data-theme="original">
               ISRAEL WEATHER
            </a>
        </div>

        <asp:DataList ID="dlCards" runat="server"
                      RepeatColumns="3"
                      CssClass="cards-container"
                      RepeatDirection="Horizontal">

            <ItemTemplate>
                <div class="info-card" onclick="navigateToURL('<%# Eval("url") %>')">
                    <img src='<%# Eval("image") %>' />
                    <p><%# Eval("text") %></p>
                </div>
            </ItemTemplate>

        </asp:DataList>

    </div>

</asp:Content>
