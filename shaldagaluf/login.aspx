<%@ Page Title="Login" Language="C#" MasterPageFile="~/danimaster.master" CodeFile="login.aspx.cs" AutoEventWireup="true" Inherits="login" ResponseEncoding="utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Productivity Hub</span>
                <h2>ברוך הבא למרכז הניהול</h2>
                <p>
                    התחבר כדי לצפות בלוחות הזמנים, לעדכן משימות ולהגיב בזמן אמת.
                    המערכת מותאמת לכל מכשיר ומאפשרת מקסום יעילות יומיומי.
                </p>

                <div class="auth-highlights">
                    <div class="auth-highlight">
                        <span>01</span>
                        עדכוני אירועים בזמן אמת
                    </div>
                    <div class="auth-highlight">
                        <span>02</span>
                        תכנון משימות חכם
                    </div>
                    <div class="auth-highlight">
                        <span>03</span>
                        התאמה לצוותים שונים
                    </div>
                </div>
            </div>

            <div class="auth-form">
                <h3>התחברות למערכת</h3>
                <p class="auth-support">הזן את פרטי המשתמש שסופקו לך על ידי מנהל המערכת</p>

                <asp:Button ID="btnGoogleLogin" runat="server" Text="התחבר עם Google" OnClientClick="showGoogleLoginModal(event); return false;" CssClass="google-button" />
                <div class="auth-divider">
                    <span>או</span>
                </div>

                <div class="form-field">
                    <label for="txtUserName">שם משתמש</label>
                    <asp:TextBox ID="txtUserName" runat="server" CssClass="textbox" placeholder="לדוגמה: yair.k" />
                </div>

                <div class="form-field">
                    <label for="txtPassword">סיסמה</label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="textbox" placeholder="••••••••" />
                </div>

                <asp:Button ID="btnLogin" runat="server" Text="כניסה למערכת" OnClick="btnLogin_Click" CssClass="button" />

                <asp:Label ID="lblError" runat="server" CssClass="auth-error" />

                <div class="auth-support">
                    <a href="login-with-code.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">התחבר עם קוד (ללא סיסמה)</a>
                    <a href="forgotPassword.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">שכחת סיסמה?</a>
                    לא רשומים עדיין? <a href="register.aspx">צרו משתמש חדש</a>
                </div>
            </div>
        </div>
    </section>

    <div id="googleLoginModal" class="google-login-modal" style="display: none;">
        <div class="google-login-modal-overlay" onclick="closeGoogleLoginModal()"></div>
        <div class="google-login-modal-content">
            <div class="google-login-modal-header">
                <h3>התחברות עם Google</h3>
                <button class="google-login-modal-close" onclick="closeGoogleLoginModal()">&times;</button>
            </div>
            <div class="google-login-modal-body">
                <div id="continueAsSection" class="google-login-option" style="display: none;">
                    <div class="google-login-user-info">
                        <div class="google-login-avatar">
                            <span id="googleLoginAvatarLetter"></span>
                        </div>
                        <div class="google-login-user-details">
                            <p class="google-login-email" id="googleLoginEmail"></p>
                            <p class="google-login-hint">המשך עם החשבון הזה</p>
                        </div>
                    </div>
                    <button id="btnContinueAs" class="google-login-primary-btn" onclick="continueAsGoogleUser()">
                        המשך כ-<span id="continueAsEmailText"></span>
                    </button>
                </div>
                <div class="google-login-divider" id="googleLoginDivider" style="display: none;"></div>
                <button id="btnSwitchAccount" class="google-login-secondary-btn" onclick="switchGoogleAccount()">
                    <img src="https://img.icons8.com/color/16/000000/google-logo.png" alt="Google icon" />
                    התחבר עם חשבון Google אחר
                </button>
            </div>
        </div>
    </div>

    <style>
        .google-login-modal {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            z-index: 10000;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .google-login-modal-overlay {
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.6);
            backdrop-filter: blur(4px);
        }

        .google-login-modal-content {
            position: relative;
            background: var(--surface);
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            max-width: 450px;
            width: 90%;
            max-height: 90vh;
            overflow-y: auto;
            z-index: 10001;
            direction: rtl;
        }

        .google-login-modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 24px;
            border-bottom: 1px solid var(--border);
        }

        .google-login-modal-header h3 {
            margin: 0;
            font-size: 20px;
            font-weight: 700;
            color: var(--heading);
        }

        .google-login-modal-close {
            background: none;
            border: none;
            font-size: 28px;
            color: var(--text);
            cursor: pointer;
            padding: 0;
            width: 32px;
            height: 32px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 50%;
            transition: background 0.2s ease;
        }

        .google-login-modal-close:hover {
            background: var(--border);
        }

        .google-login-modal-body {
            padding: 24px;
        }

        .google-login-option {
            margin-bottom: 20px;
        }

        .google-login-user-info {
            display: flex;
            align-items: center;
            gap: 16px;
            padding: 16px;
            background: var(--bg);
            border-radius: 12px;
            margin-bottom: 16px;
        }

        .google-login-avatar {
            width: 48px;
            height: 48px;
            border-radius: 50%;
            background: var(--brand);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 20px;
            font-weight: 700;
            flex-shrink: 0;
        }

        .google-login-user-details {
            flex: 1;
        }

        .google-login-email {
            margin: 0 0 4px 0;
            font-weight: 600;
            color: var(--heading);
            font-size: 15px;
        }

        .google-login-hint {
            margin: 0;
            font-size: 13px;
            color: var(--text);
            opacity: 0.7;
        }

        .google-login-primary-btn {
            width: 100%;
            padding: 14px 24px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            cursor: pointer;
            transition: background 0.2s ease, transform 0.15s ease;
            box-shadow: 0 4px 12px rgba(229, 9, 20, 0.3);
        }

        .google-login-primary-btn:hover {
            background: var(--brand-dark);
            transform: translateY(-1px);
        }

        .google-login-primary-btn:active {
            transform: translateY(0);
        }

        .google-login-divider {
            margin: 20px 0;
            text-align: center;
            position: relative;
            color: var(--text);
            opacity: 0.5;
            font-size: 14px;
        }

        .google-login-divider::before,
        .google-login-divider::after {
            content: '';
            position: absolute;
            top: 50%;
            width: 40%;
            height: 1px;
            background: var(--border);
        }

        .google-login-divider::before {
            left: 0;
        }

        .google-login-divider::after {
            right: 0;
        }

        .google-login-secondary-btn {
            width: 100%;
            padding: 12px 24px;
            background: #fff;
            color: #4285F4;
            border: 2px solid #4285F4;
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            cursor: pointer;
            transition: background 0.2s ease, color 0.2s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
        }

        .google-login-secondary-btn:hover {
            background: #f8f9fa;
        }

        .google-login-secondary-btn img {
            width: 18px;
            height: 18px;
        }
    </style>

    <script type="text/javascript">
        function showGoogleLoginModal(event) {
            event.preventDefault();
            event.stopPropagation();
            
            var modal = document.getElementById('googleLoginModal');
            var storedEmail = localStorage.getItem('googleLoginEmail');
            
            if (storedEmail) {
                document.getElementById('googleLoginEmail').textContent = storedEmail;
                document.getElementById('continueAsEmailText').textContent = storedEmail;
                var firstLetter = storedEmail.charAt(0).toUpperCase();
                document.getElementById('googleLoginAvatarLetter').textContent = firstLetter;
                
                document.getElementById('continueAsSection').style.display = 'block';
                document.getElementById('googleLoginDivider').style.display = 'block';
            } else {
                document.getElementById('continueAsSection').style.display = 'none';
                document.getElementById('googleLoginDivider').style.display = 'none';
            }
            
            modal.style.display = 'flex';
        }

        function closeGoogleLoginModal() {
            document.getElementById('googleLoginModal').style.display = 'none';
        }

        function continueAsGoogleUser() {
            var storedEmail = localStorage.getItem('googleLoginEmail');
            if (!storedEmail) {
                switchGoogleAccount();
                return;
            }

            window.location.href = '<%= ResolveUrl("google-quick-login.aspx") %>?email=' + encodeURIComponent(storedEmail);
        }

        function switchGoogleAccount() {
            window.location.href = '<%= ResolveUrl("login.aspx") %>?action=google-login';
        }

        document.addEventListener('click', function(event) {
            var modal = document.getElementById('googleLoginModal');
            if (event.target === modal) {
                closeGoogleLoginModal();
            }
        });

        document.addEventListener('keydown', function(event) {
            if (event.key === 'Escape') {
                closeGoogleLoginModal();
            }
        });
    </script>
</asp:Content>

