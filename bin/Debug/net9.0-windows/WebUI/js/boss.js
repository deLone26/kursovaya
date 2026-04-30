let selectedPlanId = -1;
let selectedAvariyaId = -1;
let currentTab = 'plans';

function switchStatsPanel(tab) {
    const statsPlans = document.getElementById('statsPlans');
    const statsAvariya = document.getElementById('statsAvariya');
    const statsHistory = document.getElementById('statsHistory');
    
    if (statsPlans) statsPlans.style.display = 'none';
    if (statsAvariya) statsAvariya.style.display = 'none';
    if (statsHistory) statsHistory.style.display = 'none';
    
    if (tab === 'plans' && statsPlans) {
        statsPlans.style.display = 'grid';
        if (window.lastUncompletedCost) {
            const plannedCostEl = document.getElementById('plannedCost');
            if (plannedCostEl) plannedCostEl.innerText = window.lastUncompletedCost;
        }
    } else if (tab === 'avariya' && statsAvariya) {
        statsAvariya.style.display = 'grid';
    } else if (tab === 'history' && statsHistory) {
        statsHistory.style.display = 'grid';
        if (window.lastHistoryTotalCost) {
            const historyTotalCostEl = document.getElementById('historyTotalCost');
            if (historyTotalCostEl) historyTotalCostEl.innerText = window.lastHistoryTotalCost + ' руб.';
        }
        if (window.lastHistoryTotalCount) {
            const historyTotalCountEl = document.getElementById('historyTotalCount');
            if (historyTotalCountEl) historyTotalCountEl.innerText = window.lastHistoryTotalCount;
        }
    }
}

function el(id) {
    return document.getElementById(id);
}

document.addEventListener('DOMContentLoaded', function() {
    console.log('BOSS.JS загружен');
    setDefaultDates();
    setupEventListeners();
    switchStatsPanel('plans');
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage('loadEquipment');
        window.chrome.webview.postMessage('loadTipTypes');
        window.chrome.webview.postMessage('loadResponsible');
        window.chrome.webview.postMessage('loadStatistics');
    }
});

window.receiveFromCSharp = function(command, data) {
    console.log('Получено от C#:', command);
    try {
        switch(command) {
            case 'fillEquipment': fillEquipment(data); break;
            case 'fillTipTypes': fillTipTypes(data); break;
            case 'fillResponsible': fillResponsible(data); break;
            case 'displayPlans': displayPlans(data); break;
            case 'displayAvariya': displayAvariya(data); break;
            case 'displayHistory': displayHistory(data); break;
            case 'updateStatistics': updateStatistics(data); break;
            case 'showSuccess': alert('✅ ' + data); break;
            case 'showError': alert('❌ ' + data); break;
            default: console.log('Неизвестная команда:', command);
        }
    } catch (error) { console.error('Ошибка:', error); }
};

function setDefaultDates() {
    const today = new Date();
    const monthAgo = new Date(); 
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    const formatDate = (date) => date.toISOString().split('T')[0];
    
    const planStart = el('planStartDate');
    const planEnd = el('planEndDate');
    const avariyaStart = el('avariyaStartDate');
    const avariyaEnd = el('avariyaEndDate');
    const historyStart = el('historyStartDate');
    const historyEnd = el('historyEndDate');
    const start = el('startDate');
    const end = el('endDate');
    
    if (planStart) planStart.value = formatDate(monthAgo);
    if (planEnd) planEnd.value = formatDate(today);
    if (avariyaStart) avariyaStart.value = formatDate(monthAgo);
    if (avariyaEnd) avariyaEnd.value = formatDate(today);
    if (historyStart) historyStart.value = formatDate(monthAgo);
    if (historyEnd) historyEnd.value = formatDate(today);
    if (start) start.value = formatDate(today);
    
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    if (end) end.value = formatDate(nextWeek);
}

