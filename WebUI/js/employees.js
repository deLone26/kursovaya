let employeesData = [];
let selectedEmployeeId = -1;
let currentUserRole = '';
let currentUserId = 0;

// Установка роли текущего пользователя (вызывается из C#)
window.setCurrentUserRole = function(role, userId) {
    console.log("setCurrentUserRole вызван с параметрами:", role, userId);
    currentUserRole = role;
    currentUserId = userId;
    
    const userInfoSpan = document.querySelector('.user-info span');
    if (userInfoSpan) {
        const roleNames = { admin: 'Администратор', boss: 'Начальник', slesar: 'Слесарь', operator: 'Оператор' };
        userInfoSpan.textContent = roleNames[role] || 'Сотрудник';
    }
    
    // Настройка интерфейса в зависимости от роли
    if (currentUserRole === 'boss') {
        console.log("Настройка интерфейса для начальника");
        
        // Блокируем поля, которые нельзя редактировать
        const readonlyFields = ['familiya', 'imya', 'otchestvo', 'login', 'role'];
        readonlyFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = true;
                field.style.backgroundColor = '#f1f5f9';
            }
        });
        
        // Поля, доступные для редактирования
        const editableFields = ['telefon', 'email'];
        editableFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = false;
                field.style.backgroundColor = 'white';
            }
        });
        
        // Скрываем кнопки, которые не должны быть доступны
        const addBtn = document.querySelector('.btn-add');
        const deleteBtn = document.querySelector('.btn-delete');
        const clearBtn = document.querySelector('.btn-clear');
        const updateBtn = document.querySelector('.btn-update');
        
        if (addBtn) addBtn.style.display = 'none';
        if (deleteBtn) deleteBtn.style.display = 'none';
        if (clearBtn) clearBtn.style.display = 'none';
        if (updateBtn) updateBtn.style.display = 'flex';
        
        selectedEmployeeId = -1;
        document.getElementById('selectedEmployeeId').value = '-1';
        
        showToast('Внимание: вы можете изменять только телефон и email сотрудников', 'warning');
    } else if (currentUserRole === 'admin') {
        console.log("Настройка интерфейса для администратора");
        
        const allFields = ['familiya', 'imya', 'otchestvo', 'telefon', 'email', 'login', 'role'];
        allFields.forEach(fieldId => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.disabled = false;
                field.style.backgroundColor = 'white';
            }
        });
        
        const addBtn = document.querySelector('.btn-add');
        const deleteBtn = document.querySelector('.btn-delete');
        const clearBtn = document.querySelector('.btn-clear');
        const updateBtn = document.querySelector('.btn-update');
        
        if (addBtn) addBtn.style.display = 'flex';
        if (deleteBtn) deleteBtn.style.display = 'flex';
        if (clearBtn) clearBtn.style.display = 'flex';
        if (updateBtn) updateBtn.style.display = 'flex';
    }
};

// Обновление таблицы из C#
window.updateEmployees = function(data) {
    console.log("updateEmployees получил данные:", data);
    if (data && data.employees) {
        employeesData = data.employees;
        displayEmployees(employeesData);
        updateStatistics();
    } else {
        console.error("Нет данных в updateEmployees");
    }
};

// Показ сообщений
window.showMessage = function(message, type) {
    showToast(message, type);
};

