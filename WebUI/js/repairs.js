let currentEmployeeId = 0;
let currentUserLogin = '';
let currentUserRole = '';

document.addEventListener('DOMContentLoaded', function() {
    // Установка дат по умолчанию
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    
    const startDate = document.getElementById('startDate');
    const endDate = document.getElementById('endDate');
    if (startDate) startDate.value = monthAgo.toISOString().split('T')[0];
    if (endDate) endDate.value = today.toISOString().split('T')[0];
    
    // Настройка обработчиков
    setupEventListeners();
    setupTabs();
    setupModal();
    setupEquipmentSearch();
    
    // Загрузка данных
    loadTasks('', '', true);
    loadEquipment('');
});

function setCurrentUser(id, login, role) {
    currentEmployeeId = id;
    currentUserLogin = login;
    currentUserRole = role;
    document.getElementById('userName').innerText = 'Слесарь';
}

window.receiveFromCSharp = function(command, data) {
    console.log('Получено:', command, data);
    try {
        switch(command) {
            case 'displayTasks': displayTasks(data); break;
            case 'updateStatistics': updateStatistics(data); break;
            case 'displayEquipment': displayEquipment(data); break;
            case 'showSuccess': showToast('✅ ' + data, 'success'); break;
            case 'showError': showToast('❌ ' + data, 'error'); break;
            case 'showPassport': showPassportModal(data); break;
        }
    } catch(e) { console.error('Ошибка:', e); }
};

function loadTasks(startDate, endDate, showAll) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadTasks', startDate: startDate, endDate: endDate, showAll: showAll
        }));
    }
}

function loadEquipment(filter) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadEquipment', filter: filter || ''
        }));
    }
}

function loadPassport(id) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadPassport', id: id
        }));
    }
}

function updateStatistics(data) {
    let stats = typeof data === 'string' ? JSON.parse(data) : data;
    const html = `
        <div class="stat-card"><div class="stat-icon">📋</div><div class="stat-info"><div class="stat-value">${stats.total || 0}</div><div class="stat-label">Всего</div></div></div>
        <div class="stat-card warning"><div class="stat-icon">⚙️</div><div class="stat-info"><div class="stat-value">${stats.inProgress || 0}</div><div class="stat-label">В работе</div></div></div>
        <div class="stat-card success"><div class="stat-icon">✅</div><div class="stat-info"><div class="stat-value">${stats.completed || 0}</div><div class="stat-label">Выполнено</div></div></div>
    `;
    document.getElementById('statsGrid').innerHTML = html;
}

function displayTasks(data) {
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    const tbody = document.getElementById('tasksTableBody');
    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="loading">Нет заданий</td></tr>';
        return;
    }
    let html = '';
    items.forEach(t => {
        let statusClass = t.status === 'Завершен' ? 'status-completed' : (t.status === 'В работе' ? 'status-progress' : 'status-pending');
        let statusText = t.status === 'Завершен' ? 'Выполнено' : (t.status === 'В работе' ? 'В работе' : 'Ожидает');
        html += `<tr>
            <td>${t.id}</td>
            <td>${escapeHtml(t.equipment)}</td>
            <td>${escapeHtml(t.tip)}</td>
            <td>${t.start_date} ${t.end_date ? '— ' + t.end_date : ''}</td>
            <td><span class="status-badge ${statusClass}">${statusText}</span></td>
            <td>${t.status !== 'Завершен' ? `<button class="btn-complete" onclick="openCompleteModal(${t.id})">Выполнить</button>` : '✅'}</td>
        </tr>`;
    });
    tbody.innerHTML = html;
}

function displayEquipment(data) {
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    const grid = document.getElementById('equipmentGrid');
    if (!items || items.length === 0) {
        grid.innerHTML = '<div class="loading">Оборудование не найдено</div>';
        return;
    }
    let html = '';
    items.forEach(e => {
        let statusClass = e.status_name === 'В работе' ? 'status-working' : (e.status_name === 'В ремонте' ? 'status-repair' : 'status-conservation');
        html += `<div class="equipment-card" onclick="loadPassport(${e.id})">
            <div class="equipment-title">${escapeHtml(e.nazvanie)}</div>
            <div class="equipment-info">🏭 Тип: ${escapeHtml(e.tip || '-')}</div>
            <div class="equipment-info">📐 Модель: ${escapeHtml(e.model || '-')}</div>
            <div class="equipment-info">🔢 Зав. №: ${escapeHtml(e.seriionmer || '-')}</div>
            <div><span class="status-badge ${statusClass}">${escapeHtml(e.status_name)}</span></div>
        </div>`;
    });
    grid.innerHTML = html;
}

