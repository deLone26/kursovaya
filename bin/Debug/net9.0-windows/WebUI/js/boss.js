let allPlans = [], allAvariya = [], allHistory = [], currentEditPlanId = null;
let currentFilters = { equipmentFilter: '0', statusFilter: '', responsibleFilter: '', searchFilter: '', startDate: '', endDate: '' };
let selectedAccidentId = null, shownAccidentIds = new Set();
let currentBossId = 0, currentBossLogin = '', currentBossRole = '', currentBossFio = '';
let bossNotifications = [], bossUnreadCount = 0;
let allPlansOriginal = [];
let allAvariyaOriginal = [];
let allHistoryOriginal = [];
let allRepairHistoryOriginal = [];

let overdueNotified = false;
let expiringNotified = false;

function resetNotificationFlags() {
    overdueNotified = false;
    expiringNotified = false;
}

// ========== ФУНКЦИИ СОРТИРОВКИ ==========

let sortConfig = {
    plans: { column: 'id', direction: 'asc' },
    avariya: { column: 'id', direction: 'asc' },
    history: { column: 'id', direction: 'asc' }
};

function sortPlans(column) {
    if (sortConfig.plans.column === column) {
        sortConfig.plans.direction = sortConfig.plans.direction === 'asc' ? 'desc' : 'asc';
    } else {
        sortConfig.plans.column = column;
        sortConfig.plans.direction = 'asc';
    }
    
    const sorted = [...allPlans];
    sorted.sort((a, b) => {
        let valA, valB;
        switch(column) {
            case 'id': valA = a.id; valB = b.id; break;
            case 'equipment': valA = a.equipment.toLowerCase(); valB = b.equipment.toLowerCase(); break;
            case 'tip': valA = a.tip.toLowerCase(); valB = b.tip.toLowerCase(); break;
            case 'start_date': valA = a.start_date.split('.').reverse().join(''); valB = b.start_date.split('.').reverse().join(''); break;
            case 'end_date': valA = a.end_date.split('.').reverse().join(''); valB = b.end_date.split('.').reverse().join(''); break;
            case 'responsible': valA = a.responsible.toLowerCase(); valB = b.responsible.toLowerCase(); break;
            case 'status': valA = a.status.toLowerCase(); valB = b.status.toLowerCase(); break;
            default: valA = a.id; valB = b.id;
        }
        if (valA < valB) return sortConfig.plans.direction === 'asc' ? -1 : 1;
        if (valA > valB) return sortConfig.plans.direction === 'asc' ? 1 : -1;
        return 0;
    });
    allPlans = sorted;
    renderPlansTable();
}

function sortAvariya(column) {
    if (sortConfig.avariya.column === column) {
        sortConfig.avariya.direction = sortConfig.avariya.direction === 'asc' ? 'desc' : 'asc';
    } else {
        sortConfig.avariya.column = column;
        sortConfig.avariya.direction = 'asc';
    }
    
    const sorted = [...allAvariya];
    sorted.sort((a, b) => {
        let valA, valB;
        switch(column) {
            case 'id': valA = a.id; valB = b.id; break;
            case 'equipment': valA = a.equipment.toLowerCase(); valB = b.equipment.toLowerCase(); break;
            case 'date': valA = a.date; valB = b.date; break;
            case 'status': valA = a.status.toLowerCase(); valB = b.status.toLowerCase(); break;
            default: valA = a.id; valB = b.id;
        }
        if (valA < valB) return sortConfig.avariya.direction === 'asc' ? -1 : 1;
        if (valA > valB) return sortConfig.avariya.direction === 'asc' ? 1 : -1;
        return 0;
    });
    allAvariya = sorted;
    renderAvariyaTable();
}

function sortHistory(column) {
    if (sortConfig.history.column === column) {
        sortConfig.history.direction = sortConfig.history.direction === 'asc' ? 'desc' : 'asc';
    } else {
        sortConfig.history.column = column;
        sortConfig.history.direction = 'asc';
    }
    
    const sorted = [...allHistory];
    sorted.sort((a, b) => {
        let valA, valB;
        switch(column) {
            case 'id': valA = a.id; valB = b.id; break;
            case 'equipment_name': valA = a.equipment_name.toLowerCase(); valB = b.equipment_name.toLowerCase(); break;
            case 'accident_date': valA = a.accident_date; valB = b.accident_date; break;
            case 'responsible': valA = (a.responsible || '').toLowerCase(); valB = (b.responsible || '').toLowerCase(); break;
            case 'completion_date': valA = a.completion_date || ''; valB = b.completion_date || ''; break;
            default: valA = a.id; valB = b.id;
        }
        if (valA < valB) return sortConfig.history.direction === 'asc' ? -1 : 1;
        if (valA > valB) return sortConfig.history.direction === 'asc' ? 1 : -1;
        return 0;
    });
    allHistory = sorted;
    renderCompletedAvariyaTable(JSON.stringify(allHistory));
}