// Отображение сотрудников
function displayEmployees(employees) {
    console.log("displayEmployees вызван, количество:", employees ? employees.length : 0);
    const tbody = document.getElementById('employeesTableBody');
    
    if (!tbody) {
        console.error("tbody не найден");
        return;
    }
    
    if (!employees || employees.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="loading-row"><i class="fas fa-users"></i><p>Нет данных о сотрудниках</p></td></tr>`;
        return;
    }
    
    tbody.innerHTML = '';
    
    employees.forEach(emp => {
        const row = document.createElement('tr');
        row.id = `emp-${emp.id}`;
        row.onclick = () => selectEmployee(emp.id);
        row.style.cursor = 'pointer';
        
        let roleClass = 'role-operator';
        let roleText = 'Оператор';
        
        if (emp.role === 'admin') {
            roleClass = 'role-admin';
            roleText = 'Администратор';
        } else if (emp.role === 'boss') {
            roleClass = 'role-boss';
            roleText = 'Начальник котельной';
        } else if (emp.role === 'slesar') {
            roleClass = 'role-slesar';
            roleText = 'Слесарь';
        } else if (emp.role === 'operator') {
            roleClass = 'role-operator';
            roleText = 'Оператор';
        }
        
        row.innerHTML = `
            <td>${emp.id}</td>
            <td>${escapeHtml(emp.familiya || '')}</td>
            <td>${escapeHtml(emp.imya || '')}</td>
            <td>${escapeHtml(emp.otchestvo || '')}</td>
            <td>${escapeHtml(emp.telefon || '')}</td>
            <td>${escapeHtml(emp.email || '')}</td>
            <td><span class="role-badge ${roleClass}">${roleText}</span></td>
        `;
        
        tbody.appendChild(row);
    });
    
    console.log("Отображено сотрудников:", employees.length);
}

// Выбор сотрудника
function selectEmployee(id) {
    console.log("selectEmployee вызван, роль:", currentUserRole, "ID:", id);
    
    const emp = employeesData.find(e => e.id === id);
    if (!emp) {
        console.error("Сотрудник не найден, ID:", id);
        return;
    }
    
    // Заполняем форму
    document.getElementById('familiya').value = emp.familiya || '';
    document.getElementById('imya').value = emp.imya || '';
    document.getElementById('otchestvo').value = emp.otchestvo || '';
    document.getElementById('telefon').value = emp.telefon || '';
    document.getElementById('email').value = emp.email || '';
    document.getElementById('login').value = emp.login || '';
    document.getElementById('role').value = emp.role || 'operator';
    document.getElementById('password').value = '';
    
    if (currentUserRole === 'boss') {
        selectedEmployeeId = id;
        document.getElementById('selectedEmployeeId').value = id;
        showToast('Вы можете изменить только телефон и email', 'info');
    } else {
        selectedEmployeeId = id;
        document.getElementById('selectedEmployeeId').value = id;
        
        document.querySelectorAll('tbody tr').forEach(row => row.classList.remove('selected'));
        const selectedRow = document.getElementById(`emp-${id}`);
        if (selectedRow) selectedRow.classList.add('selected');
    }
}

// Поиск
function searchEmployees() {
    const searchText = document.getElementById('searchInput').value.toLowerCase();
    if (!searchText) {
        displayEmployees(employeesData);
        return;
    }
    
    const filtered = employeesData.filter(emp => 
        (emp.familiya && emp.familiya.toLowerCase().includes(searchText)) ||
        emp.id.toString().includes(searchText)
    );
    displayEmployees(filtered);
}

function loadEmployees() {
    console.log("loadEmployees вызван");
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEmployees' }));
    }
}

// Добавление (только для администратора)
function addEmployee() {
    if (currentUserRole === 'boss') {
        showToast('У вас нет прав на добавление сотрудников', 'warning');
        return;
    }
    
    const familiya = document.getElementById('familiya').value.trim();
    const imya = document.getElementById('imya').value.trim();
    const login = document.getElementById('login').value.trim();
    const password = document.getElementById('password').value;
    const role = document.getElementById('role').value;
    
    if (!familiya) {
        showToast('Введите фамилию!', 'warning');
        return;
    }
    if (!imya) {
        showToast('Введите имя!', 'warning');
        return;
    }
    if (!login) {
        showToast('Введите логин!', 'warning');
        return;
    }
    if (!password || password.length < 6) {
        showToast('Пароль должен быть не менее 6 символов!', 'warning');
        return;
    }
    
    if (window.chrome?.webview) {
        const employee = {
            familiya: familiya,
            imya: imya,
            otchestvo: document.getElementById('otchestvo').value,
            telefon: document.getElementById('telefon').value,
            email: document.getElementById('email').value,
            login: login,
            password: password,
            role: role
        };
        
        console.log("Отправляем добавление:", employee);
        window.chrome.webview.postMessage(JSON.stringify({ action: 'addEmployee', data: employee }));
    }
}

// Обновление
function updateEmployee() {
    console.log("updateEmployee вызван, роль:", currentUserRole, "selectedEmployeeId:", selectedEmployeeId);
    
    if (selectedEmployeeId === -1 || selectedEmployeeId === null) {
        showToast('Сначала выберите сотрудника из таблицы!', 'warning');
        return;
    }
    
    if (window.chrome?.webview) {
        const employee = {
            id: selectedEmployeeId,
            familiya: document.getElementById('familiya').value,
            imya: document.getElementById('imya').value,
            otchestvo: document.getElementById('otchestvo').value,
            telefon: document.getElementById('telefon').value,
            email: document.getElementById('email').value,
            login: document.getElementById('login').value,
            password: document.getElementById('password').value,
            role: document.getElementById('role').value
        };
        
        console.log("Отправляем обновление:", employee);
        window.chrome.webview.postMessage(JSON.stringify({ action: 'updateEmployee', data: employee }));
    }
}

// Удаление (только для администратора)
function deleteEmployee() {
    if (currentUserRole === 'boss') {
        showToast('У вас нет прав на удаление сотрудников', 'warning');
        return;
    }
    
    if (selectedEmployeeId === -1) {
        showToast('Выберите сотрудника!', 'warning');
        return;
    }
    
    if (confirm('Вы уверены, что хотите удалить этого сотрудника?')) {
        if (window.chrome?.webview) {
            console.log("Отправляем удаление ID:", selectedEmployeeId);
            window.chrome.webview.postMessage(JSON.stringify({ action: 'deleteEmployee', id: selectedEmployeeId }));
        }
    }
}

// Очистка формы
function clearForm() {
    if (currentUserRole === 'boss') {
        showToast('Очистка формы недоступна в режиме просмотра', 'warning');
        return;
    }
    
    selectedEmployeeId = -1;
    document.getElementById('selectedEmployeeId').value = '-1';
    document.getElementById('familiya').value = '';
    document.getElementById('imya').value = '';
    document.getElementById('otchestvo').value = '';
    document.getElementById('telefon').value = '';
    document.getElementById('email').value = '';
    document.getElementById('login').value = '';
    document.getElementById('password').value = '';
    document.getElementById('role').value = 'operator';
    
    document.querySelectorAll('tbody tr').forEach(row => row.classList.remove('selected'));
}

// Статистика
function updateStatistics() {
    const totalEl = document.getElementById('totalEmployees');
    const activeEl = document.getElementById('activeEmployees');
    if (totalEl) totalEl.textContent = employeesData.length;
    if (activeEl) activeEl.textContent = employeesData.length;
}

// Toast уведомления
function showToast(message, type) {
    const toast = document.getElementById('toast');
    if (!toast) return;
    
    toast.innerHTML = `<div class="toast-content">${escapeHtml(message)}</div>`;
    toast.className = `toast ${type}`;
    toast.style.display = 'block';
    
    setTimeout(() => {
        toast.style.display = 'none';
    }, 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/[&<>]/g, m => m === '&' ? '&amp;' : (m === '<' ? '&lt;' : '&gt;'));
}

// Загрузка при старте
document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM загружен, отправляем loadEmployees");
    loadEmployees();
});