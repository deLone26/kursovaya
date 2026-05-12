let allPlans = [], allAvariya = [], allHistory = [], currentEditPlanId = null;
let currentFilters = { equipmentFilter: '0', statusFilter: '', responsibleFilter: '', searchFilter: '', startDate: '', endDate: '' };
let selectedAccidentId = null, shownAccidentIds = new Set();
let currentBossId = 0, currentBossLogin = '', currentBossRole = '', currentBossFio = '';
let bossNotifications = [], bossUnreadCount = 0;

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
        document.querySelectorAll('.tab-btn').forEach(btn => { if (btn.dataset.tab === 'avariya') btn.click(); });
        setTimeout(() => highlightAccidentById(parseInt(accidentId)), 300);
    } else if (planId && planId !== '') {
        document.querySelectorAll('.tab-btn').forEach(btn => { if (btn.dataset.tab === 'plans') btn.click(); });
        setTimeout(() => highlightPlanById(parseInt(planId)), 300);
    }
    document.getElementById('bossNotifDropdown').style.display = 'none';
}

function highlightAccidentById(accidentId) {
    let rows = document.querySelectorAll('#avariyaTableBody tr');
    for (let row of rows) {
        let firstCell = row.cells[0];
        if (firstCell && parseInt(firstCell.innerText) === accidentId) {
            rows.forEach(r => r.classList.remove('selected')); row.classList.add('selected');
            row.scrollIntoView({ behavior: 'smooth', block: 'center' }); break;
        }
    }
}

function highlightPlanById(planId) {
    let rows = document.querySelectorAll('#plansTableBody tr');
    for (let row of rows) {
        let firstCell = row.cells[0];
        if (firstCell && parseInt(firstCell.innerText) === planId) {
            rows.forEach(r => r.classList.remove('selected')); row.classList.add('selected');
            row.scrollIntoView({ behavior: 'smooth', block: 'center' }); break;
        }
    }
}

function clearAllNotifications() { bossNotifications = []; bossUnreadCount = 0; updateBossNotificationUI(); }
function toggleBossNotifications() { let p = document.getElementById('bossNotifDropdown'); if (p) p.style.display = p.style.display === 'block' ? 'none' : 'block'; }
document.addEventListener('click', function(e) { let p = document.getElementById('bossNotifDropdown'), b = document.getElementById('bossBellBtn'); if (p && p.style.display === 'block') { if (b && b.contains(e.target)) return; if (!p.contains(e.target)) p.style.display = 'none'; } });

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command);
    if (command === 'fillEquipment') { fillSelect('equipmentFilter', data, true); fillSelect('planEquipment', data, false); }
    else if (command === 'fillTipTypesForPlan') fillSelect('createPlanTip', data, false);
    else if (command === 'fillResponsibleForPlan') fillSelect('createPlanResponsible', data, false);
    else if (command === 'fillTipTypes') fillSelect('planTip', data, false);
    else if (command === 'fillResponsible') { fillSelect('planResponsible', data, false); fillSelect('responsibleFilter', data, true); }
    else if (command === 'displayPlans') { const r = typeof data === 'string' ? JSON.parse(data) : data; allPlans = r.plans || []; renderPlansTable(); }
    else if (command === 'displayAvariya') { allAvariya = typeof data === 'string' ? JSON.parse(data) : data; renderAvariyaTable(); }
    else if (command === 'displayCompletedAvariya') renderCompletedAvariyaTable(data);
    else if (command === 'displayRepairHistory') renderRepairHistoryTable(data);
    else if (command === 'updateStatistics') { const s = typeof data === 'string' ? JSON.parse(data) : data;
        document.getElementById('statEquipment').innerHTML = s.totalEquipment || 0;
        document.getElementById('statActiveAvariya').innerHTML = s.activeAvariya || 0;
        document.getElementById('statCompletedAvariya').innerHTML = s.completedAvariya || 0;
        document.getElementById('statPlans').innerHTML = s.totalPlans || 0;
        document.getElementById('statCompleted').innerHTML = s.completedPlans || 0;
        document.getElementById('statOverdue').innerHTML = s.overduePlans || 0; }
    else if (command === 'showNewAvariya') { const av = typeof data === 'string' ? JSON.parse(data) : data; addBossNotification('Новая авария', `Оборудование: ${av.equipment}\nДата: ${av.date}`, 'error', av.id); loadAvariya(); loadStatistics(); if (Notification.permission === 'granted') new Notification('⚠️ Новая авария', { body: `Оборудование: ${av.equipment}\nДата: ${av.date}\nОписание: ${av.description}` }); }
    else if (command === 'showPlanCompleted') { const p = typeof data === 'string' ? JSON.parse(data) : data; addBossNotification('✅ План выполнен', `Оборудование: ${p.equipment}\nСлесарь: ${p.responsible}`, 'success', null, p.id); loadPlans(); loadStatistics(); loadRepairHistory(); }
    else if (command === 'showOverdueWarning') { const p = typeof data === 'string' ? JSON.parse(data) : data; addBossNotification('⚠️ Истекает срок', `Оборудование: ${p.equipment}\nДата окончания: ${p.end_date}`, 'error', null, p.id); }
    else if (command === 'showPlanOverdue') { const p = typeof data === 'string' ? JSON.parse(data) : data; addBossNotification('❌ План просрочен', `Оборудование: ${p.equipment}\nДата окончания: ${p.end_date}`, 'error', null, p.id); loadPlans(); loadStatistics(); }
    else if (command === 'showSuccess') showToast(data, 'success');
    else if (command === 'showError') showToast(data, 'error');
};