function showOnceOverdueNotification(plans) {
    if (!overdueNotified && plans && plans.length > 0) {
        overdueNotified = true;
        let planList = plans.map(p => `• ${p.equipment} - срок до ${p.end_date} (ID: ${p.id})`).join('\n');
        // Отправляем уведомление с planId первого просроченного плана
        if (plans.length > 0) {
            addBossNotification('❌ Просроченные задачи', `Следующие задачи просрочены:\n${planList}`, 'error', null, plans[0].id);
        }
    }
}

function showOnceExpiringNotification(plans) {
    if (!expiringNotified && plans && plans.length > 0) {
        expiringNotified = true;
        let planList = plans.map(p => `• ${p.equipment} - срок до ${p.end_date} (ID: ${p.id})`).join('\n');
        // Отправляем уведомление с planId первого истекающего плана
        if (plans.length > 0) {
            addBossNotification('⚠️ Истекает срок выполнения ТО', `Следующие задачи требуют внимания:\n${planList}`, 'error', null, plans[0].id);
        }
    }
}

function getDeclension(number, one, two, five) {
    let n = Math.abs(number) % 100;
    if (n >= 5 && n <= 20) return five;
    n = n % 10;
    if (n === 1) return one;
    if (n >= 2 && n <= 4) return two;
    return five;
}


// ========== ФУНКЦИИ ПОИСКА ==========

function searchPlans() {
    const searchText = document.getElementById('searchPlansInput').value.toLowerCase().trim();
    
    if (!searchText) {
        allPlans = [...allPlansOriginal];
    } else {
        allPlans = allPlansOriginal.filter(plan => {
            return plan.id.toString().includes(searchText) ||
                   plan.equipment.toLowerCase().includes(searchText) ||
                   (plan.tip && plan.tip.toLowerCase().includes(searchText)) ||
                   (plan.opisanie && plan.opisanie.toLowerCase().includes(searchText)) ||
                   (plan.responsible && plan.responsible.toLowerCase().includes(searchText)) ||
                   (plan.status && plan.status.toLowerCase().includes(searchText));
        });
    }
    sortPlans(sortConfig.plans.column);
}

function searchAvariya() {
    const searchText = document.getElementById('searchAvariyaInput').value.toLowerCase().trim();
    
    if (!searchText) {
        allAvariya = [...allAvariyaOriginal];
    } else {
        allAvariya = allAvariyaOriginal.filter(av => {
            return av.id.toString().includes(searchText) ||
                   av.equipment.toLowerCase().includes(searchText) ||
                   (av.description && av.description.toLowerCase().includes(searchText)) ||
                   (av.consequences && av.consequences.toLowerCase().includes(searchText)) ||
                   (av.status && av.status.toLowerCase().includes(searchText));
        });
    }
    sortAvariya(sortConfig.avariya.column);
}

function searchHistory() {
    const searchText = document.getElementById('searchHistoryInput').value.toLowerCase().trim();
    
    if (!searchText) {
        allHistory = [...allHistoryOriginal];
    } else {
        allHistory = allHistoryOriginal.filter(item => {
            return item.id.toString().includes(searchText) ||
                   (item.equipment_name && item.equipment_name.toLowerCase().includes(searchText)) ||
                   (item.description && item.description.toLowerCase().includes(searchText)) ||
                   (item.responsible && item.responsible.toLowerCase().includes(searchText)) ||
                   (item.spare_parts && item.spare_parts.toLowerCase().includes(searchText));
        });
    }
    sortHistory(sortConfig.history.column);
}

function searchRepairHistory() {
    const searchText = document.getElementById('searchRepairInput').value.toLowerCase().trim();
    
    if (!searchText) {
        renderRepairHistoryTable(JSON.stringify(allRepairHistoryOriginal));
        return;
    }
    
    const filtered = allRepairHistoryOriginal.filter(item => {
        return (item.equipment_name && item.equipment_name.toLowerCase().includes(searchText)) ||
               (item.tip_name && item.tip_name.toLowerCase().includes(searchText)) ||
               (item.sotrudnik_name && item.sotrudnik_name.toLowerCase().includes(searchText)) ||
               (item.opisanie && item.opisanie.toLowerCase().includes(searchText)) ||
               (item.zamennaya_detal && item.zamennaya_detal.toLowerCase().includes(searchText));
    });
    renderRepairHistoryTable(JSON.stringify(filtered));
}

// ========== ОСНОВНЫЕ ФУНКЦИИ ==========

function setCurrentUser(id, login, role, fullName) {
    currentBossId = id; currentBossLogin = login; currentBossRole = role; currentBossFio = fullName;
    const fioElement = document.getElementById('bossFio');
    if (fioElement) fioElement.innerText = fullName || 'Сотрудник';
}

function sendToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action: action, ...data });
    if (window.chrome && window.chrome.webview) window.chrome.webview.postMessage(msg);
}

function formatRelativeTime(date) {
    let now = new Date(), diff = Math.floor((now - date) / 1000 / 60);
    if (diff < 1) return "только что";
    if (diff < 60) return `${diff} мин назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч назад`;
    return `${Math.floor(diff / 1440)} дн назад`;
}

// ========== УВЕДОМЛЕНИЯ ==========

function addBossNotification(title, message, type = 'info', accidentId = null, planId = null) {
    if (accidentId && shownAccidentIds.has(accidentId)) return;
    let notification = {
        id: Date.now(), title: title, message: message, time: formatRelativeTime(new Date()),
        type: type, accidentId: accidentId, planId: planId, isRead: false
    };
    if (accidentId) shownAccidentIds.add(accidentId);
    bossNotifications.unshift(notification);
    bossUnreadCount++;
    updateBossNotificationUI();
    showToast(`${title}: ${message}`, type === 'error' ? 'error' : 'info');
}

function updateBossNotificationUI() {
    let countEl = document.getElementById('bossNotifBadge');
    if (countEl) { countEl.innerText = bossUnreadCount; countEl.style.display = bossUnreadCount > 0 ? 'flex' : 'none'; }
    let container = document.getElementById('bossNotifList');
    if (!container) return;
    if (bossNotifications.length === 0) { container.innerHTML = '<div class="notif-item">Нет уведомлений</div>'; return; }
    let html = '';
    for (let n of bossNotifications) {
        let unreadClass = !n.isRead ? 'unread' : '', criticalClass = n.type === 'error' ? 'critical' : '';
        let icon = n.type === 'error' ? '⚠️' : (n.type === 'success' ? '✅' : '🔔');
        html += `<div class="notif-item ${unreadClass} ${criticalClass}" data-accident-id="${n.accidentId || ''}" data-plan-id="${n.planId || ''}" onclick="onBossNotificationClick(this)">
            <div class="notif-title ${criticalClass}">${icon} ${escapeHtml(n.title)}</div>
            <div class="notif-text">${escapeHtml(n.message)}</div><div class="notif-time">${n.time}</div></div>`;
    }
    html += `<div class="notification-footer"><a onclick="clearAllNotifications()">Очистить все</a></div>`;
    container.innerHTML = html;
}

function onBossNotificationClick(element) {
    let accidentId = element.getAttribute('data-accident-id'), planId = element.getAttribute('data-plan-id');
    
    if (accidentId && accidentId !== '') {
        // Переключаемся на вкладку аварий
        document.querySelectorAll('.tab-btn').forEach(btn => { 
            if (btn.dataset.tab === 'avariya') btn.click(); 
        });
        setTimeout(() => highlightAccidentById(parseInt(accidentId)), 300);
    } else if (planId && planId !== '') {
        // Переключаемся на вкладку планов
        document.querySelectorAll('.tab-btn').forEach(btn => { 
            if (btn.dataset.tab === 'plans') btn.click(); 
        });
        setTimeout(() => highlightPlanById(parseInt(planId)), 300);
    }
    document.getElementById('bossNotifDropdown').style.display = 'none';
}

function highlightAccidentById(accidentId) {
    let rows = document.querySelectorAll('#avariyaTableBody tr');
    for (let row of rows) {
        let firstCell = row.cells[0];
        if (firstCell && parseInt(firstCell.innerText) === accidentId) {
            rows.forEach(r => r.classList.remove('selected'));
            row.classList.add('selected');
            // Прокрутка к выделенной строке
            row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            // Эффект мигания для привлечения внимания
            let originalBg = row.style.backgroundColor;
            row.style.transition = 'background 0.3s';
            row.style.backgroundColor = '#fef3c7';
            setTimeout(() => {
                row.style.backgroundColor = '';
            }, 1500);
            break;
        }
    }
}

function highlightPlanById(planId) {
    let rows = document.querySelectorAll('#plansTableBody tr');
    for (let row of rows) {
        let firstCell = row.cells[0];
        if (firstCell && parseInt(firstCell.innerText) === planId) {
            rows.forEach(r => r.classList.remove('selected'));
            row.classList.add('selected');
            // Прокрутка к выделенной строке
            row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            // Эффект мигания для привлечения внимания
            row.style.transition = 'background 0.3s';
            row.style.backgroundColor = '#fef3c7';
            setTimeout(() => {
                row.style.backgroundColor = '';
            }, 1500);
            break;
        }
    }
}

function clearAllNotifications() { bossNotifications = []; bossUnreadCount = 0; updateBossNotificationUI(); }
function toggleBossNotifications() { let p = document.getElementById('bossNotifDropdown'); if (p) p.style.display = p.style.display === 'block' ? 'none' : 'block'; }