function setupEventListeners() {
    // Переключение вкладок
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            this.classList.add('active');
            const tabId = this.dataset.tab;
            const tabElement = document.getElementById(`tab-${tabId}`);
            if (tabElement) tabElement.classList.add('active');
            currentTab = tabId;
            switchStatsPanel(tabId);
            
            if (tabId === 'plans') {
                loadPlans(el('planStartDate')?.value || '', el('planEndDate')?.value || '', el('showAllPlans')?.checked || false);
            } else if (tabId === 'avariya') {
                loadAvariya(el('avariyaStartDate')?.value || '', el('avariyaEndDate')?.value || '', el('showAllAvariya')?.checked || false);
            } else if (tabId === 'history') {
                loadHistory(el('historyStartDate')?.value || '', el('historyEndDate')?.value || '');
            }
        });
    });
    
    // Фильтры
    const applyPlanFilter = el('applyPlanFilter');
    if (applyPlanFilter) applyPlanFilter.onclick = () => loadPlans(el('planStartDate')?.value, el('planEndDate')?.value, el('showAllPlans')?.checked);
    
    const applyAvariyaFilter = el('applyAvariyaFilter');
    if (applyAvariyaFilter) applyAvariyaFilter.onclick = () => loadAvariya(el('avariyaStartDate')?.value, el('avariyaEndDate')?.value, el('showAllAvariya')?.checked);
    
    const applyHistoryFilter = el('applyHistoryFilter');
    if (applyHistoryFilter) applyHistoryFilter.onclick = () => loadHistory(el('historyStartDate')?.value, el('historyEndDate')?.value);
    
    // CRUD кнопки
    const addPlanBtn = el('addPlanBtn');
    if (addPlanBtn) addPlanBtn.onclick = addPlan;
    
    const updatePlanBtn = el('updatePlanBtn');
    if (updatePlanBtn) updatePlanBtn.onclick = updatePlan;
    
    const deletePlanBtn = el('deletePlanBtn');
    if (deletePlanBtn) deletePlanBtn.onclick = deletePlan;
    
    const clearFormBtn = el('clearFormBtn');
    if (clearFormBtn) clearFormBtn.onclick = clearForm;
    
    // Создание плана из аварии
    const createFromAvariya = el('createPlanFromAvariya');
    if (createFromAvariya) {
        createFromAvariya.onclick = () => {
            if (selectedAvariyaId === -1) { alert('Выберите аварию!'); return; }
            if (window.chrome?.webview) window.chrome.webview.postMessage(JSON.stringify({ action: 'createPlanFromAvariya', id: selectedAvariyaId }));
        };
    }
    
    // Отчеты
    const exportExcelBtn = el('exportExcelBtn');
    if (exportExcelBtn) exportExcelBtn.onclick = () => window.chrome.webview.postMessage(JSON.stringify({ action: 'exportToExcel' }));
    
    const exportWordBtn = el('exportWordBtn');
    if (exportWordBtn) exportWordBtn.onclick = () => window.chrome.webview.postMessage(JSON.stringify({ action: 'exportToWord' }));
    
    const previewReportBtn = el('previewReportBtn');
    if (previewReportBtn) previewReportBtn.onclick = () => window.chrome.webview.postMessage(JSON.stringify({ action: 'previewReport' }));
    
    // Чекбоксы
    const showAllPlans = el('showAllPlans');
    if (showAllPlans) {
        showAllPlans.onchange = (e) => {
            const start = el('planStartDate');
            const end = el('planEndDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
            loadPlans(start?.value || '', end?.value || '', e.target.checked);
        };
    }
    
    const showAllAvariya = el('showAllAvariya');
    if (showAllAvariya) {
        showAllAvariya.onchange = (e) => {
            const start = el('avariyaStartDate');
            const end = el('avariyaEndDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
            loadAvariya(start?.value || '', end?.value || '', e.target.checked);
        };
    }
}

function loadPlans(startDate, endDate, showAll) {
    if (window.chrome?.webview) window.chrome.webview.postMessage(JSON.stringify({ action: 'loadPlans', startDate: startDate || '', endDate: endDate || '', showAll: showAll || false }));
}

function loadAvariya(startDate, endDate, showAll) {
    if (window.chrome?.webview) window.chrome.webview.postMessage(JSON.stringify({ action: 'loadAvariya', startDate: startDate || '', endDate: endDate || '', showAll: showAll || false }));
}

function loadHistory(startDate, endDate) {
    if (window.chrome?.webview) window.chrome.webview.postMessage(JSON.stringify({ action: 'loadHistory', startDate: startDate || '', endDate: endDate || '' }));
}

function fillEquipment(data) {
    const select = el('equipmentSelect');
    if (!select) return;
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    let html = '<option value="">Выберите оборудование</option>';
    if (Array.isArray(items)) {
        items.forEach(item => {
            html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
        });
    }
    select.innerHTML = html;
}

function fillTipTypes(data) {
    const select = el('tipSelect');
    if (!select) return;
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    let html = '<option value="">Выберите тип ТО</option>';
    if (Array.isArray(items)) {
        items.forEach(item => {
            html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
        });
    }
    select.innerHTML = html;
}

function fillResponsible(data) {
    const select = el('responsibleSelect');
    if (!select) return;
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    let html = '<option value="">Выберите ответственного</option>';
    if (Array.isArray(items)) {
        items.forEach(item => {
            html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
        });
    }
    select.innerHTML = html;
}

function displayPlans(data) {
    const tbody = el('plansTableBody');
    if (!tbody) return;
    try {
        let response = typeof data === 'string' ? JSON.parse(data) : data;
        let items = response.plans || [];
        let uncompletedCost = response.uncompletedCost || '0.00';
        window.lastUncompletedCost = uncompletedCost;
        
        const plannedCostEl = el('plannedCost');
        if (plannedCostEl) plannedCostEl.innerText = uncompletedCost;
        
        if (!items.length) { 
            tbody.innerHTML = '<tr><td colspan="9" class="loading">Нет данных</td></tr>'; 
            return; 
        }
        
        items.sort((a,b) => {
            if (a.responsible !== b.responsible) return a.responsible.localeCompare(b.responsible);
            return new Date(a.start_date.split('.').reverse().join('-')) - new Date(b.start_date.split('.').reverse().join('-'));
        });
        
        let html = '';
        let lastResponsible = '';
        let taskIndex = 0;
        
        for (let row of items) {
            const isCompleted = (row.status === '✅ Завершен' || row.status === 'Завершен');
            const rowClass = isCompleted ? 'completed-row' : '';
            if (lastResponsible !== row.responsible) { 
                lastResponsible = row.responsible; 
                taskIndex = 0; 
            }
            const indent = taskIndex * 20;
            html += `<tr onclick="selectPlan(${row.id})" class="${rowClass}" style="cursor:pointer;">`;
            html += `<td style="padding-left:${indent}px;">${row.id}</td>`;
            html += `<td>${escapeHtml(row.equipment)}</td>`;
            html += `<td>${escapeHtml(row.tip)}</td>`;
            html += `<td>${row.start_date}</td>`;
            html += `<td>${row.end_date}</td>`;
            html += `<td>${escapeHtml(row.responsible)}</td>`;
            html += `<td>${row.status}</td>`;
            html += `<td>${row.avariya_id > 0 ? row.avariya_id : '-'}</td>`;
            html += `<td>${row.cost}</td>`;
            html += `</tr>`;
            taskIndex++;
        }
        tbody.innerHTML = html;
    } catch(e) { 
        console.error(e); 
        tbody.innerHTML = '<tr><td colspan="9" class="loading">Ошибка загрузки</td></tr>'; 
    }
}

function displayAvariya(data) {
    const tbody = el('avariyaTableBody');
    if (!tbody) return;
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        if (!items.length) { 
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>'; 
            return; 
        }
        let html = '';
        for (let row of items) {
            html += `<tr onclick="selectAvariya(${row.id})" style="cursor:pointer;${row.has_plan === '✅' ? 'background:#e8f5e8' : ''}">`;
            html += `<td>${row.id}</td>`;
            html += `<td>${escapeHtml(row.equipment)}</td>`;
            html += `<td>${row.date}</td>`;
            html += `<td>${escapeHtml(row.description)}</td>`;
            html += `<td>${escapeHtml(row.consequences)}</td>`;
            html += `<td>${row.status}</td>`;
            html += `<td>${row.has_plan}</td>`;
            html += `</tr>`;
        }
        tbody.innerHTML = html;
    } catch(e) { 
        console.error(e); 
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Ошибка загрузки</td></tr>'; 
    }
}

