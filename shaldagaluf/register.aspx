<%@ Page Title="הרשמה" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="register.aspx.cs" Inherits="register" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="register-shell">
        <div class="register-bg" aria-hidden="true">
            <span class="register-glow register-glow-a"></span>
            <span class="register-glow register-glow-b"></span>
            <span class="register-grid"></span>
        </div>
        <div class="register-container">
            <div class="register-header">
                <span class="hero-eyebrow">OptiSched</span>
                <h2 class="register-title">הרשמה למערכת</h2>
                <p class="register-subtitle">צור חשבון חדש כדי להתחיל לתכנן משימות ולוחות זמנים</p>
            </div>

            <div class="register-form-container">
                <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">שם משתמש <span class="required">*</span></label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-input" placeholder="הזן שם משתמש" MaxLength="30"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvUsername" runat="server" ControlToValidate="txtUsername"
                            ErrorMessage="חובה להזין שם משתמש" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:RegularExpressionValidator ID="revUsername" runat="server" ControlToValidate="txtUsername"
                            ValidationExpression="^[A-Za-z0-9_\.\-]{3,30}$"
                            ErrorMessage="שם משתמש: 3–30 תווים באנגלית/ספרות/._-"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>

                    <div class="form-group">
                        <label class="form-label">אימייל <span class="required">*</span></label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-input" placeholder="your.email@example.com"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                            ErrorMessage="חובה להזין אימייל" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail"
                            ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                            ErrorMessage="כתובת אימייל לא תקינה"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">שם פרטי <span class="required">*</span></label>
                        <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-input" placeholder="הזן שם פרטי"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvFirstName" runat="server" ControlToValidate="txtFirstName"
                            ErrorMessage="חובה להזין שם פרטי" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>

                    <div class="form-group">
                        <label class="form-label">שם משפחה <span class="required">*</span></label>
                        <asp:TextBox ID="txtLastName" runat="server" CssClass="form-input" placeholder="הזן שם משפחה"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvLastName" runat="server" ControlToValidate="txtLastName"
                            ErrorMessage="חובה להזין שם משפחה" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">סיסמה <span class="required">*</span></label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="הזן סיסמה"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                            ErrorMessage="חובה להזין סיסמה" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:RegularExpressionValidator ID="revPassword" runat="server" ControlToValidate="txtPassword"
                            ValidationExpression="^.{6,}$"
                            ErrorMessage="הסיסמה חייבת להכיל לפחות 6 תווים"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>

                    <div class="form-group">
                        <label class="form-label">אימות סיסמה <span class="required">*</span></label>
                        <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="הזן סיסמה שוב"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvConfirmPassword" runat="server" ControlToValidate="txtConfirmPassword"
                            ErrorMessage="חובה לאמת סיסמה" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:CompareValidator ID="cvPassword" runat="server" ControlToValidate="txtConfirmPassword"
                            ControlToCompare="txtPassword" ErrorMessage="הסיסמאות אינן תואמות"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">טלפון <span class="required">*</span></label>
                        <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="form-input" placeholder="050-1234567"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvPhone" runat="server" ControlToValidate="txtPhone"
                            ErrorMessage="חובה להזין טלפון" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:RegularExpressionValidator ID="revPhone" runat="server" ControlToValidate="txtPhone"
                            ValidationExpression="^0(5\d|[2-489]|7[2-9])-?\d{7}$"
                            ErrorMessage="טלפון לא תקין (לדוגמה 0501234567)"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>

                    <div class="form-group">
                        <label class="form-label">תעודת זהות <span class="required">*</span></label>
                        <asp:TextBox ID="txtID" runat="server" CssClass="form-input" placeholder="הזן תעודת זהות" MaxLength="9"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfvID" runat="server" ControlToValidate="txtID"
                            ErrorMessage="חובה להזין תעודת זהות" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                        <asp:RegularExpressionValidator ID="revID" runat="server" ControlToValidate="txtID"
                            ValidationExpression="^\d{5,9}$"
                            ErrorMessage="תעודת זהות: 5–9 ספרות"
                            CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">מין <span class="required">*</span></label>
                        <div class="radio-button-wrapper">
                            <asp:RadioButtonList ID="rblGender" runat="server" CssClass="radio-button-list" RepeatDirection="Horizontal" RepeatLayout="Flow">
                                <asp:ListItem Text="זכר" Value="1" Selected="True"></asp:ListItem>
                                <asp:ListItem Text="נקבה" Value="2"></asp:ListItem>
                                <asp:ListItem Text="אחר" Value="3"></asp:ListItem>
                            </asp:RadioButtonList>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">עיר <span class="required">*</span></label>
                        <div class="searchable-dropdown-wrapper">
                            <div class="searchable-dropdown-container">
                                <input type="text" id="citySearchInput" class="form-input searchable-dropdown-input" placeholder="חפש ובחר עיר..." autocomplete="off" onkeyup="filterDropdownCities()" onfocus="showDropdown()" />
                                <button type="button" class="btn-clear-city" onclick="clearCitySelection()">נקה</button>
                            </div>
                            <asp:DropDownList ID="ddlOptions" runat="server" CssClass="form-input city-dropdown" style="display:none;">
                                <asp:ListItem Text="בחר עיר" Value="" />
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvCity" runat="server" ControlToValidate="ddlOptions"
                                InitialValue="" ErrorMessage="חובה לבחור עיר" CssClass="field-error" Display="Dynamic"
                                ValidationGroup="Register" />
                            <div id="cityDropdownList" class="city-dropdown-list"></div>
                        </div>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">שנת לידה <span class="required">*</span></label>
                    <asp:TextBox ID="txtYearOfBirth" runat="server" CssClass="form-input" placeholder="1990" TextMode="Number"></asp:TextBox>
                    <asp:RequiredFieldValidator ID="rfvYear" runat="server" ControlToValidate="txtYearOfBirth"
                        ErrorMessage="חובה להזין שנת לידה" CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                    <asp:RangeValidator ID="rvYear" runat="server" ControlToValidate="txtYearOfBirth"
                        Type="Integer" MinimumValue="1906" MaximumValue="2016"
                        ErrorMessage="שנת לידה לא בטווח תקין"
                        CssClass="field-error" Display="Dynamic" ValidationGroup="Register" />
                </div>

                <div class="form-actions">
                    <div class="form-field">
                        <asp:Button ID="btnGoogleRegister" runat="server" Text="הירשם עם Google" OnClick="btnGoogleRegister_Click" 
                            CssClass="google-button" CausesValidation="false" />
                    </div>

                    <div style="margin: 24px 0; text-align: center; position: relative;">
                        <div style="position: relative; text-align: center;">
                            <span style="background: var(--surface); padding: 0 16px; color: var(--text); font-size: 14px; position: relative; z-index: 1;">או</span>
                            <div style="position: absolute; top: 50%; left: 0; right: 0; height: 1px; background: var(--border); z-index: 0;"></div>
                        </div>
                    </div>

                    <asp:Button ID="btnRegister" runat="server" Text="הירשם" OnClick="btnRegister_Click"
                        CssClass="btn-register" ValidationGroup="Register" CausesValidation="true" />

                    <asp:Button ID="btnFillDemo" runat="server" Text="מלא דוגמה" OnClick="btnFillDemo_Click"
                        CssClass="btn-fill-demo" CausesValidation="false"
                        ToolTip="ממלא את הטופס במשתמש לדוגמה תקין (מתחלף בין 20 דוגמאות)" />
                </div>

                <div class="register-footer">
                    <p>כבר יש לך חשבון? <a href="login.aspx">התחבר למערכת</a></p>
                </div>
            </div>
        </div>
    </section>

    <style>
        .register-shell {
            position: relative;
            width: min(920px, 94%);
            margin: 28px auto 70px;
            padding: 28px 12px 40px;
            isolation: isolate;
        }

        .register-bg {
            position: absolute;
            inset: -48px -12% -20px;
            z-index: -1;
            pointer-events: none;
            overflow: hidden;
            border-radius: 36px;
        }

        .register-glow {
            position: absolute;
            border-radius: 50%;
            filter: blur(50px);
            opacity: 0.9;
        }

        .register-glow-a {
            width: 340px;
            height: 340px;
            top: -80px;
            left: -40px;
            background: rgba(229, 9, 20, 0.28);
        }

        .register-glow-b {
            width: 280px;
            height: 280px;
            right: -30px;
            bottom: 10%;
            background: rgba(229, 9, 20, 0.18);
        }

        .register-grid {
            position: absolute;
            inset: 0;
            opacity: 0.09;
            background-image:
                linear-gradient(rgba(255, 255, 255, 0.45) 1px, transparent 1px),
                linear-gradient(90deg, rgba(255, 255, 255, 0.45) 1px, transparent 1px);
            background-size: 48px 48px;
            mask-image: radial-gradient(ellipse 70% 60% at 50% 35%, #000 20%, transparent 75%);
        }

        .register-container {
            position: relative;
            max-width: 800px;
            margin: 0 auto;
        }

        .register-header {
            text-align: center;
            margin-bottom: 28px;
        }

        .register-header .hero-eyebrow {
            display: inline-block;
            margin-bottom: 12px;
            color: var(--brand);
            opacity: 1;
        }

        .register-title {
            font-size: clamp(28px, 4vw, 36px);
            font-weight: 700;
            color: var(--heading);
            margin: 0 0 10px;
        }

        .register-subtitle {
            font-size: 16px;
            color: var(--muted);
            margin: 0 auto;
            max-width: 36ch;
            line-height: 1.55;
        }

        .register-form-container {
            background:
                radial-gradient(circle at top right, rgba(229, 9, 20, 0.16), transparent 40%),
                linear-gradient(180deg, rgba(31, 33, 40, 0.95), var(--surface));
            border-radius: 24px;
            padding: clamp(28px, 4vw, 44px);
            box-shadow: 0 24px 60px rgba(0, 0, 0, 0.45);
            border: 1px solid rgba(255, 255, 255, 0.08);
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

        .form-row .form-group {
            margin-bottom: 0;
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
            transition: border-color .2s ease, box-shadow .2s ease;
            box-sizing: border-box;
        }

        .form-input:focus {
            outline: none;
            border-color: var(--brand);
            box-shadow: 0 0 0 3px rgba(229, 9, 20, 0.1);
        }

        select.form-input {
            cursor: pointer;
            appearance: none;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23ffffff' d='M6 9L1 4h10z'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: left 16px center;
            padding-right: 16px;
            padding-left: 40px;
        }

        .searchable-dropdown-wrapper {
            position: relative;
        }

        .searchable-dropdown-container {
            display: flex;
            gap: 8px;
        }

        .searchable-dropdown-input {
            flex: 1;
        }

        .btn-clear-city {
            padding: 12px 20px;
            background: var(--border);
            color: var(--text);
            border: 1px solid var(--border);
            border-radius: 8px;
            font-weight: 600;
            font-size: 14px;
            cursor: pointer;
            transition: background .2s ease, border-color .2s ease;
            white-space: nowrap;
        }

        .btn-clear-city:hover {
            background: var(--brand);
            border-color: var(--brand);
            color: #fff;
        }

        .city-dropdown {
            width: 100%;
        }

        .city-dropdown-list {
            display: none;
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            margin-top: 4px;
            background: var(--surface);
            border: 1px solid var(--border);
            border-radius: 8px;
            max-height: 300px;
            overflow-y: auto;
            z-index: 1000;
            box-shadow: var(--shadow-md);
        }

        .city-dropdown-list.show {
            display: block;
        }

        .city-dropdown-item {
            padding: 12px 16px;
            cursor: pointer;
            border-bottom: 1px solid var(--border);
            transition: background .2s ease;
            direction: rtl;
            text-align: right;
        }

        .city-dropdown-item:last-child {
            border-bottom: none;
        }

        .city-dropdown-item:hover {
            background: var(--brand);
            color: #fff;
        }

        .city-dropdown-item.selected {
            background: var(--brand);
            color: #fff;
        }

        .radio-button-wrapper {
            padding: 12px 0;
        }

        .radio-button-list {
            list-style: none;
            padding: 0;
            margin: 0;
            display: flex;
            gap: 24px;
            flex-direction: row-reverse;
            flex-wrap: wrap;
        }

        .radio-button-list li {
            display: flex;
            align-items: center;
            gap: 8px;
            margin: 0;
        }

        .radio-button-list input[type="radio"] {
            width: 20px;
            height: 20px;
            cursor: pointer;
            margin: 0;
            accent-color: var(--brand);
        }

        .radio-button-list label {
            margin: 0;
            cursor: pointer;
            font-weight: normal;
            font-size: 15px;
            color: var(--text);
            user-select: none;
        }

        .form-message {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-bottom: 24px;
            text-align: center;
            font-weight: 600;
            min-height: 24px;
        }

        .form-message.err {
            background: #fce8e6;
            color: #c5221f;
            border: 1px solid #f5c2c0;
        }

        .form-message.ok {
            background: #e6f4ea;
            color: #137333;
            border: 1px solid #b7e1c1;
        }

        .field-error {
            display: block;
            color: #c5221f;
            font-size: 13px;
            margin-top: 6px;
        }

        .form-actions {
            margin-top: 32px;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .btn-register {
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

        .btn-register:hover {
            background: var(--brand-dark);
            transform: translateY(-1px);
        }

        .btn-register:active {
            transform: translateY(1px);
        }

        .btn-fill-demo {
            width: 100%;
            margin-top: 12px;
            padding: 12px 28px;
            background: transparent;
            color: var(--text);
            border: 1px dashed var(--border);
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            cursor: pointer;
            transition: border-color .2s ease, color .2s ease, background .2s ease;
        }

        .btn-fill-demo:hover {
            border-color: var(--brand);
            color: var(--brand);
            background: rgba(229, 9, 20, 0.08);
        }

        .register-footer {
            margin-top: 24px;
            text-align: center;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .register-footer p {
            color: var(--text);
            opacity: 0.8;
            margin: 0;
        }

        .register-footer a {
            color: var(--brand);
            font-weight: 600;
            text-decoration: none;
        }

        .register-footer a:hover {
            text-decoration: underline;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .radio-button-list {
                flex-direction: column;
                align-items: flex-start;
            }
        }
    </style>

    <script type="text/javascript">
        var allCities = [];
        var selectedCityId = '';
        var selectedCityName = '';

        function loadAllCities() {
            var dropdown = document.getElementById('<%= ddlOptions.ClientID %>');
            allCities = [];
            
            for (var i = 0; i < dropdown.options.length; i++) {
                if (dropdown.options[i].value !== '') {
                    allCities.push({
                        id: dropdown.options[i].value,
                        name: dropdown.options[i].text
                    });
                }
            }
        }

        function filterDropdownCities() {
            var searchText = document.getElementById('citySearchInput').value.toLowerCase();
            var dropdownList = document.getElementById('cityDropdownList');
            var hiddenDropdown = document.getElementById('<%= ddlOptions.ClientID %>');
            
            if (!allCities || allCities.length === 0) {
                loadAllCities();
            }

            dropdownList.innerHTML = '';

            if (searchText === '') {
                showAllCities();
                return;
            }

            var filteredCities = [];
            for (var i = 0; i < allCities.length; i++) {
                var cityName = allCities[i].name.toLowerCase();
                if (cityName.indexOf(searchText) !== -1) {
                    filteredCities.push(allCities[i]);
                }
            }

            if (filteredCities.length === 0) {
                var noResult = document.createElement('div');
                noResult.className = 'city-dropdown-item';
                noResult.textContent = 'לא נמצאו תוצאות';
                noResult.style.cursor = 'default';
                dropdownList.appendChild(noResult);
            } else {
                for (var j = 0; j < filteredCities.length; j++) {
                    var item = document.createElement('div');
                    item.className = 'city-dropdown-item';
                    item.textContent = filteredCities[j].name;
                    item.onclick = function(city) {
                        return function() {
                            selectCity(city.id, city.name);
                        };
                    }(filteredCities[j]);
                    dropdownList.appendChild(item);
                }
            }

            showDropdown();
        }

        function showAllCities() {
            var dropdownList = document.getElementById('cityDropdownList');
            dropdownList.innerHTML = '';

            for (var i = 0; i < allCities.length; i++) {
                var item = document.createElement('div');
                item.className = 'city-dropdown-item';
                item.textContent = allCities[i].name;
                item.onclick = function(city) {
                    return function() {
                        selectCity(city.id, city.name);
                    };
                }(allCities[i]);
                dropdownList.appendChild(item);
            }
        }

        function showDropdown() {
            var dropdownList = document.getElementById('cityDropdownList');
            if (dropdownList.innerHTML === '') {
                showAllCities();
            }
            dropdownList.classList.add('show');
        }

        function hideDropdown() {
            var dropdownList = document.getElementById('cityDropdownList');
            dropdownList.classList.remove('show');
        }

        function selectCity(cityId, cityName) {
            selectedCityId = cityId;
            selectedCityName = cityName;
            
            document.getElementById('citySearchInput').value = cityName;
            var hiddenDropdown = document.getElementById('<%= ddlOptions.ClientID %>');
            hiddenDropdown.value = cityId;
            
            hideDropdown();
        }

        function clearCitySelection() {
            selectedCityId = '';
            selectedCityName = '';
            document.getElementById('citySearchInput').value = '';
            var hiddenDropdown = document.getElementById('<%= ddlOptions.ClientID %>');
            hiddenDropdown.selectedIndex = 0;
            hideDropdown();
        }

        document.addEventListener('click', function(event) {
            var wrapper = document.querySelector('.searchable-dropdown-wrapper');
            if (wrapper && !wrapper.contains(event.target)) {
                hideDropdown();
            }
        });

        window.onload = function() {
            loadAllCities();
        };
    </script>
</asp:Content>