document.addEventListener('click', function(e) { 
    let p = document.getElementById('bossNotifDropdown'), b = document.getElementById('bossBellBtn'); 
    if (p && p.style.display === 'block') { 
        if (b && b.contains(e.target)) return; 
        if (!p.contains(e.target)) p.style.display = 'none'; 
    } 
});

// ========== ОБРАБОТКА ДАННЫХ ИЗ C# ==========

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command);
    if (command === 'fillEquipment') { fillSelect('equipmentFilter', data, true); fillSelect('planEquipment', data, false); }
    else if (command === 'fillTipTypesForPlan') fillSelect('createPlanTip', data, false);
    else if (command === 'fillResponsibleForPlan') fillSelect('createPlanResponsible', data, false);
    else if (command === 'fillTipTypes') fillSelect('planTip', data, false);
    else if (command === 'fillResponsible') { fillSelect('planResponsible', data, false); fillSelect('responsibleFilter', data, true); }
    else if (command === 'displayPlans') {
    const result = typeof data === 'string' ? JSON.parse(data) : data;
    allPlansOriginal = result.plans || [];
    allPlans = [...allPlansOriginal];
    renderPlansTable();
}
    else if (command === 'displayAvariya') {
    allAvariyaOriginal = typeof data === 'string' ? JSON.parse(data) : data;
    allAvariya = [...allAvariyaOriginal];
    renderAvariyaTable();
}
    else if (command === 'displayCompletedAvariya') {
        allHistoryOriginal = typeof data === 'string' ? JSON.parse(data) : data;
        allHistory = [...allHistoryOriginal];
        renderCompletedAvariyaTable(data);
    }
else if (command === 'showOnceOverdue') { 
    const plans = typeof data === 'string' ? JSON.parse(data) : data;
    showOnceOverdueNotification(plans);
}
else if (command === 'showOnceExpiring') { 
    const plans = typeof data === 'string' ? JSON.parse(data) : data;
    showOnceExpiringNotification(plans);
}
else if (command === 'displayStatisticsWithDates') {
    displayStatisticsWithDates(data);
}
    else if (command === 'displayRepairHistory') {
    const items = typeof data === 'string' ? JSON.parse(data) : data;
    renderRepairHistoryTable(JSON.stringify(items));
}
    else if (command === 'updateStatistics') { 
        const s = typeof data === 'string' ? JSON.parse(data) : data;
        document.getElementById('statEquipment').innerHTML = s.totalEquipment || 0;
        document.getElementById('statActiveAvariya').innerHTML = s.activeAvariya || 0;
        document.getElementById('statCompletedAvariya').innerHTML = s.completedAvariya || 0;
        document.getElementById('statPlans').innerHTML = s.totalPlans || 0;
        document.getElementById('statCompleted').innerHTML = s.completedPlans || 0;
        document.getElementById('statOverdue').innerHTML = s.overduePlans || 0; 
    }
    else if (command === 'showNewAvariya') { 
        const av = typeof data === 'string' ? JSON.parse(data) : data; 
        addBossNotification('Новая авария', `Оборудование: ${av.equipment}\nДата: ${av.date}`, 'error', av.id); 
        loadAvariya(); 
        loadStatistics(); 
        if (Notification.permission === 'granted') new Notification('⚠️ Новая авария', { body: `Оборудование: ${av.equipment}\nДата: ${av.date}\nОписание: ${av.description}` }); 
    }
    else if (command === 'showPlanCompleted') { 
        const p = typeof data === 'string' ? JSON.parse(data) : data; 
        addBossNotification('✅ План выполнен', `Оборудование: ${p.equipment}\nСлесарь: ${p.responsible}`, 'success', null, p.id); 
        loadPlans(); 
        loadStatistics(); 
        loadRepairHistory(); 
    }
    else if (command === 'showOverdueWarning') { 
        const p = typeof data === 'string' ? JSON.parse(data) : data; 
        addBossNotification('⚠️ Истекает срок', `Оборудование: ${p.equipment}\nДата окончания: ${p.end_date}`, 'error', null, p.id); 
    }
    else if (command === 'showPlanOverdue') { 
        const p = typeof data === 'string' ? JSON.parse(data) : data; 
        addBossNotification('❌ План просрочен', `Оборудование: ${p.equipment}\nДата окончания: ${p.end_date}`, 'error', null, p.id); 
        loadPlans(); 
        loadStatistics(); 
    }
    else if (command === 'showSuccess') showToast(data, 'success');
    else if (command === 'showError') showToast(data, 'error');
};