function displayHistory(data) {
    const tbody = el('historyTableBody');
    if (!tbody) return;
    try {
        let response = typeof data === 'string' ? JSON.parse(data) : data;
        let items = response.history || [];
        let totalCost = response.totalCost || '0.00';
        let totalCount = response.totalCount || 0;
        
        window.lastHistoryTotalCost = totalCost;
        window.lastHistoryTotalCount = totalCount;
        
        const historyTotalCostEl = el('historyTotalCost');
        if (historyTotalCostEl) historyTotalCostEl.innerText = totalCost + ' руб.';
        
        const historyTotalCountEl = el('historyTotalCount');
        if (historyTotalCountEl) historyTotalCountEl.innerText = totalCount;
        
        if (!items.length) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет записей в истории</td></tr>';
            return;
        }
        let html = '';
        for (let row of items) {
            html += `<tr>`;
            html += `<td>${escapeHtml(row.equipment_name || '')}</td>`;
            html += `<td>${escapeHtml(row.tip_name || '')}</td>`;
            html += `<td>${row.plan_date || ''}</td>`;
            html += `<td>${row.completed_date || ''}</td>`;
            html += `<td>${escapeHtml(row.sotrudnik_name || '')}</td>`;
            html += `<td>${escapeHtml(row.opisanie || '')}</td>`;
            html += `<td>${row.cost || '0.00'} руб.</td>`;
            html += `</tr>`;
        }
        tbody.innerHTML = html;
    } catch(e) { 
        console.error(e); 
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Ошибка загрузки данных</td></tr>'; 
    }
}