function showPassportModal(data) {
    let info = typeof data === 'string' ? JSON.parse(data) : data;
    document.getElementById('passportTitle').innerHTML = `📄 ${escapeHtml(info.title)}`;
    let c = info.content;
    document.getElementById('passportBody').innerHTML = `
        <div class="passport-section"><h4>Общие сведения</h4>
        <div class="passport-row"><div class="passport-label">Наименование</div><div class="passport-value">${escapeHtml(c.nazvanie)}</div></div>
        <div class="passport-row"><div class="passport-label">Тип</div><div class="passport-value">${escapeHtml(c.tip)}</div></div>
        <div class="passport-row"><div class="passport-label">Модель</div><div class="passport-value">${escapeHtml(c.model)}</div></div>
        <div class="passport-row"><div class="passport-label">Зав. номер</div><div class="passport-value">${escapeHtml(c.seriionmer)}</div></div>
        <div class="passport-row"><div class="passport-label">Место установки</div><div class="passport-value">${escapeHtml(c.mesto)}</div></div>
        </div>
        <div class="passport-section"><h4>Технические характеристики</h4>
        <div class="passport-row"><div class="passport-label">Мощность</div><div class="passport-value">${c.moshnost} МВт</div></div>
        <div class="passport-row"><div class="passport-label">Давление</div><div class="passport-value">${c.davlenie} МПа</div></div>
        <div class="passport-row"><div class="passport-label">Производитель</div><div class="passport-value">${escapeHtml(c.proizvoditel)}</div></div>
        <div class="passport-row"><div class="passport-label">Дата установки</div><div class="passport-value">${c.dataUstanovki}</div></div>
        <div class="passport-row"><div class="passport-label">Статус</div><div class="passport-value">${escapeHtml(c.statusName)}</div></div>
        </div>
    `;
    document.getElementById('passportModal').style.display = 'flex';
}

function openCompleteModal(taskId) {
    document.getElementById('taskId').value = taskId;
    document.getElementById('actualStartDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('completionDescription').value = '';
    document.getElementById('completeModal').style.display = 'flex';
}

function closeCompleteModal() {
    document.getElementById('completeModal').style.display = 'none';
}

function closePassportModal() {
    document.getElementById('passportModal').style.display = 'none';
}

function completeTask() {
    const taskId = document.getElementById('taskId').value;
    const description = document.getElementById('completionDescription').value.trim();
    const startDate = document.getElementById('actualStartDate').value;
    const endDate = document.getElementById('actualEndDate').value;
    
    if (!description) { showToast('Введите описание!', 'error'); return; }
    if (!startDate) { showToast('Введите дату начала!', 'error'); return; }
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'completeTask', id: parseInt(taskId), description: description,
            actualStartDate: startDate, actualEndDate: endDate
        }));
    }
    closeCompleteModal();
}

function showToast(message, type) {
    const toast = document.getElementById('toastMessage');
    toast.textContent = message;
    toast.className = `toast ${type}`;
    toast.style.display = 'block';
    setTimeout(() => { toast.style.display = 'none'; }, 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/[&<>]/g, function(m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}

function setupEventListeners() {
    const applyBtn = document.getElementById('applyFilterBtn');
    if (applyBtn) {
        applyBtn.onclick = () => {
            const start = document.getElementById('startDate')?.value || '';
            const end = document.getElementById('endDate')?.value || '';
            const showAll = document.getElementById('showAll')?.checked || false;
            loadTasks(start, end, showAll);
        };
    }
    const completeBtn = document.getElementById('completeTaskBtn');
    if (completeBtn) completeBtn.onclick = completeTask;
    const cancelModalBtn = document.getElementById('cancelModalBtn');
    if (cancelModalBtn) cancelModalBtn.onclick = closeCompleteModal;
    const closePassportBtn = document.getElementById('closePassportBtn');
    if (closePassportBtn) closePassportBtn.onclick = closePassportModal;
    document.querySelectorAll('.close').forEach(btn => {
        btn.onclick = function() {
            document.getElementById('completeModal').style.display = 'none';
            document.getElementById('passportModal').style.display = 'none';
        };
    });
}

function setupTabs() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.onclick = () => {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const tabName = btn.dataset.tab;
            document.getElementById('tasksTab').classList.toggle('active', tabName === 'tasks');
            document.getElementById('passportsTab').classList.toggle('active', tabName === 'passports');
            if (tabName === 'passports') loadEquipment('');
        };
    });
}

function setupModal() {
    window.onclick = (event) => {
        if (event.target.classList.contains('modal')) {
            event.target.style.display = 'none';
        }
    };
}

function setupEquipmentSearch() {
    const searchBtn = document.getElementById('searchEquipmentBtn');
    const searchInput = document.getElementById('equipmentSearch');
    if (searchBtn) {
        searchBtn.onclick = () => loadEquipment(searchInput?.value || '');
    }
    if (searchInput) {
        searchInput.onkeyup = (e) => { if (e.key === 'Enter') loadEquipment(searchInput.value); };
    }
}