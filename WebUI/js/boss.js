let allPlans = [];
let allAvariya = [];
let allHistory = [];
let currentEditPlanId = null;
let currentFilters = { equipmentFilter: '0', statusFilter: '', startDate: '', endDate: '' };
let selectedAccidentId = null;
let shownAccidentIds = new Set();

// Данные текущего пользователя
let currentBossId = 0;
let currentBossLogin = '';
let currentBossRole = '';
let currentBossFio = '';

function setCurrentUser(id, login, role, fullName) {
    currentBossId = id;
    currentBossLogin = login;
    currentBossRole = role;
    currentBossFio = fullName;
    
    const fioElement = document.getElementById('bossFio');
    if (fioElement) {
        fioElement.innerText = fullName || 'Сотрудник';
    }
    
    console.log('Установлен пользователь:', fullName, 'Роль:', role);
}

// Уведомления для начальника
let bossNotifications = [];
let bossUnreadCount = 0;

function clearAllNotifications() {
    bossNotifications = [];
    bossUnreadCount = 0;
    updateBossNotificationUI();
    const badge = document.getElementById('bossNotifBadge');
    if (badge) badge.style.display = 'none';
}

function sendToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action: action, ...data });
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(msg);
    }
}

function renderCompletedAvariyaTable(data) {
    const tbody = document.getElementById('historyTableBody');
    const items = typeof data === 'string' ? JSON.parse(data) : data;
    
    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let h of items) {
        html += `<tr data-id="${h.id}" onclick="selectHistoryRow(this, ${h.id})">
                    <td>${h.id || '-'}</td>
                    <td>${escapeHtml(h.equipment_name)}</td>
                    <td>${h.accident_date || '-'}</td>
                    <td>${escapeHtml(h.description || '-')}</td>
                    <td>${escapeHtml(h.responsible || '-')}</td>
                    <td>${escapeHtml(h.spare_parts || '-')}</td>
                    <td>${h.completion_date || '-'}</td>
                </tr>`;
    }
    tbody.innerHTML = html;
}

function addBossNotification(title, message, type = 'info', accidentId = null) {
    if (accidentId && shownAccidentIds.has(accidentId)) {
        return;
    }
    
    let now = new Date();
    let timeText = formatRelativeTime(now);
    
    let notification = {
        id: Date.now(),
        title: title,
        message: message,
        time: timeText,
        timestamp: now,
        type: type,
        accidentId: accidentId,
        isRead: false
    };
    
    if (accidentId) {
        shownAccidentIds.add(accidentId);
    }
    
    bossNotifications.unshift(notification);
    bossUnreadCount++;
    updateBossNotificationUI();
    
    showToast(`${title}: ${message}`, type === 'error' ? 'error' : 'info');
}

function updateBossNotificationUI() {
    let countEl = document.getElementById('bossNotifBadge');
    if (countEl) {
        countEl.innerText = bossUnreadCount;
        countEl.style.display = bossUnreadCount > 0 ? 'flex' : 'none';
    }
    
    let container = document.getElementById('bossNotifList');
    if (!container) return;
    
    if (bossNotifications.length === 0) {
        container.innerHTML = '<div class="notif-item">Нет уведомлений</div>';
        return;
    }
    
    let html = '';
    for (let n of bossNotifications) {
        let unreadClass = !n.isRead ? 'unread' : '';
        let criticalClass = n.type === 'error' ? 'critical' : '';
        let icon = n.type === 'error' ? '⚠️' : '🔔';
        
        html += `
            <div class="notif-item ${unreadClass} ${criticalClass}" data-accident-id="${n.accidentId || ''}" onclick="onBossNotificationClick(this)">
                <div class="notif-title ${criticalClass}">${icon} ${escapeHtml(n.title)}</div>
                <div class="notif-text">${escapeHtml(n.message)}</div>
                <div class="notif-time">${n.time}</div>
            </div>
        `;
    }
    
    html += `
        <div class="notification-footer">
            <a onclick="clearAllNotifications()">Очистить все</a>
        </div>
    `;
    
    container.innerHTML = html;
}

function onBossNotificationClick(element) {
    let accidentId = element.getAttribute('data-accident-id');
    
    if (accidentId && accidentId !== '') {
        document.querySelectorAll('.tab-btn').forEach(btn => {
            if (btn.dataset.tab === 'avariya') btn.click();
        });
        
        setTimeout(() => {
            highlightAccidentById(parseInt(accidentId));
        }, 300);
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
            row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            break;
        }
    }
}