function updateStatistics(data) {
    try {
        let stats = typeof data === 'string' ? JSON.parse(data) : data;
        const totalEquipment = el('totalEquipment');
        const totalPlans = el('totalPlans');
        const completedPlans = el('completedPlans');
        const overduePlans = el('overduePlans');
        const inProgressPlans = el('inProgressPlans');
        const totalCost = el('totalCost');
        const totalAvariya = el('totalAvariya');
        const inProgressAvariya = el('inProgressAvariya');
        const completedAvariya = el('completedAvariya');
        const needPlanAvariya = el('needPlanAvariya');
        
        if (totalEquipment) totalEquipment.innerText = stats.totalEquipment || 0;
        if (totalPlans) totalPlans.innerText = stats.totalPlans || 0;
        if (completedPlans) completedPlans.innerText = stats.completedPlans || 0;
        if (overduePlans) overduePlans.innerText = stats.overduePlans || 0;
        if (inProgressPlans) inProgressPlans.innerText = stats.inProgressPlans || 0;
        if (totalCost) totalCost.innerText = stats.totalCost || '0.00';
        if (totalAvariya) totalAvariya.innerText = stats.totalAvariya || 0;
        if (inProgressAvariya) inProgressAvariya.innerText = stats.inProgressAvariya || 0;
        if (completedAvariya) completedAvariya.innerText = stats.completedAvariya || 0;
        if (needPlanAvariya) needPlanAvariya.innerText = stats.needPlanAvariya || 0;
    } catch(e) { console.error(e); }
}