function fillSelect(id, data, addAll) { 
    let select = document.getElementById(id); 
    if (!select) return; 
    let items = typeof data === 'string' ? JSON.parse(data) : data; 
    let html = ''; 
    if (addAll) html += '<option value="">Все</option>'; 
    items.forEach(item => { 
        if (item.id !== 0) html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`; 
    }); 
    select.innerHTML = html; 
}

// ========== ОТОБРАЖЕНИЕ ТАБЛИЦ ==========

function renderPlansTable() {
    let tbody = document.getElementById('plansTableBody');
    if (!allPlans.length) { 
        tbody.innerHTML = '<tr><td colspan="10" class="loading">Нет данных</td</td>'; 
        return; 
    }
    
    let html = '';
    for (let plan of allPlans) {
        let statusClass = '';
        let statusText = plan.status || '';
        
        if (statusText === 'Просрочен') statusClass = 'status-overdue';
        else if (statusText === 'Завершен') statusClass = 'status-completed';
        else if (statusText === 'В работе') statusClass = 'status-progress';
        else if (statusText === 'Отправлено в работу') statusClass = 'status-sent';
        else statusClass = 'status-sent';
        
        let opisanie = plan.opisanie || '-';
        if (opisanie.length > 50) opisanie = opisanie.substring(0, 47) + '...';
        
        html += `<tr data-id="${plan.id}" onclick="selectPlanRow(this, ${plan.id})">
            <td style="width:50px">${plan.id}</td>
            <td style="width:160px">${escapeHtml(plan.equipment)}</td>
            <td style="width:120px">${escapeHtml(plan.tip)}</td>
            <td style="width:100px">${plan.start_date || '-'}</td>
            <td style="width:100px">${plan.end_date || '-'}</td>
            <td style="width:150px">${escapeHtml(plan.responsible)}</td>
            <td style="width:200px" title="${escapeHtml(plan.opisanie || '')}">${escapeHtml(opisanie)}</td>
            <td style="width:110px"><span class="status-badge ${statusClass}">${escapeHtml(statusText)}</span></td>
            <td style="width:60px">${plan.has_avariya === 'Да' ? '✅' : '❌'}</td>
            <td style="width:100px">
                <button class="edit-btn" onclick="event.stopPropagation(); editPlan(${plan.id})">✏️</button>
                <button class="delete-btn" onclick="event.stopPropagation(); deletePlan(${plan.id})">🗑️</button>
            </td>
        <tr>`;
    }
    tbody.innerHTML = html;
}

function renderAvariyaTable() {
    let tbody = document.getElementById('avariyaTableBody');
    if (!allAvariya.length) { 
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет активных заявок</td</tr>'; 
        return; 
    }
    
    let html = '';
    for (let av of allAvariya) {
        // Определяем класс статуса
        let statusClass = '';
        let statusText = av.status || 'Зарегистрирована';
        if (statusText === 'Завершена') statusClass = 'status-completed';
        else if (statusText === 'В работе') statusClass = 'status-progress';
        else statusClass = 'status-sent';
        
        // Обрезаем длинное описание
        let description = av.description || '-';
        if (description.length > 60) description = description.substring(0, 57) + '...';
        
        let consequences = av.consequences || '-';
        if (consequences.length > 50) consequences = consequences.substring(0, 47) + '...';
        
        html += `<tr data-id="${av.id}" onclick="selectAccidentRow(this, ${av.id})">
            <td style="width:55px">${av.id}</td>
            <td style="width:170px">${escapeHtml(av.equipment)}</td>
            <td style="width:130px">${av.date || '-'}</td>
            <td style="width:220px" title="${escapeHtml(av.description || '')}">${escapeHtml(description)}</td>
            <td style="width:160px" title="${escapeHtml(av.consequences || '')}">${escapeHtml(consequences)}</td>
            <td style="width:110px"><span class="status-badge ${statusClass}">${escapeHtml(statusText)}</span></td>
            <td style="width:130px"><button class="create-plan-btn" onclick="event.stopPropagation(); openCreatePlanModal(${av.id})">📋 Запланировать</button></td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

function renderCompletedAvariyaTable(data) {
    let tbody = document.getElementById('historyTableBody');
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    
    if (!items || !items.length) {
        tbody.innerHTML = '<tr><td colspan="8" class="loading">Нет данных</td</tr>';
        return;
    }
    
    let html = '';
    for (let h of items) {
        let deadlineClass = h.deadline_status === 'Просрочена' ? 'status-overdue' : 'status-on-time';
        let deadlineText = h.deadline_status === 'Просрочена' ? '⚠️ Просрочена' : '✅ В срок';
        
        html += `<tr data-id="${h.id || ''}">
            <td style="width:55px">${h.id || '-'}</td>
            <td style="width:160px">${escapeHtml(h.equipment_name || '-')}</td>
            <td style="width:110px">${h.accident_date || '-'}</td>
            <td style="width:190px">${escapeHtml(h.description || '-')}</td>
            <td style="width:140px">${escapeHtml(h.responsible || '-')}</td>
            <td style="width:150px">${escapeHtml(h.spare_parts || '-')}</td>
            <td style="width:110px">${h.completion_date || '-'}</td>
            <td style="width:100px"><span class="status-badge ${deadlineClass}">${deadlineText}</span></td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

function renderRepairHistoryTable(data) {
    let tbody = document.getElementById('repairHistoryTableBody');
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    
    if (!items || !items.length) {
        tbody.innerHTML = '<tr><td colspan="8" class="loading">Нет данных</td</tr>';
        return;
    }
    
    let html = '';
    for (let h of items) {
        let deadlineClass = h.deadline_status === 'Просрочена' ? 'status-overdue' : 'status-on-time';
        let deadlineText = h.deadline_status === 'Просрочена' ? '⚠️ Просрочена' : '✅ В срок';
        
        html += `<tr>
            <td style="width:170px">${escapeHtml(h.equipment_name || '-')}</td>
            <td style="width:110px">${escapeHtml(h.tip_name || '-')}</td>
            <td style="width:100px">${h.plan_date || '-'}</td>
            <td style="width:100px">${h.completed_date || '-'}</td>
            <td style="width:150px">${escapeHtml(h.sotrudnik_name || '-')}</td>
            <td style="width:190px">${escapeHtml(h.opisanie || '-')}</td>
            <td style="width:140px">${escapeHtml(h.zamennaya_detal || '-')}</td>
            <td style="width:100px"><span class="status-badge ${deadlineClass}">${deadlineText}</span></td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

// ========== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ==========

function selectPlanRow(e, id) { 
    document.querySelectorAll('#plansTableBody tr').forEach(r => r.classList.remove('selected')); 
    e.classList.add('selected'); 
}
function selectAccidentRow(e, id) { 
    document.querySelectorAll('#avariyaTableBody tr').forEach(r => r.classList.remove('selected')); 
    e.classList.add('selected'); 
    selectedAccidentId = id; 
}
function loadPlans() { sendToCSharp('loadPlans', currentFilters); }
function loadAvariya() { 
    sendToCSharp('loadAvariya', { 
        startDate: document.getElementById('avariyaStartDate').value, 
        endDate: document.getElementById('avariyaEndDate').value 
    }); 
}
function loadCompletedAvariya() { 
    sendToCSharp('loadCompletedAvariya', { 
        startDate: document.getElementById('historyStartDate').value, 
        endDate: document.getElementById('historyEndDate').value 
    }); 
}
function loadRepairHistory() { 
    sendToCSharp('loadRepairHistory', { 
        startDate: document.getElementById('repairHistoryStartDate').value, 
        endDate: document.getElementById('repairHistoryEndDate').value 
    }); 
}
function loadStatistics() { sendToCSharp('loadStatistics'); }
function applyFilters() { 
    currentFilters = { 
        equipmentFilter: document.getElementById('equipmentFilter').value, 
        statusFilter: document.getElementById('statusFilter').value, 
        responsibleFilter: document.getElementById('responsibleFilter').value, 
        searchFilter: document.getElementById('searchFilter').value, 
        startDate: document.getElementById('startDateFilter').value, 
        endDate: document.getElementById('endDateFilter').value 
    }; 
    loadPlans(); 
}

function addPlan() { 
    let d = document.getElementById('planDescription').value; 
    sendToCSharp('addPlan', { 
        equipment: parseInt(document.getElementById('planEquipment').value), 
        tip: parseInt(document.getElementById('planTip').value), 
        startDate: document.getElementById('planStartDate').value, 
        endDate: document.getElementById('planEndDate').value, 
        responsible: parseInt(document.getElementById('planResponsible').value), 
        opisanie: d || '' 
    }); 
    closeModal('planModal'); 
}
function editPlan(id) { 
    let plan = allPlans.find(p => p.id === id); 
    if (!plan) return; 
    currentEditPlanId = id; 
    document.getElementById('planModalTitle').innerText = 'Редактирование плана'; 
    document.getElementById('planEquipment').value = plan.equipment_id; 
    document.getElementById('planTip').value = plan.tip_id; 
    document.getElementById('planStartDate').value = plan.start_date.split('.').reverse().join('-'); 
    document.getElementById('planEndDate').value = plan.end_date.split('.').reverse().join('-'); 
    document.getElementById('planResponsible').value = plan.responsible_id; 
    document.getElementById('planDescription').value = plan.opisanie || ''; 
    document.getElementById('planModal').style.display = 'flex'; 
}
function updatePlan() { 
    let d = document.getElementById('planDescription').value; 
    sendToCSharp('updatePlan', { 
        id: currentEditPlanId, 
        equipment: parseInt(document.getElementById('planEquipment').value), 
        tip: parseInt(document.getElementById('planTip').value), 
        startDate: document.getElementById('planStartDate').value, 
        endDate: document.getElementById('planEndDate').value, 
        responsible: parseInt(document.getElementById('planResponsible').value), 
        status: document.getElementById('planStatus').value, 
        opisanie: d || '' 
    }); 
    closeModal('planModal'); 
}
function deletePlan(id) { 
    if (confirm('Удалить план?')) sendToCSharp('deletePlan', { id: id }); 
}
function openCreatePlanModal(accidentId) { 
    let accident = allAvariya.find(a => a.id === accidentId); 
    if (!accident) return; 
    document.getElementById('createPlanAvariyaId').value = accidentId; 
    document.getElementById('createPlanEquipment').value = accident.equipment; 
    document.getElementById('createPlanDescription').value = accident.description; 
    let today = new Date().toISOString().split('T')[0]; 
    document.getElementById('createPlanStartDate').value = today; 
    document.getElementById('createPlanEndDate').value = today; 
    document.getElementById('createPlanOpisanie').value = ''; 
    let tipSelect = document.getElementById('createPlanTip'); 
    for (let i = 0; i < tipSelect.options.length; i++) {
        if (tipSelect.options[i].text.includes('Аварийный')) { 
            tipSelect.selectedIndex = i; 
            break; 
        } 
    } 
    document.getElementById('createPlanModal').style.display = 'flex'; 
}
function exportToExcel() { sendToCSharp('exportToExcel', { reportType: document.getElementById('reportTypeSelect').value }); }
function exportToWord() { sendToCSharp('exportToWord', { reportType: document.getElementById('reportTypeSelect').value }); }
function previewReport() { sendToCSharp('previewReport'); }
function setDefaultDates() { 
    let today = new Date(), monthAgo = new Date(); 
    monthAgo.setMonth(monthAgo.getMonth() - 1); 
    let f = d => d.toISOString().split('T')[0]; 
    document.getElementById('startDateFilter').value = f(monthAgo); 
    document.getElementById('endDateFilter').value = f(today); 
    let threeMonthsAgo = new Date();
    threeMonthsAgo.setMonth(threeMonthsAgo.getMonth() - 3);
    document.getElementById('avariyaStartDate').value = f(threeMonthsAgo); 
    document.getElementById('avariyaEndDate').value = f(today); 
    document.getElementById('historyStartDate').value = f(monthAgo); 
    document.getElementById('historyEndDate').value = f(today); 
    document.getElementById('repairHistoryStartDate').value = f(monthAgo); 
    document.getElementById('repairHistoryEndDate').value = f(today); 
    document.getElementById('statStartDate').value = f(monthAgo);
document.getElementById('statEndDate').value = f(today);
}
function setupTabs() { 
    document.querySelectorAll('.tab-btn').forEach(btn => { 
        btn.onclick = () => { 
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active')); 
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active')); 
            btn.classList.add('active'); 
            let tab = document.getElementById(`${btn.dataset.tab}Tab`); 
            if (tab) tab.classList.add('active'); 
            if (btn.dataset.tab === 'plans') { loadPlans(); resetNotificationFlags(); }
            if (btn.dataset.tab === 'avariya') loadAvariya(); 
            if (btn.dataset.tab === 'history') loadCompletedAvariya(); 
            if (btn.dataset.tab === 'repairHistory') loadRepairHistory(); 
            if (btn.dataset.tab === 'statistics') loadStatisticsWithDates(); 
        }; 
    }); 
}

function applyPlansFilters() {
    const startDate = document.getElementById('startDateFilter').value;
    const endDate = document.getElementById('endDateFilter').value;
    const searchText = document.getElementById('searchPlansInput').value;
    
    currentFilters = { startDate: startDate, endDate: endDate, searchText: searchText };
    sendToCSharp('loadPlans', currentFilters);
}

function setupEventListeners() {
    document.getElementById('applyFilterBtn').onclick = () => applyPlansFilters();
    document.getElementById('applyAvariyaFilterBtn').onclick = () => loadAvariya();
    document.getElementById('applyHistoryFilterBtn').onclick = () => loadCompletedAvariya();
    document.getElementById('applyRepairHistoryFilterBtn').onclick = () => loadRepairHistory();
    document.getElementById('addPlanBtn').onclick = () => { 
        currentEditPlanId = null; 
        document.getElementById('planModalTitle').innerText = 'Добавление плана'; 
        document.getElementById('planEquipment').value = ''; 
        document.getElementById('planTip').value = ''; 
        document.getElementById('planStartDate').value = ''; 
        document.getElementById('planEndDate').value = ''; 
        document.getElementById('planResponsible').value = ''; 
        document.getElementById('planDescription').value = ''; 
        document.getElementById('planModal').style.display = 'flex'; 
    };
    document.getElementById('savePlanBtn').onclick = () => { 
        if (currentEditPlanId) updatePlan(); 
        else addPlan(); 
    };
    document.getElementById('exportExcelBtn').onclick = () => exportToExcel();
    document.getElementById('exportWordBtn').onclick = () => exportToWord();
    document.getElementById('previewReportBtn').onclick = () => previewReport();
    document.getElementById('bossBellBtn').onclick = () => toggleBossNotifications();
    document.getElementById('confirmCreatePlanBtn').onclick = () => { 
        let a = document.getElementById('createPlanAvariyaId').value; 
        sendToCSharp('createPlanFromAvariya', { 
            id: parseInt(a), 
            tipId: parseInt(document.getElementById('createPlanTip').value), 
            startDate: document.getElementById('createPlanStartDate').value, 
            endDate: document.getElementById('createPlanEndDate').value, 
            responsibleId: parseInt(document.getElementById('createPlanResponsible').value), 
            opisanie: document.getElementById('createPlanOpisanie').value 
        }); 
        closeModal('createPlanModal'); 
    };
    
    // ========== НАСТРОЙКА ПОИСКА (с задержкой 300 мс) ==========
    const searchPlansInput = document.getElementById('searchPlansInput');
    if (searchPlansInput) {
        searchPlansInput.addEventListener('input', function() {
            clearTimeout(window.searchPlansTimeout);
            window.searchPlansTimeout = setTimeout(() => searchPlans(), 300);
        });
    }
    const applyStatFilterBtn = document.getElementById('applyStatFilterBtn');
if (applyStatFilterBtn) {
    applyStatFilterBtn.onclick = () => loadStatisticsWithDates();
}
    const searchAvariyaInput = document.getElementById('searchAvariyaInput');
    if (searchAvariyaInput) {
        searchAvariyaInput.addEventListener('input', function() {
            clearTimeout(window.searchAvariyaTimeout);
            window.searchAvariyaTimeout = setTimeout(() => searchAvariya(), 300);
        });
    }

	const searchPlansBtn = document.getElementById('searchPlansBtn');
if (searchPlansBtn) {
    searchPlansBtn.onclick = () => searchPlans();
}

// Кнопка поиска для аварий
const searchAvariyaBtn = document.getElementById('searchAvariyaBtn');
if (searchAvariyaBtn) {
    searchAvariyaBtn.onclick = () => searchAvariya();
}

// Кнопка поиска для истории аварий
const searchHistoryBtn = document.getElementById('searchHistoryBtn');
if (searchHistoryBtn) {
    searchHistoryBtn.onclick = () => searchHistory();
}

// Кнопка поиска для истории ремонтов
const searchRepairBtn = document.getElementById('searchRepairBtn');
if (searchRepairBtn) {
    searchRepairBtn.onclick = () => searchRepairHistory();
}
    
    const searchHistoryInput = document.getElementById('searchHistoryInput');
    if (searchHistoryInput) {
        searchHistoryInput.addEventListener('input', function() {
            clearTimeout(window.searchHistoryTimeout);
            window.searchHistoryTimeout = setTimeout(() => searchHistory(), 300);
        });
    }
    
    const searchRepairInput = document.getElementById('searchRepairInput');
    if (searchRepairInput) {
        searchRepairInput.addEventListener('input', function() {
            clearTimeout(window.searchRepairTimeout);
            window.searchRepairTimeout = setTimeout(() => searchRepairHistory(), 300);
        });
    }
}

function closeModal(m) { 
    let modal = document.getElementById(m); 
    if (modal) modal.style.display = 'none'; 
}
function showToast(msg, type) { 
    let toast = document.getElementById('toast'); 
    if (!toast) return; 
    let icon = type === 'success' ? '✅' : (type === 'error' ? '⚠️' : 'ℹ️'); 
    toast.innerHTML = `<div class="toast-content"><div class="toast-icon">${icon}</div><div class="toast-message">${escapeHtml(msg)}</div></div>`; 
    toast.className = `toast ${type}`; 
    toast.style.display = 'block'; 
    setTimeout(() => toast.style.display = 'none', 3000); 
}
function escapeHtml(t) { 
    if (!t) return ''; 
    return t.replace(/[&<>]/g, m => m === '&' ? '&amp;' : (m === '<' ? '&lt;' : '&gt;')); 
}

// ========== ИНИЦИАЛИЗАЦИЯ ==========
if (Notification.permission === 'default') Notification.requestPermission();
document.addEventListener('DOMContentLoaded', () => { 
    setupTabs(); 
    setupEventListeners(); 
    setDefaultDates(); 
    sendToCSharp('loadEquipment'); 
    sendToCSharp('loadTipTypes'); 
    sendToCSharp('loadResponsible'); 
    applyFilters(); 
    loadAvariya(); 
    loadCompletedAvariya(); 
    loadStatisticsWithDates();
});