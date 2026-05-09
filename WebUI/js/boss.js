let allPlans = [];
let allAvariya = [];
let allHistory = [];
let currentEditPlanId = null;
let currentFilters = { equipmentFilter: '0', statusFilter: '', startDate: '', endDate: '' };

function sendToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action: action, ...data });
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(msg);
    }
}

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command, data);
    
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
    else if (command === 'updateStatistics') {
        const stats = typeof data === 'string' ? JSON.parse(data) : data;
        document.getElementById('statEquipment').innerHTML = stats.totalEquipment || 0;
        document.getElementById('statAvariya').innerHTML = stats.totalAvariya || 0;
        document.getElementById('statPlans').innerHTML = stats.totalPlans || 0;
        document.getElementById('statCompleted').innerHTML = stats.completedPlans || 0;
        document.getElementById('statOverdue').innerHTML = stats.overduePlans || 0;
        document.getElementById('statInProgress').innerHTML = stats.inProgressPlans || 0;
    }
    else if (command === 'showNewAvariya') {
        const av = typeof data === 'string' ? JSON.parse(data) : data;
        showToast(`🚨 НОВАЯ АВАРИЯ!\nОборудование: ${av.equipment}\nДата: ${av.date}`, 'error');
        // Обновляем список аварий и статистику
        loadAvariya();
        loadStatistics();
        // Всплывающее уведомление браузера
        if (Notification.permission === 'granted') {
            new Notification('Новая авария', {
                body: `Оборудование: ${av.equipment}\nДата: ${av.date}`,
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
        tbody.innerHTML = '<tr><td colspan="9" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let plan of allPlans) {
        let statusClass = '';
        if (plan.status === 'Просрочен') statusClass = 'critical';
        if (plan.status === 'Завершен') statusClass = 'completed';
        
        html += `<tr>
            <td>${plan.id}</td>
            <td>${escapeHtml(plan.equipment)}</td>
            <td>${escapeHtml(plan.tip)}</td>
            <td>${plan.start_date}</td>
            <td>${plan.end_date}</td>
            <td>${escapeHtml(plan.responsible)}</td>
            <td class="${statusClass}">${plan.status}</td>
            <td>${plan.has_avariya}</td>
            <td>
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
        tbody.innerHTML = '<tr><td colspan="8" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let av of allAvariya) {
        const hasPlan = av.has_plan === 'Да';
        const actionButton = !hasPlan 
            ? `<button class="create-plan-btn" onclick="createPlanFromAvariya(${av.id})">Запланировать ремонт</button>` 
            : '<span style="color: #10b981;">✅ План создан</span>';
        
        html += `<tr>
            <td>${av.id}</td>
            <td>${escapeHtml(av.equipment)}</td>
            <td>${av.date}</td>
            <td>${escapeHtml(av.description)}</td>
            <td>${escapeHtml(av.consequences)}</td>
            <td>${escapeHtml(av.status)}</td>
            <td>${av.has_plan}</td>
            <td>${actionButton}</td>
        </tr>`;
    }
    tbody.innerHTML = html;
}

function renderHistoryTable() {
    const tbody = document.getElementById('historyTableBody');
    if (!allHistory.length) {
        tbody.innerHTML = '<tr><td colspan="6" class="loading">Нет данных</td></tr>';
        return;
    }
    
    let html = '';
    for (let h of allHistory) {
        html += `<tr>
            <td>${escapeHtml(h.equipment_name)}</td>
            <td>${escapeHtml(h.tip_name)}</td>
            <td>${h.plan_date}</td>
            <td>${h.completed_date}</td>
            <td>${escapeHtml(h.sotrudnik_name)}</td>
            <td>${escapeHtml(h.opisanie)}</td>
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
    document.getElementById('planStatus').value = plan.status;
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

function createPlanFromAvariya(id) {
    sendToCSharp('createPlanFromAvariya', { id: id });
    setTimeout(() => {
        switchToPlansTab();
    }, 500);
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
    
    document.getElementById('startDateFilter').value = formatDate(monthAgo);
    document.getElementById('endDateFilter').value = formatDate(today);
    document.getElementById('avariyaStartDate').value = formatDate(monthAgo);
    document.getElementById('avariyaEndDate').value = formatDate(today);
    document.getElementById('historyStartDate').value = formatDate(monthAgo);
    document.getElementById('historyEndDate').value = formatDate(today);
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
            document.getElementById(`${tabName}Tab`).classList.add('active');
            
            if (tabName === 'plans') loadPlans();
            if (tabName === 'avariya') loadAvariya();
            if (tabName === 'history') loadHistory();
            if (tabName === 'statistics') loadStatistics();
        };
    });
}

function setupEventListeners() {
    document.getElementById('applyFilterBtn').onclick = () => applyFilters();
    document.getElementById('applyAvariyaFilterBtn').onclick = () => loadAvariya();
    document.getElementById('applyHistoryFilterBtn').onclick = () => loadHistory();
    document.getElementById('addPlanBtn').onclick = () => {
        currentEditPlanId = null;
        document.getElementById('planModalTitle').innerText = 'Добавление плана';
        document.getElementById('planEquipment').value = '';
        document.getElementById('planTip').value = '';
        document.getElementById('planStartDate').value = '';
        document.getElementById('planEndDate').value = '';
        document.getElementById('planResponsible').value = '';
        document.getElementById('planModal').style.display = 'flex';
    };
    document.getElementById('savePlanBtn').onclick = () => {
        if (currentEditPlanId) {
            updatePlan();
        } else {
            addPlan();
        }
    };
    document.getElementById('exportExcelBtn').onclick = () => exportToExcel();
    document.getElementById('exportWordBtn').onclick = () => exportToWord();
    document.getElementById('previewReportBtn').onclick = () => previewReport();
}

function closeModal(modalId) {
    document.getElementById(modalId).style.display = 'none';
}

function showToast(msg, type) {
    const toast = document.getElementById('toast');
    toast.textContent = msg;
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

// Запрос разрешения на уведомления
if (Notification.permission === 'default') {
    Notification.requestPermission();
}

document.addEventListener('DOMContentLoaded', () => {
    setupTabs();
    setupEventListeners();
    setDefaultDates();
    
    // Загружаем все необходимые данные
    sendToCSharp('loadEquipment');
    sendToCSharp('loadTipTypes');
    sendToCSharp('loadResponsible');
    
    // Устанавливаем фильтры по умолчанию и загружаем данные
    applyFilters();
    loadAvariya();
    loadHistory();
    loadStatistics();
});