function selectPlan(id) { 
    selectedPlanId = id; 
    // Визуальное выделение строки
    document.querySelectorAll('#plansTable tbody tr').forEach(tr => tr.classList.remove('selected'));
    if (event && event.currentTarget) event.currentTarget.classList.add('selected');
}

function selectAvariya(id) { 
    selectedAvariyaId = id; 
    document.querySelectorAll('#avariyaTable tbody tr').forEach(tr => tr.classList.remove('selected'));
    if (event && event.currentTarget) event.currentTarget.classList.add('selected');
}

function selectEquipmentById(id) { 
    const select = el('equipmentSelect'); 
    if (select) { 
        for (let i = 0; i < select.options.length; i++) { 
            if (select.options[i].value == id) { 
                select.selectedIndex = i; 
                break; 
            } 
        } 
    } 
}

function switchToPlansTab() { 
    const tab = document.querySelector('[data-tab="plans"]'); 
    if (tab) tab.click(); 
}

function addPlan() {
    const eq = el('equipmentSelect')?.value;
    const tip = el('tipSelect')?.value;
    const start = el('startDate')?.value;
    const end = el('endDate')?.value;
    const resp = el('responsibleSelect')?.value;
    const status = el('statusSelect')?.value;
    const cost = el('cost')?.value;
    
    if (!eq || !tip || !resp || !start || !end || !cost) { 
        alert('Заполните все поля!'); 
        return; 
    }
    if (new Date(start) > new Date(end)) { 
        alert('Дата начала не может быть позже даты окончания!'); 
        return; 
    }
    if (isNaN(parseFloat(cost)) || parseFloat(cost) <= 0) { 
        alert('Введите корректную стоимость!'); 
        return; 
    }
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'addPlan', 
            equipment: parseInt(eq), 
            tip: parseInt(tip), 
            startDate: start, 
            endDate: end, 
            responsible: parseInt(resp), 
            status: status, 
            cost: parseFloat(cost) 
        }));
    }
}

function updatePlan() {
    if (selectedPlanId === -1) { 
        alert('Выберите план для обновления!'); 
        return; 
    }
    if (!confirm('Обновить выбранный план?')) return;
    
    const eq = el('equipmentSelect')?.value;
    const tip = el('tipSelect')?.value;
    const start = el('startDate')?.value;
    const end = el('endDate')?.value;
    const resp = el('responsibleSelect')?.value;
    const status = el('statusSelect')?.value;
    const cost = el('cost')?.value;
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'updatePlan', 
            id: selectedPlanId, 
            equipment: parseInt(eq), 
            tip: parseInt(tip), 
            startDate: start, 
            endDate: end, 
            responsible: parseInt(resp), 
            status: status, 
            cost: parseFloat(cost) 
        }));
    }
}

function deletePlan() {
    if (selectedPlanId === -1) { 
        alert('Выберите план для удаления!'); 
        return; 
    }
    if (!confirm('Удалить выбранный план?')) return;
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'deletePlan', id: selectedPlanId }));
    }
}

function clearForm() {
    selectedPlanId = -1;
    const equipment = el('equipmentSelect');
    const tip = el('tipSelect');
    const responsible = el('responsibleSelect');
    const status = el('statusSelect');
    const cost = el('cost');
    
    if (equipment) equipment.selectedIndex = 0;
    if (tip) tip.selectedIndex = 0;
    if (responsible) responsible.selectedIndex = 0;
    if (status) status.selectedIndex = 0;
    if (cost) cost.value = '';
    setDefaultDates();
    document.querySelectorAll('#plansTable tbody tr').forEach(tr => tr.classList.remove('selected'));
}

function escapeHtml(s) { 
    if (!s) return ''; 
    return s.replace(/[&<>]/g, function(m) { 
        if (m === '&') return '&amp;'; 
        if (m === '<') return '&lt;'; 
        if (m === '>') return '&gt;'; 
        return m; 
    }); 
}