function selectAccidentRow(element, accidentId) {
    document.querySelectorAll('#avariyaTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
    selectedAccidentId = accidentId;
}

function selectPlanRow(element, planId) {
    document.querySelectorAll('#plansTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
}

function selectHistoryRow(element, historyId) {
    document.querySelectorAll('#historyTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
}

function selectRepairHistoryRow(element, repairId) {
    document.querySelectorAll('#repairHistoryTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
}

function markBossNotificationRead(id) {
    let notif = bossNotifications.find(n => n.id === id);
    if (notif && !notif.isRead) {
        notif.isRead = true;
        bossUnreadCount--;
        updateBossNotificationUI();
    }
}

function loadCompletedAvariya() {
    const startDate = document.getElementById('historyStartDate').value;
    const endDate = document.getElementById('historyEndDate').value;
    sendToCSharp('loadCompletedAvariya', { startDate, endDate });
}

function loadRepairHistory() {
    const startDate = document.getElementById('repairHistoryStartDate').value;
    const endDate = document.getElementById('repairHistoryEndDate').value;
    sendToCSharp('loadRepairHistory', { startDate, endDate });
}

function toggleBossNotifications() {
    let panel = document.getElementById('bossNotifDropdown');
    if (panel) {
        panel.style.display = panel.style.display === 'block' ? 'none' : 'block';
    }
}

document.addEventListener('click', function(event) {
    let panel = document.getElementById('bossNotifDropdown');
    let btn = document.getElementById('bossBellBtn');
    if (panel && panel.style.display === 'block') {
        if (btn && btn.contains(event.target)) return;
        if (!panel.contains(event.target)) {
            panel.style.display = 'none';
        }
    }
});

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command);
    
    if (command === 'fillEquipment') {
        const select = document.getElementById('equipmentFilter');
        const planSelect = document.getElementById('planEquipment');
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        
        if (select) {
            select.innerHTML = '<option value="0">Все оборудование</option>';
            items.forEach(item => {
                if (item.id !== 0) {
                    select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
                }
            });
        }
        if (planSelect) {
            planSelect.innerHTML = '<option value="">Выберите оборудование</option>';
            items.forEach(item => {
                if (item.id !== 0) {
                    planSelect.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
                }
            });
        }
    }
    else if (command === 'fillTipTypesForPlan') {
        const select = document.getElementById('createPlanTip');
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        if (select) {
            select.innerHTML = '<option value="">Выберите тип ТО</option>';
            items.forEach(item => {
                select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
            });
            for (let i = 0; i < select.options.length; i++) {
                if (select.options[i].text === 'Аварийный ремонт' || 
                    select.options[i].text.includes('Аварийный')) {
                    select.selectedIndex = i;
                    break;
                }
            }
        }
    }
    else if (command === 'fillResponsibleForPlan') {
        const select = document.getElementById('createPlanResponsible');
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        if (select) {
            select.innerHTML = '<option value="">Выберите ответственного</option>';
            items.forEach(item => {
                select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
            });
        }
    }
    else if (command === 'fillTipTypes') {
        const select = document.getElementById('planTip');
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        if (select) {
            select.innerHTML = '<option value="">Выберите тип ТО</option>';
            items.forEach(item => {
                select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
            });
        }
    }
    else if (command === 'fillResponsible') {
        const select = document.getElementById('planResponsible');
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        if (select) {
            select.innerHTML = '<option value="">Выберите ответственного</option>';
            items.forEach(item => {
                select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
            });
        }
    }
    else if (command === 'displayPlans') {
        const result = typeof data === 'string' ? JSON.parse(data) : data;
        allPlans = result.plans || [];
        renderPlansTable();
    }
    else if (command === 'displayAvariya') {
        allAvariya = typeof data === 'string' ? JSON.parse(data) : data;
        renderAvariyaTable();
    }
    else if (command === 'displayHistory') {
        const result = typeof data === 'string' ? JSON.parse(data) : data;
        allHistory = result.history || [];
        renderHistoryTable();
    }
    else if (command === 'displayCompletedAvariya') {
        renderCompletedAvariyaTable(data);
    }
    else if (command === 'displayRepairHistory') {
        renderRepairHistoryTable(data);
    }
    else if (command === 'updateStatistics') {
        const stats = typeof data === 'string' ? JSON.parse(data) : data;
        document.getElementById('statEquipment').innerHTML = stats.totalEquipment || 0;
        document.getElementById('statActiveAvariya').innerHTML = stats.activeAvariya || 0;
        document.getElementById('statCompletedAvariya').innerHTML = stats.completedAvariya || 0;
        document.getElementById('statPlans').innerHTML = stats.totalPlans || 0;
        document.getElementById('statCompleted').innerHTML = stats.completedPlans || 0;
        document.getElementById('statOverdue').innerHTML = stats.overduePlans || 0;
    }
    else if (command === 'showNewAvariya') {
        const av = typeof data === 'string' ? JSON.parse(data) : data;
        addBossNotification('Новая авария', `Оборудование: ${av.equipment}\nДата: ${av.date}`, 'error', av.id);
        loadAvariya();
        loadStatistics();
        if (Notification.permission === 'granted') {
            new Notification('⚠️ Новая авария', {
                body: `Оборудование: ${av.equipment}\nДата: ${av.date}\nОписание: ${av.description}`,
                icon: 'https://cdn-icons-png.flaticon.com/512/190/190411.png'
            });
        }
    }
    else if (command === 'showSuccess') {
        showToast(data, 'success');
    }
    else if (command === 'showError') {
        showToast(data, 'error');
    }
};

function renderPlansTable() {
    const tbody = document.getElementById('plansTableBody');
    if (!allPlans.length) {
        tbody.innerHTML = '<tr><td colspan="9" class="loading">Нет данных</td</tr>';
        return;
    }
    
    let html = '';
    for (let plan of allPlans) {
        let statusClass = '';
        if (plan.status === 'Просрочен') statusClass = 'status-overdue';
        if (plan.status === 'Завершен') statusClass = 'status-completed';
        if (plan.status === 'В работе') statusClass = 'status-progress';
        if (plan.status === 'Отправлено в работу') statusClass = 'status-sent';
        
        html += `<tr data-id="${plan.id}" onclick="selectPlanRow(this, ${plan.id})">
                    <td style="width:55px">${plan.id}</td>
                    <td style="width:180px">${escapeHtml(plan.equipment)}</td>
                    <td style="width:130px">${escapeHtml(plan.tip)}</td>
                    <td style="width:105px">${plan.start_date}</td>
                    <td style="width:105px">${plan.end_date}</td>
                    <td style="width:160px">${escapeHtml(plan.responsible)}</td>
                    <td style="width:140px"><span class="status-badge ${statusClass}">${plan.status}</span></td>
                    <td style="width:70px">${plan.has_avariya}</td>
                    <td style="width:110px">
                        <button class="edit-btn" onclick="editPlan(${plan.id})">✏️</button>
                        <button class="delete-btn" onclick="deletePlan(${plan.id})">🗑️</button>
                    </td>
                </tr>`;
    }
    tbody.innerHTML = html;
}

function renderAvariyaTable() {
    const tbody = document.getElementById('avariyaTableBody');
    if (!allAvariya.length) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let av of allAvariya) {
        let actionButton = `<button class="create-plan-btn" onclick="openCreatePlanModal(${av.id})">📋 Запланировать</button>`;
        
        let statusClass = '';
        if (av.status === 'Завершена') statusClass = 'status-completed';
        if (av.status === 'В работе') statusClass = 'status-progress';
        
        html += `<tr data-id="${av.id}" onclick="selectAccidentRow(this, ${av.id})">
                    <td>${av.id}</td>
                    <td>${escapeHtml(av.equipment)}</td>
                    <td>${av.date}</td>
                    <td>${escapeHtml(av.description)}</td>
                    <td>${escapeHtml(av.consequences)}</td>
                    <td><span class="status-badge ${statusClass}">${escapeHtml(av.status)}</span></td>
                    <td>${actionButton}</td>
                </tr>`;
    }
    tbody.innerHTML = html;
}

function renderHistoryTable() {
    const tbody = document.getElementById('historyTableBody');
    if (!allHistory.length) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let h of allHistory) {
        html += `<tr data-id="${h.id}" onclick="selectHistoryRow(this, ${h.id})">
                    <td>${h.id || '-'}</td>
                    <td>${escapeHtml(h.equipment_name)}</td>
                    <td>${h.accident_date || '-'}</td>
                    <td>${escapeHtml(h.description || '-')}</td>
                    <td>${escapeHtml(h.responsible || '-')}</td>
                    <td>${escapeHtml(h.spare_parts || '-')}</td>
                    <td>${h.completion_date || '-'}</td>
                </tr>`;
    }
    tbody.innerHTML = html;
}

