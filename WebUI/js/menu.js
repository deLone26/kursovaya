let currentRole = '';
let currentDolzhnost = '';

// Функция, которую вызывает C# после загрузки страницы
window.setUserRole = function(role, dolzhnost) {
    currentRole = role;
    currentDolzhnost = dolzhnost || getDolzhnostByRole(role);
    
    // Отображение информации о пользователе
    const userInfo = document.getElementById('userInfo');
    if (userInfo) {
        userInfo.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap;">
                <div>
                    <strong>👤 ${currentDolzhnost}</strong>
                    <span style="margin: 0 10px">|</span>
                    <span>Роль: ${getRoleDisplayName(role)}</span>
                </div>
                <div style="font-size: 12px; color: #6c757d;">
                    ${new Date().toLocaleDateString('ru-RU')}
                </div>
            </div>
        `;
    }
    
    buildMenu();
};

// Получение должности по роли
function getDolzhnostByRole(role) {
    const roleMap = {
        'app_admin': 'Администратор системы',
        'app_boss': 'Начальник цеха',
        'app_slesar': 'Слесарь',
        'app_operator': 'Оператор'
    };
    return roleMap[role] || 'Сотрудник';
}

// Отображение роли
function getRoleDisplayName(role) {
    const roleMap = {
        'app_admin': 'Администратор',
        'app_boss': 'Начальник',
        'app_slesar': 'Слесарь',
        'app_operator': 'Оператор'
    };
    return roleMap[role] || role;
}

// Настройка меню для каждой роли (без Дашборда, Паспортов, Бюджета)
const menuConfig = {
    app_admin: [
        { id: 'users', title: '👥 Управление пользователями', desc: 'Создание и редактирование учетных записей', action: 'openUsers' },
        { id: 'employees', title: '👨‍🔧 Сотрудники', desc: 'Список и редактирование сотрудников', action: 'openEmployees' },
        { id: 'equipment', title: '🔧 Оборудование', desc: 'Характеристики и состояние оборудования', action: 'openEquipment' },
        { id: 'plans', title: '📋 Планы ТО', desc: 'Графики технического обслуживания', action: 'openPlansTO' },
        { id: 'accidents', title: '⚠️ Аварии', desc: 'Регистрация и контроль аварий', action: 'openAccidents' },
        { id: 'repairs', title: '🔨 Ремонты', desc: 'Учет ремонтных работ', action: 'openRepairs' }
    ],
    app_boss: [
        { id: 'employees', title: '👨‍🔧 Сотрудники', desc: 'Управление персоналом', action: 'openEmployees' },
        { id: 'equipment', title: '🔧 Оборудование', desc: 'Просмотр оборудования', action: 'openEquipment' },
        { id: 'plans', title: '📋 Планы ТО', desc: 'Планирование обслуживания', action: 'openPlansTO' },
        { id: 'reports', title: '📊 Отчеты', desc: 'Формирование отчетности', action: 'openReports' }
    ],
    app_operator: [
        { id: 'equipment', title: '🔧 Оборудование', desc: 'Просмотр оборудования', action: 'openEquipment' },
        { id: 'accidents', title: '⚠️ Аварии', desc: 'Просмотр аварий', action: 'openAccidents' }
    ],
    app_slesar: [
        { id: 'equipment', title: '🔧 Оборудование', desc: 'Паспорта оборудования', action: 'openEquipment' },
        { id: 'plans', title: '📋 Планы ТО', desc: 'Плановые работы', action: 'openPlansTO' },
        { id: 'accidents', title: '⚠️ Аварии', desc: 'Аварийные ситуации', action: 'openAccidents' },
        { id: 'repairs', title: '🔨 Ремонты', desc: 'Выполненные ремонты', action: 'openRepairs' }
    ]
};

// Построение меню
function buildMenu() {
    const container = document.getElementById('menuContainer');
    if (!container) return;
    
    container.innerHTML = '';
    
    const items = menuConfig[currentRole] || [];
    
    if (items.length === 0) {
        container.innerHTML = '<div class="loading">Нет доступных разделов</div>';
        return;
    }
    
    items.forEach(item => {
        const card = document.createElement('div');
        card.className = 'card';
        card.setAttribute('data-action', item.action);
        
        card.innerHTML = `
            <div class="card-header">
                <h3>${item.title}</h3>
                <p>${item.desc}</p>
            </div>
            <div class="card-footer">
                <span>➡ Перейти</span>
            </div>
        `;
        
        card.onclick = () => {
            // Отправляем сообщение в C#
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({ 
                    action: item.action,
                    id: item.id 
                }));
            }
        };
        
        container.appendChild(card);
    });
}

// Отправка уведомления о готовности
document.addEventListener('DOMContentLoaded', () => {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'pageReady'
        }));
    }
});