let employees = [];
let allPlans = [];
let allAvariya = [];
let allHistory = [];
let currentEditPlanId = null;

function sendToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action: action, ...data });
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(msg);
    } else {
        console.log('WebView не доступен');
    }
}

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command, data);
    if (command === 'fillEquipment') {
        const select = document.getElementById('planEquipment');
        select.innerHTML = '<option value="">Выберите оборудование</option>';
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        items.forEach(item => {
            select.innerHTML += `<option value="${item.id}">${item.name}</option>`;
        });
    }
    else if (command === 'fillTipTypes') {
        const select = document.getElementById('planTip');
        select.innerHTML = '<option value="">Выберите тип ТО</option>';
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        items.forEach(item => {
            select.innerHTML += `<option value="${item.id}">${item.name}</option>`;
        });
    }
    else if (command === 'fillResponsible') {
        const select = document.getElementById('planResponsible');
        select.innerHTML = '<option value="">Выберите ответственного</option>';
        const items = typeof data === 'string' ? JSON.parse(data) : data;
        items.forEach(item => {
            select.innerHTML += `<option value="${item.id}">${item.name}</option>`;
        });
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
        html += `<tr>
            <td>${av.id}</td>
            <td>${escapeHtml(av.equipment)}</td>
            <td>${av.date}</td>
            <td>${escapeHtml(av.description)}</td>
            <td>${escapeHtml(av.consequences)}</td>
            <td>${escapeHtml(av.status)}</td>
            <td>${av.has_plan}</td>
            <td>
                <button class="create-plan-btn" onclick="createPlanFromAvariya(${av.id})">Создать план</button>
            </td>
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

function loadEquipment() { sendToCSharp('loadEquipment'); }
function loadTipTypes() { sendToCSharp('loadTipTypes'); }
function loadResponsible() { sendToCSharp('loadResponsible'); }
function loadPlans() { sendToCSharp('loadPlans', {}); }
function loadAvariya() { sendToCSharp('loadAvariya', {}); }
function loadHistory() { sendToCSharp('loadHistory', {}); }
function loadStatistics() { sendToCSharp('loadStatistics'); }

function addPlan() {
    const equipment = document.getElementById('planEquipment').value;
    const tip = document.getElementById('planTip').value;
    const startDate = document.getElementById('planStartDate').value;
    const endDate = document.getElementById('planEndDate').value;
    const responsible = document.getElementById('planResponsible').value;
    const status = document.getElementById('planStatus').value;
    
    if (!equipment || !tip || !startDate || !endDate || !responsible) {
        showToast('Заполните все поля!', 'error');
        return;
    }
    
    sendToCSharp('addPlan', {
        equipment: parseInt(equipment),
        tip: parseInt(tip),
        startDate: startDate,
        endDate: endDate,
        responsible: parseInt(responsible),
        status: status
    });
    closeModal('planModal');
}

function editPlan(id) {
    const plan = allPlans.find(p => p.id === id);
    if (!plan) return;
    
    currentEditPlanId = id;
    document.getElementById('planModalTitle').innerText = 'Редактирование плана';
    
    document.getElementById('planEquipment').value = plan.equipment_id || '';
    document.getElementById('planTip').value = plan.tip_id || '';
    document.getElementById('planStartDate').value = plan.start_date;
    document.getElementById('planEndDate').value = plan.end_date;
    document.getElementById('planResponsible').value = plan.responsible_id || '';
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
}

function exportToExcel() { sendToCSharp('exportToExcel'); }
function exportToWord() { sendToCSharp('exportToWord'); }
function previewReport() { sendToCSharp('previewReport'); }

function setDefaultDates() {
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    
    const startDateFilter = document.getElementById('startDateFilter');
    const endDateFilter = document.getElementById('endDateFilter');
    const avariyaStartDate = document.getElementById('avariyaStartDate');
    const avariyaEndDate = document.getElementById('avariyaEndDate');
    const historyStartDate = document.getElementById('historyStartDate');
    const historyEndDate = document.getElementById('historyEndDate');
    
    if (startDateFilter) startDateFilter.value = monthAgo.toISOString().split('T')[0];
    if (endDateFilter) endDateFilter.value = today.toISOString().split('T')[0];
    if (avariyaStartDate) avariyaStartDate.value = monthAgo.toISOString().split('T')[0];
    if (avariyaEndDate) avariyaEndDate.value = today.toISOString().split('T')[0];
    if (historyStartDate) historyStartDate.value = monthAgo.toISOString().split('T')[0];
    if (historyEndDate) historyEndDate.value = today.toISOString().split('T')[0];
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
    document.getElementById('logoutBtn').onclick = () => sendToCSharp('logout');
    document.getElementById('applyFilterBtn').onclick = () => loadPlans();
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
        document.getElementById('planStatus').value = 'Назначена';
        document.getElementById('planModal').style.display = 'flex';
    };
    document.getElementById('savePlanBtn').onclick = () => {
        if (currentEditPlanId) {
            updatePlan();
        } else {
            addPlan();
        }
    };
    document.getElementById('exportToExcelBtn').onclick = () => exportToExcel();
    document.getElementById('exportToWordBtn').onclick = () => exportToWord();
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

document.addEventListener('DOMContentLoaded', () => {
    setupTabs();
    setupEventListeners();
    setDefaultDates();
    
    loadEquipment();
    loadTipTypes();
    loadResponsible();
    loadPlans();
    loadAvariya();
    loadHistory();
    loadStatistics();
});