function renderRepairHistoryTable(data) {
    const tbody = document.getElementById('repairHistoryTableBody');
    const items = typeof data === 'string' ? JSON.parse(data) : data;
    
    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let h of items) {
        html += `<tr onclick="selectRepairHistoryRow(this, ${h.id})">
                    <td>${escapeHtml(h.equipment_name)}</td>
                    <td>${escapeHtml(h.tip_name)}</td>
                    <td>${h.plan_date}</td>
                    <td>${h.completed_date || '—'}</td>
                    <td>${escapeHtml(h.sotrudnik_name)}</td>
                    <td>${escapeHtml(h.opisanie)}</td>
                    <td>${escapeHtml(h.zamennaya_detal || '—')}</td>
                </tr>`;
    }
    tbody.innerHTML = html;
}

function loadPlans() {
    sendToCSharp('loadPlans', currentFilters);
}

function loadAvariya() {
    const startDate = document.getElementById('avariyaStartDate').value;
    const endDate = document.getElementById('avariyaEndDate').value;
    sendToCSharp('loadAvariya', { startDate, endDate });
}

function loadHistory() {
    const startDate = document.getElementById('historyStartDate').value;
    const endDate = document.getElementById('historyEndDate').value;
    sendToCSharp('loadHistory', { startDate, endDate });
}

