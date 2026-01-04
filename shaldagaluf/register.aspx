<%@ Page Title="הרשמה" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="register.aspx.cs" Inherits="register" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="register-shell">
        <div class="register-container">
            <div class="register-header">
                <h2 class="register-title">הרשמה למערכת</h2>
                <p class="register-subtitle">צור חשבון חדש כדי להתחיל להשתמש ב-OptiSched</p>
            </div>

            <div class="register-form-container">
                <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">שם משתמש <span class="required">*</span></label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-input" placeholder="הזן שם משתמש"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">אימייל <span class="required">*</span></label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-input" placeholder="your.email@example.com"></asp:TextBox>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">שם פרטי <span class="required">*</span></label>
                        <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-input" placeholder="הזן שם פרטי"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">שם משפחה <span class="required">*</span></label>
                        <asp:TextBox ID="txtLastName" runat="server" CssClass="form-input" placeholder="הזן שם משפחה"></asp:TextBox>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">סיסמה <span class="required">*</span></label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="הזן סיסמה"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">אימות סיסמה <span class="required">*</span></label>
                        <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="הזן סיסמה שוב"></asp:TextBox>
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label class="form-label">טלפון <span class="required">*</span></label>
                        <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="form-input" placeholder="050-1234567"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label">תעודת זהות <span class="required">*</span></label>
                        <asp:TextBox ID="txtID" runat="server" CssClass="form-input" placeholder="הזן תעודת זהות"></asp:TextBox>
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
                            <div id="cityDropdownList" class="city-dropdown-list"></div>
                        </div>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">שנת לידה <span class="required">*</span></label>
                    <asp:TextBox ID="txtYearOfBirth" runat="server" CssClass="form-input" placeholder="1990" TextMode="Number"></asp:TextBox>
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnRegister" runat="server" Text="הירשם" OnClick="btnRegister_Click" CssClass="btn-register" />
                </div>

                <div class="register-footer">
                    <p>כבר יש לך חשבון? <a href="login.aspx">התחבר למערכת</a></p>
                </div>
            </div>
        </div>
    </section>

    <style>
        .register-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .register-container {
            max-width: 800px;
            margin: 0 auto;
        }

        .register-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .register-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .register-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .register-form-container {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
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
