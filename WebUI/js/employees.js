// ================== ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ==================
let employeesData = [];
let selectedEmployeeId = -1;

console.log("===== employees.js загружен =====");

// Функция для обновления таблицы из C#
window.updateEmployees = function(data) {
    console.log("updateEmployees вызван с данными:", data);
    
    if (data && data.employees) {
        employeesData = data.employees;
        displayEmployees(employeesData);
        updateStatistics();
    } else {
        console.error("Нет данных в updateEmployees");
    }
};

// ================== ОТОБРАЖЕНИЕ СОТРУДНИКОВ ==================
function displayEmployees(employees) {
    console.log("displayEmployees, количество:", employees.length);
    const tbody = document.getElementById('employeesTableBody');
    
    if (!tbody) {
        console.error("employeesTableBody не найден!");
        return;
    }
    
    if (!employees || employees.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 50px;">
                    <i class="fas fa-users" style="font-size: 40px; opacity: 0.3;"></i>
                    <p>Нет данных о сотрудниках</p>
                </td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = '';
    
    employees.forEach(emp => {
        const row = document.createElement('tr');
        row.onclick = () => selectEmployee(emp.id);
        row.id = `emp-${emp.id}`;
        
        // Определяем класс для роли
        let roleClass = 'role-operator';
        let roleText = 'Оператор';
        
        if (emp.role === 'admin') {
            roleClass = 'role-admin';
            roleText = 'Админ';
        } else if (emp.role === 'boss') {
            roleClass = 'role-boss';
            roleText = 'Руковод';
        } else if (emp.role === 'slesar') {
            roleClass = 'role-slesar';
            roleText = 'Слесарь';
        }
        
        row.innerHTML = `
            <td>${emp.id}</td>
            <td>${emp.familiya || ''}</td>
            <td>${emp.imya || ''}</td>
            <td>${emp.otchestvo || ''}</td>
            <td>${emp.dolzhnost || ''}</td>
            <td>${emp.telefon || ''}</td>
            <td>${emp.email || ''}</td>
            <td><span class="role-badge ${roleClass}">${roleText}</span></td>
        `;
        
        tbody.appendChild(row);
    });
}

// ================== ВЫБОР СОТРУДНИКА ==================
function selectEmployee(id) {
    console.log("Выбран сотрудник ID:", id);
    selectedEmployeeId = id;
    
    // Подсветка строки
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('selected');
    });
    
    const selectedRow = document.getElementById(`emp-${id}`);
    if (selectedRow) {
        selectedRow.classList.add('selected');
    }
    
    // Заполняем форму
    const emp = employeesData.find(e => e.id === id);
    if (emp) {
        document.getElementById('familiya').value = emp.familiya || '';
        document.getElementById('imya').value = emp.imya || '';
        document.getElementById('otchestvo').value = emp.otchestvo || '';
        document.getElementById('dolzhnost').value = emp.dolzhnost || '';
        document.getElementById('telefon').value = emp.telefon || '';
        document.getElementById('email').value = emp.email || '';
        document.getElementById('role').value = emp.role || 'operator';
    }
}

// ================== ПОИСК ==================
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

// ================== СТАТИСТИКА ==================
function updateStatistics() {
    const totalEl = document.getElementById('totalEmployees');
    if (totalEl) {
        totalEl.textContent = employeesData.length;
    }
}

// ================== ЗАГРУЗКА ПРИ СТАРТЕ ==================
document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM загружен, отправляем запрос в C#");
    
    // Отправляем сообщение в C# для загрузки данных
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'loadEmployees' 
        }));
    }
});