function loadRepairHistory() {
    const startDate = document.getElementById('repairHistoryStartDate').value;
    const endDate = document.getElementById('repairHistoryEndDate').value;
    sendToCSharp('loadRepairHistory', { startDate, endDate });
}

function loadStatistics() {
    sendToCSharp('loadStatistics');
}

function applyFilters() {
    currentFilters = {
        equipmentFilter: document.getElementById('equipmentFilter').value,
        statusFilter: document.getElementById('statusFilter').value,
        startDate: document.getElementById('startDateFilter').value,
        endDate: document.getElementById('endDateFilter').value
    };
    loadPlans();
}

function addPlan() {
    const equipment = document.getElementById('planEquipment').value;
    const tip = document.getElementById('planTip').value;
    const startDate = document.getElementById('planStartDate').value;
    const endDate = document.getElementById('planEndDate').value;
    const responsible = document.getElementById('planResponsible').value;
    
    if (!equipment || !tip || !startDate || !endDate || !responsible) {
        showToast('Заполните все поля!', 'error');
        return;
    }
    
    sendToCSharp('addPlan', {
        equipment: parseInt(equipment),
        tip: parseInt(tip),
        startDate: startDate,
        endDate: endDate,
        responsible: parseInt(responsible)
    });
    closeModal('planModal');
}

function editPlan(id) {
    const plan = allPlans.find(p => p.id === id);
    if (!plan) return;
    
    currentEditPlanId = id;
    document.getElementById('planModalTitle').innerText = 'Редактирование плана';
    document.getElementById('planEquipment').value = plan.equipment_id;
    document.getElementById('planTip').value = plan.tip_id;
    document.getElementById('planStartDate').value = plan.start_date.split('.').reverse().join('-');
    document.getElementById('planEndDate').value = plan.end_date.split('.').reverse().join('-');
    document.getElementById('planResponsible').value = plan.responsible_id;
    document.getElementById('planModal').style.display = 'flex';
}

function updatePlan() {
    const equipment = document.getElementById('planEquipment').value;
    const tip = document.getElementById('planTip').value;
    const startDate = document.getElementById('planStartDate').value;
    const endDate = document.getElementById('planEndDate').value;
    const responsible = document.getElementById('planResponsible').value;
    const status = document.getElementById('planStatus').value;
    
    sendToCSharp('updatePlan', {
        id: currentEditPlanId,
        equipment: parseInt(equipment),
        tip: parseInt(tip),
        startDate: startDate,
        endDate: endDate,
        responsible: parseInt(responsible),
        status: status
    });
    closeModal('planModal');
}

function deletePlan(id) {
    if (confirm('Удалить план?')) {
        sendToCSharp('deletePlan', { id: id });
    }
}