function fillSelect(id, data, addAll) { let select = document.getElementById(id); if (!select) return; let items = typeof data === 'string' ? JSON.parse(data) : data; let html = ''; if (addAll) html += '<option value="">Все</option>'; items.forEach(item => { if (item.id !== 0) html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`; }); select.innerHTML = html; }

function renderPlansTable() {
    let tbody = document.getElementById('plansTableBody');
    if (!allPlans.length) { tbody.innerHTML = '<tr><td colspan="10" class="loading">Нет данных</td</tr>'; return; }
    let html = '';
    for (let plan of allPlans) {
        let statusClass = plan.status === 'Просрочен' ? 'status-overdue' : (plan.status === 'Завершен' ? 'status-completed' : (plan.status === 'В работе' ? 'status-progress' : 'status-sent'));
        let opisanie = plan.opisanie || '-'; if (opisanie.length > 50) opisanie = opisanie.substring(0, 47) + '...';
        html += `<tr data-id="${plan.id}" onclick="selectPlanRow(this, ${plan.id})">
            <td>${plan.id}</td><td>${escapeHtml(plan.equipment)}</td><td>${escapeHtml(plan.tip)}</td>
            <td>${plan.start_date}</td><td>${plan.end_date}</td><td>${escapeHtml(plan.responsible)}</td>
            <td><span title="${escapeHtml(plan.opisanie || '')}">${escapeHtml(opisanie)}</span></td>
            <td><span class="status-badge ${statusClass}">${plan.status}</span></td><td>${plan.has_avariya}</td>
            <td><button class="edit-btn" onclick="editPlan(${plan.id})">✏️</button><button class="delete-btn" onclick="deletePlan(${plan.id})">🗑️</button></td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

function renderAvariyaTable() {
    let tbody = document.getElementById('avariyaTableBody');
    if (!allAvariya.length) { tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td</tr>'; return; }
    let html = '';
    for (let av of allAvariya) {
        let statusClass = av.status === 'Завершена' ? 'status-completed' : (av.status === 'В работе' ? 'status-progress' : '');
        html += `<tr data-id="${av.id}" onclick="selectAccidentRow(this, ${av.id})">
            <td>${av.id}</td><td>${escapeHtml(av.equipment)}</td><td>${av.date}</td>
            <td>${escapeHtml(av.description)}</td><td>${escapeHtml(av.consequences)}</td>
            <td><span class="status-badge ${statusClass}">${escapeHtml(av.status)}</span></td>
            <td><button class="create-plan-btn" onclick="openCreatePlanModal(${av.id})">📋 Запланировать</button></td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

function renderCompletedAvariyaTable(data) {
    let tbody = document.getElementById('historyTableBody'), items = typeof data === 'string' ? JSON.parse(data) : data;
    if (!items || !items.length) { tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td</table>'; return; }
    let html = '';
    for (let h of items) html += `<tr><td>${h.id || '-'}</td><td>${escapeHtml(h.equipment_name)}</td><td>${h.accident_date || '-'}</td><td>${escapeHtml(h.description || '-')}</td><td>${escapeHtml(h.responsible || '-')}</td><td>${escapeHtml(h.spare_parts || '-')}</td><td>${h.completion_date || '-'}</td></tr>`;
    tbody.innerHTML = html;
}

function renderRepairHistoryTable(data) {
    let tbody = document.getElementById('repairHistoryTableBody'), items = typeof data === 'string' ? JSON.parse(data) : data;
    if (!items || !items.length) { tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td</tr>'; return; }
    let html = '';
    for (let h of items) html += `<tr><td>${escapeHtml(h.equipment_name)}</td><td>${escapeHtml(h.tip_name)}</td><td>${h.plan_date}</td><td>${h.completed_date || '—'}</td><td>${escapeHtml(h.sotrudnik_name)}</td><td>${escapeHtml(h.opisanie)}</td><td>${escapeHtml(h.zamennaya_detal || '—')}</td></tr>`;
    tbody.innerHTML = html;
}

function selectPlanRow(e, id) { document.querySelectorAll('#plansTableBody tr').forEach(r => r.classList.remove('selected')); e.classList.add('selected'); }
function selectAccidentRow(e, id) { document.querySelectorAll('#avariyaTableBody tr').forEach(r => r.classList.remove('selected')); e.classList.add('selected'); selectedAccidentId = id; }
function loadPlans() { sendToCSharp('loadPlans', currentFilters); }
function loadAvariya() { sendToCSharp('loadAvariya', { startDate: document.getElementById('avariyaStartDate').value, endDate: document.getElementById('avariyaEndDate').value }); }
function loadCompletedAvariya() { sendToCSharp('loadCompletedAvariya', { startDate: document.getElementById('historyStartDate').value, endDate: document.getElementById('historyEndDate').value }); }
function loadRepairHistory() { sendToCSharp('loadRepairHistory', { startDate: document.getElementById('repairHistoryStartDate').value, endDate: document.getElementById('repairHistoryEndDate').value }); }
function loadStatistics() { sendToCSharp('loadStatistics'); }
function applyFilters() { currentFilters = { equipmentFilter: document.getElementById('equipmentFilter').value, statusFilter: document.getElementById('statusFilter').value, responsibleFilter: document.getElementById('responsibleFilter').value, searchFilter: document.getElementById('searchFilter').value, startDate: document.getElementById('startDateFilter').value, endDate: document.getElementById('endDateFilter').value }; loadPlans(); }

function addPlan() { let d = document.getElementById('planDescription').value; sendToCSharp('addPlan', { equipment: parseInt(document.getElementById('planEquipment').value), tip: parseInt(document.getElementById('planTip').value), startDate: document.getElementById('planStartDate').value, endDate: document.getElementById('planEndDate').value, responsible: parseInt(document.getElementById('planResponsible').value), opisanie: d || '' }); closeModal('planModal'); }
function editPlan(id) { let plan = allPlans.find(p => p.id === id); if (!plan) return; currentEditPlanId = id; document.getElementById('planModalTitle').innerText = 'Редактирование плана'; document.getElementById('planEquipment').value = plan.equipment_id; document.getElementById('planTip').value = plan.tip_id; document.getElementById('planStartDate').value = plan.start_date.split('.').reverse().join('-'); document.getElementById('planEndDate').value = plan.end_date.split('.').reverse().join('-'); document.getElementById('planResponsible').value = plan.responsible_id; document.getElementById('planDescription').value = plan.opisanie || ''; document.getElementById('planModal').style.display = 'flex'; }
function updatePlan() { let d = document.getElementById('planDescription').value; sendToCSharp('updatePlan', { id: currentEditPlanId, equipment: parseInt(document.getElementById('planEquipment').value), tip: parseInt(document.getElementById('planTip').value), startDate: document.getElementById('planStartDate').value, endDate: document.getElementById('planEndDate').value, responsible: parseInt(document.getElementById('planResponsible').value), status: document.getElementById('planStatus').value, opisanie: d || '' }); closeModal('planModal'); }
function deletePlan(id) { if (confirm('Удалить план?')) sendToCSharp('deletePlan', { id: id }); }
function openCreatePlanModal(accidentId) { let accident = allAvariya.find(a => a.id === accidentId); if (!accident) return; document.getElementById('createPlanAvariyaId').value = accidentId; document.getElementById('createPlanEquipment').value = accident.equipment; document.getElementById('createPlanDescription').value = accident.description; let today = new Date().toISOString().split('T')[0]; document.getElementById('createPlanStartDate').value = today; document.getElementById('createPlanEndDate').value = today; document.getElementById('createPlanOpisanie').value = ''; let tipSelect = document.getElementById('createPlanTip'); for (let i = 0; i < tipSelect.options.length; i++) if (tipSelect.options[i].text.includes('Аварийный')) { tipSelect.selectedIndex = i; break; } document.getElementById('createPlanModal').style.display = 'flex'; }
function exportToExcel() { sendToCSharp('exportToExcel', { reportType: document.getElementById('reportTypeSelect').value }); }
function exportToWord() { sendToCSharp('exportToWord', { reportType: document.getElementById('reportTypeSelect').value }); }
function previewReport() { sendToCSharp('previewReport'); }
function setDefaultDates() { let today = new Date(), monthAgo = new Date(); monthAgo.setMonth(monthAgo.getMonth() - 1); let f = d => d.toISOString().split('T')[0]; document.getElementById('startDateFilter').value = f(monthAgo); document.getElementById('endDateFilter').value = f(today); document.getElementById('avariyaStartDate').value = f(new Date(new Date().setMonth(new Date().getMonth() - 3))); document.getElementById('avariyaEndDate').value = f(today); document.getElementById('historyStartDate').value = f(monthAgo); document.getElementById('historyEndDate').value = f(today); document.getElementById('repairHistoryStartDate').value = f(monthAgo); document.getElementById('repairHistoryEndDate').value = f(today); }
function setupTabs() { document.querySelectorAll('.tab-btn').forEach(btn => { btn.onclick = () => { document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active')); document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active')); btn.classList.add('active'); let tab = document.getElementById(`${btn.dataset.tab}Tab`); if (tab) tab.classList.add('active'); if (btn.dataset.tab === 'plans') loadPlans(); if (btn.dataset.tab === 'avariya') loadAvariya(); if (btn.dataset.tab === 'history') loadCompletedAvariya(); if (btn.dataset.tab === 'repairHistory') loadRepairHistory(); if (btn.dataset.tab === 'statistics') loadStatistics(); }; }); }
function setupEventListeners() {
    document.getElementById('applyFilterBtn').onclick = () => applyFilters();
    document.getElementById('applyAvariyaFilterBtn').onclick = () => loadAvariya();
    document.getElementById('applyHistoryFilterBtn').onclick = () => loadCompletedAvariya();
    document.getElementById('applyRepairHistoryFilterBtn').onclick = () => loadRepairHistory();
    document.getElementById('addPlanBtn').onclick = () => { currentEditPlanId = null; document.getElementById('planModalTitle').innerText = 'Добавление плана'; document.getElementById('planEquipment').value = ''; document.getElementById('planTip').value = ''; document.getElementById('planStartDate').value = ''; document.getElementById('planEndDate').value = ''; document.getElementById('planResponsible').value = ''; document.getElementById('planDescription').value = ''; document.getElementById('planModal').style.display = 'flex'; };
    document.getElementById('savePlanBtn').onclick = () => { if (currentEditPlanId) updatePlan(); else addPlan(); };
    document.getElementById('exportExcelBtn').onclick = () => exportToExcel();
    document.getElementById('exportWordBtn').onclick = () => exportToWord();
    document.getElementById('previewReportBtn').onclick = () => previewReport();
    document.getElementById('bossBellBtn').onclick = () => toggleBossNotifications();
    document.getElementById('confirmCreatePlanBtn').onclick = () => { let a = document.getElementById('createPlanAvariyaId').value; sendToCSharp('createPlanFromAvariya', { id: parseInt(a), tipId: parseInt(document.getElementById('createPlanTip').value), startDate: document.getElementById('createPlanStartDate').value, endDate: document.getElementById('createPlanEndDate').value, responsibleId: parseInt(document.getElementById('createPlanResponsible').value), opisanie: document.getElementById('createPlanOpisanie').value }); closeModal('createPlanModal'); };
}
function closeModal(m) { let modal = document.getElementById(m); if (modal) modal.style.display = 'none'; }
function showToast(msg, type) { let toast = document.getElementById('toast'); if (!toast) return; let icon = type === 'success' ? '✅' : (type === 'error' ? '⚠️' : 'ℹ️'); toast.innerHTML = `<div class="toast-content"><div class="toast-icon">${icon}</div><div class="toast-message">${escapeHtml(msg)}</div></div>`; toast.className = `toast ${type}`; toast.style.display = 'block'; setTimeout(() => toast.style.display = 'none', 3000); }
function escapeHtml(t) { if (!t) return ''; return t.replace(/[&<>]/g, m => m === '&' ? '&amp;' : (m === '<' ? '&lt;' : '&gt;')); }
if (Notification.permission === 'default') Notification.requestPermission();
document.addEventListener('DOMContentLoaded', () => { setupTabs(); setupEventListeners(); setDefaultDates(); sendToCSharp('loadEquipment'); sendToCSharp('loadTipTypes'); sendToCSharp('loadResponsible'); applyFilters(); loadAvariya(); loadCompletedAvariya(); loadStatistics(); });