function openCreatePlanModal(accidentId) {
    const accident = allAvariya.find(a => a.id === accidentId);
    if (!accident) return;
    
    document.getElementById('createPlanAvariyaId').value = accidentId;
    document.getElementById('createPlanEquipment').value = accident.equipment;
    document.getElementById('createPlanDescription').value = accident.description;
    
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    document.getElementById('createPlanStartDate').value = today.toISOString().split('T')[0];
    document.getElementById('createPlanEndDate').value = tomorrow.toISOString().split('T')[0];
    document.getElementById('createPlanOpisanie').value = '';
    
    // Автоматически выбираем "Аварийный ремонт" в выпадающем списке типов ТО
    const tipSelect = document.getElementById('createPlanTip');
    for (let i = 0; i < tipSelect.options.length; i++) {
        if (tipSelect.options[i].text === 'Аварийный ремонт' || 
            tipSelect.options[i].text.includes('Аварийный')) {
            tipSelect.selectedIndex = i;
            break;
        }
    }
    
    document.getElementById('createPlanModal').style.display = 'flex';
}

function switchToPlansTab() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        if (btn.dataset.tab === 'plans') btn.click();
    });
}

function exportToExcel() {
    const reportType = document.getElementById('reportTypeSelect').value;
    sendToCSharp('exportToExcel', { reportType: reportType });
}

function exportToWord() {
    const reportType = document.getElementById('reportTypeSelect').value;
    sendToCSharp('exportToWord', { reportType: reportType });
}

function previewReport() {
    sendToCSharp('previewReport');
}

function setDefaultDates() {
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    
    const formatDate = (date) => date.toISOString().split('T')[0];
    
    const startDateFilter = document.getElementById('startDateFilter');
    const endDateFilter = document.getElementById('endDateFilter');
    const avariyaStartDate = document.getElementById('avariyaStartDate');
    const avariyaEndDate = document.getElementById('avariyaEndDate');
    const historyStartDate = document.getElementById('historyStartDate');
    const historyEndDate = document.getElementById('historyEndDate');
    const repairHistoryStartDate = document.getElementById('repairHistoryStartDate');
    const repairHistoryEndDate = document.getElementById('repairHistoryEndDate');
    
    if (startDateFilter) startDateFilter.value = formatDate(monthAgo);
    if (endDateFilter) endDateFilter.value = formatDate(today);
    
    if (avariyaStartDate) {
        const threeMonthsAgo = new Date();
        threeMonthsAgo.setMonth(threeMonthsAgo.getMonth() - 3);
        avariyaStartDate.value = formatDate(threeMonthsAgo);
    }
    if (avariyaEndDate) avariyaEndDate.value = formatDate(today);
    
    if (historyStartDate) historyStartDate.value = formatDate(monthAgo);
    if (historyEndDate) historyEndDate.value = formatDate(today);
    if (repairHistoryStartDate) repairHistoryStartDate.value = formatDate(monthAgo);
    if (repairHistoryEndDate) repairHistoryEndDate.value = formatDate(today);
}

function setupTabs() {
    const tabs = document.querySelectorAll('.tab-btn');
    const contents = document.querySelectorAll('.tab-content');
    
    tabs.forEach(btn => {
        btn.onclick = () => {
            tabs.forEach(b => b.classList.remove('active'));
            contents.forEach(c => c.classList.remove('active'));
            btn.classList.add('active');
            const tabName = btn.dataset.tab;
            const activeTab = document.getElementById(`${tabName}Tab`);
            if (activeTab) activeTab.classList.add('active');
            
            if (tabName === 'plans') loadPlans();
            if (tabName === 'avariya') loadAvariya();
            if (tabName === 'history') loadCompletedAvariya();
            if (tabName === 'repairHistory') loadRepairHistory();
            if (tabName === 'statistics') loadStatistics();
        };
    });
}

function setupEventListeners() {
    const applyFilterBtn = document.getElementById('applyFilterBtn');
    if (applyFilterBtn) applyFilterBtn.onclick = () => applyFilters();
    
    const applyAvariyaFilterBtn = document.getElementById('applyAvariyaFilterBtn');
    if (applyAvariyaFilterBtn) applyAvariyaFilterBtn.onclick = () => loadAvariya();
    
    const applyHistoryFilterBtn = document.getElementById('applyHistoryFilterBtn');
    if (applyHistoryFilterBtn) applyHistoryFilterBtn.onclick = () => loadHistory();
    
    const applyRepairHistoryFilterBtn = document.getElementById('applyRepairHistoryFilterBtn');
    if (applyRepairHistoryFilterBtn) applyRepairHistoryFilterBtn.onclick = () => loadRepairHistory();
    
    const addPlanBtn = document.getElementById('addPlanBtn');
    if (addPlanBtn) {
        addPlanBtn.onclick = () => {
            currentEditPlanId = null;
            document.getElementById('planModalTitle').innerText = 'Добавление плана';
            const planEquipment = document.getElementById('planEquipment');
            const planTip = document.getElementById('planTip');
            const planStartDate = document.getElementById('planStartDate');
            const planEndDate = document.getElementById('planEndDate');
            const planResponsible = document.getElementById('planResponsible');
            if (planEquipment) planEquipment.value = '';
            if (planTip) planTip.value = '';
            if (planStartDate) planStartDate.value = '';
            if (planEndDate) planEndDate.value = '';
            if (planResponsible) planResponsible.value = '';
            document.getElementById('planModal').style.display = 'flex';
        };
    }
    
    const savePlanBtn = document.getElementById('savePlanBtn');
    if (savePlanBtn) {
        savePlanBtn.onclick = () => {
            if (currentEditPlanId) updatePlan();
            else addPlan();
        };
    }
    
    const exportExcelBtn = document.getElementById('exportExcelBtn');
    if (exportExcelBtn) exportExcelBtn.onclick = () => exportToExcel();
    
    const exportWordBtn = document.getElementById('exportWordBtn');
    if (exportWordBtn) exportWordBtn.onclick = () => exportToWord();
    
    const previewReportBtn = document.getElementById('previewReportBtn');
    if (previewReportBtn) previewReportBtn.onclick = () => previewReport();
    
    const bellBtn = document.getElementById('bossBellBtn');
    if (bellBtn) {
        bellBtn.onclick = () => toggleBossNotifications();
    }
    
    const confirmCreatePlanBtn = document.getElementById('confirmCreatePlanBtn');
    if (confirmCreatePlanBtn) {
        confirmCreatePlanBtn.onclick = function() {
            const accidentId = document.getElementById('createPlanAvariyaId').value;
            const tipId = document.getElementById('createPlanTip').value;
            const startDate = document.getElementById('createPlanStartDate').value;
            const endDate = document.getElementById('createPlanEndDate').value;
            const responsibleId = document.getElementById('createPlanResponsible').value;
            const opisanie = document.getElementById('createPlanOpisanie').value;
            
            if (!tipId) {
                showToast('Выберите тип ТО!', 'error');
                return;
            }
            if (!startDate) {
                showToast('Выберите дату начала!', 'error');
                return;
            }
            if (!endDate) {
                showToast('Выберите дату окончания!', 'error');
                return;
            }
            if (!responsibleId) {
                showToast('Выберите ответственного!', 'error');
                return;
            }
            
            sendToCSharp('createPlanFromAvariya', {
                accidentId: parseInt(accidentId),
                tipId: parseInt(tipId),
                startDate: startDate,
                endDate: endDate,
                responsibleId: parseInt(responsibleId),
                opisanie: opisanie
            });
            
            closeModal('createPlanModal');
        };
    }
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.style.display = 'none';
}

function showToast(msg, type) {
    const toast = document.getElementById('toast');
    if (!toast) return;
    
    const icon = type === 'success' ? '✅' : (type === 'error' ? '⚠️' : 'ℹ️');
    
    toast.innerHTML = `
        <div class="toast-content">
            <div class="toast-icon">${icon}</div>
            <div class="toast-message">${escapeHtml(msg)}</div>
        </div>
    `;
    toast.className = `toast ${type}`;
    toast.style.display = 'block';
    
    setTimeout(() => {
        toast.style.display = 'none';
    }, 3000);
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

if (Notification.permission === 'default') {
    Notification.requestPermission();
}

function formatRelativeTime(date) {
    let now = new Date();
    let diff = Math.floor((now - date) / 1000 / 60);
    
    if (diff < 1) return "только что";
    if (diff < 60) return `${diff} мин назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч назад`;
    return `${Math.floor(diff / 1440)} дн назад`;
}

document.addEventListener('DOMContentLoaded', () => {
    setupTabs();
    setupEventListeners();
    setDefaultDates();
    
    sendToCSharp('loadEquipment');
    sendToCSharp('loadTipTypes');
    sendToCSharp('loadResponsible');
    
    applyFilters();
    loadAvariya();
    loadHistory();
    loadStatistics();
});