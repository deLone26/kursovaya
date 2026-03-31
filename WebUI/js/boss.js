// Глобальные переменные
let selectedPlanId = -1;
let selectedAvariyaId = -1;

console.log('BOSS.JS ЗАГРУЖЕН');

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM загружен');
    
    setDefaultDates();
    setupEventListeners();
    
    // Проверяем связь с C#
    if (window.chrome && window.chrome.webview) {
        console.log('WebView доступен');
        window.chrome.webview.postMessage('loadEquipment');
        window.chrome.webview.postMessage('loadTipTypes');
        window.chrome.webview.postMessage('loadResponsible');
        window.chrome.webview.postMessage('loadStatistics');
    }
});

// Получение сообщений от C#
window.receiveFromCSharp = function(command, data) {
    console.log('Получено от C#:', command, data);
    
    try {
        switch(command) {
            case 'fillEquipment':
                fillEquipment(data);
                break;
            case 'fillTipTypes':
                fillTipTypes(data);
                break;
            case 'fillResponsible':
                fillResponsible(data);
                break;
            case 'displayPlans':
                displayPlans(data);
                break;
            case 'displayAvariya':
                displayAvariya(data);
                break;
            case 'updateStatistics':
                updateStatistics(data);
                break;
            case 'showSuccess':
                alert('✅ ' + data);
                break;
            case 'showError':
                alert('❌ ' + data);
                break;
            default:
                console.log('Неизвестная команда:', command);
        }
    } catch (error) {
        console.error('Ошибка обработки:', error);
    }
};

function setDefaultDates() {
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    
    const formatDate = (date) => date.toISOString().split('T')[0];
    
    const planStart = document.getElementById('planStartDate');
    const planEnd = document.getElementById('planEndDate');
    const avariyaStart = document.getElementById('avariyaStartDate');
    const avariyaEnd = document.getElementById('avariyaEndDate');
    const start = document.getElementById('startDate');
    const end = document.getElementById('endDate');
    
    if (planStart) planStart.value = formatDate(monthAgo);
    if (planEnd) planEnd.value = formatDate(today);
    if (avariyaStart) avariyaStart.value = formatDate(monthAgo);
    if (avariyaEnd) avariyaEnd.value = formatDate(today);
    
    if (start) start.value = formatDate(today);
    
    const nextWeek = new Date();
    nextWeek.setDate(nextWeek.getDate() + 7);
    if (end) end.value = formatDate(nextWeek);
}

function setupEventListeners() {
    // Табы
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            
            this.classList.add('active');
            const tabId = this.dataset.tab;
            const tabElement = document.getElementById(`tab-${tabId}`);
            if (tabElement) tabElement.classList.add('active');
            
            if (tabId === 'plans') {
                if (window.chrome?.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({
                        action: 'loadPlans',
                        startDate: document.getElementById('planStartDate')?.value || '',
                        endDate: document.getElementById('planEndDate')?.value || '',
                        showAll: document.getElementById('showAllPlans')?.checked || false
                    }));
                }
            } else if (tabId === 'avariya') {
                if (window.chrome?.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({
                        action: 'loadAvariya',
                        startDate: document.getElementById('avariyaStartDate')?.value || '',
                        endDate: document.getElementById('avariyaEndDate')?.value || '',
                        showAll: document.getElementById('showAllAvariya')?.checked || false
                    }));
                }
            }
        });
    });
    
    // Фильтры планов
    const applyPlanFilter = document.getElementById('applyPlanFilter');
    if (applyPlanFilter) {
        applyPlanFilter.addEventListener('click', () => {
            const startDate = document.getElementById('planStartDate')?.value || '';
            const endDate = document.getElementById('planEndDate')?.value || '';
            const showAll = document.getElementById('showAllPlans')?.checked || false;
            
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'loadPlans',
                    startDate: startDate,
                    endDate: endDate,
                    showAll: showAll
                }));
            }
        });
    }
    
    // Фильтры аварий
    const applyAvariyaFilter = document.getElementById('applyAvariyaFilter');
    if (applyAvariyaFilter) {
        applyAvariyaFilter.addEventListener('click', () => {
            const startDate = document.getElementById('avariyaStartDate')?.value || '';
            const endDate = document.getElementById('avariyaEndDate')?.value || '';
            const showAll = document.getElementById('showAllAvariya')?.checked || false;
            
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'loadAvariya',
                    startDate: startDate,
                    endDate: endDate,
                    showAll: showAll
                }));
            }
        });
    }
    
    // Кнопки CRUD
    const addBtn = document.getElementById('addPlanBtn');
    if (addBtn) addBtn.addEventListener('click', addPlan);
    
    const updateBtn = document.getElementById('updatePlanBtn');
    if (updateBtn) updateBtn.addEventListener('click', updatePlan);
    
    const deleteBtn = document.getElementById('deletePlanBtn');
    if (deleteBtn) deleteBtn.addEventListener('click', deletePlan);
    
    const clearBtn = document.getElementById('clearFormBtn');
    if (clearBtn) clearBtn.addEventListener('click', clearForm);
    
    // Кнопка создания плана из аварии
    const createFromAvariya = document.getElementById('createPlanFromAvariya');
    if (createFromAvariya) {
        createFromAvariya.addEventListener('click', () => {
            if (selectedAvariyaId === -1) {
                alert('Выберите аварию!');
                return;
            }
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'createPlanFromAvariya',
                    id: selectedAvariyaId
                }));
            }
        });
    }
    
    // Отчеты
    const exportExcel = document.getElementById('exportExcelBtn');
    if (exportExcel) {
        exportExcel.addEventListener('click', () => {
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({ action: 'exportToExcel' }));
            }
        });
    }
    
    const exportWord = document.getElementById('exportWordBtn');
    if (exportWord) {
        exportWord.addEventListener('click', () => {
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({ action: 'exportToWord' }));
            }
        });
    }
    
    const previewReport = document.getElementById('previewReportBtn');
    if (previewReport) {
        previewReport.addEventListener('click', () => {
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({ action: 'previewReport' }));
            }
        });
    }
    
    // Чекбоксы
    const showAllPlans = document.getElementById('showAllPlans');
    if (showAllPlans) {
        showAllPlans.addEventListener('change', function(e) {
            const start = document.getElementById('planStartDate');
            const end = document.getElementById('planEndDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
        });
    }
    
    const showAllAvariya = document.getElementById('showAllAvariya');
    if (showAllAvariya) {
        showAllAvariya.addEventListener('change', function(e) {
            const start = document.getElementById('avariyaStartDate');
            const end = document.getElementById('avariyaEndDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
        });
    }
}

function fillEquipment(data) {
    console.log('Заполнение оборудования:', data);
    const select = document.getElementById('equipmentSelect');
    if (!select) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        let html = '<option value="">Выберите оборудование</option>';
        
        if (Array.isArray(items)) {
            items.forEach(item => {
                html += `<option value="${item.id}">${item.name}</option>`;
            });
        }
        
        select.innerHTML = html;
    } catch (e) {
        console.error('Ошибка парсинга оборудования:', e);
    }
}

function fillTipTypes(data) {
    console.log('Заполнение типов ТО:', data);
    const select = document.getElementById('tipSelect');
    if (!select) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        let html = '<option value="">Выберите тип ТО</option>';
        
        if (Array.isArray(items)) {
            items.forEach(item => {
                html += `<option value="${item.id}">${item.name}</option>`;
            });
        }
        
        select.innerHTML = html;
    } catch (e) {
        console.error('Ошибка парсинга типов ТО:', e);
    }
}

function fillResponsible(data) {
    console.log('Заполнение ответственных:', data);
    const select = document.getElementById('responsibleSelect');
    if (!select) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        let html = '<option value="">Выберите ответственного</option>';
        
        if (Array.isArray(items)) {
            items.forEach(item => {
                html += `<option value="${item.id}">${item.name}</option>`;
            });
        }
        
        select.innerHTML = html;
    } catch (e) {
        console.error('Ошибка парсинга ответственных:', e);
    }
}

function displayPlans(data) {
    console.log('Отображение планов:', data);
    const tbody = document.getElementById('plansTableBody');
    if (!tbody) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="loading">Нет данных</td></tr>';
            return;
        }
        
        let html = '';
        items.forEach(row => {
            html += `<tr onclick="selectPlan(${row.id})" style="cursor: pointer;">`;
            html += `<td>${row.id || ''}</td>`;
            html += `<td>${row.equipment || ''}</td>`;
            html += `<td>${row.tip || ''}</td>`;
            html += `<td>${row.start_date || ''}</td>`;
            html += `<td>${row.end_date || ''}</td>`;
            html += `<td>${row.responsible || ''}</td>`;
            html += `<td>${row.status || ''}</td>`;
            html += `<td>${row.avariya_id > 0 ? row.avariya_id : '-'}</td>`;
            html += `<td>${row.cost || '0.00'}</td>`;
            html += '</tr>';
        });
        tbody.innerHTML = html;
    } catch (e) {
        console.error('Ошибка отображения планов:', e);
        tbody.innerHTML = '<tr><td colspan="9" class="loading">Ошибка загрузки данных</td></tr>';
    }
}

function displayAvariya(data) {
    console.log('Отображение аварий:', data);
    const tbody = document.getElementById('avariyaTableBody');
    if (!tbody) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
            return;
        }
        
        let html = '';
        items.forEach(row => {
            const bgColor = row.has_plan === '✅' ? '#e8f5e8' : '#ffebee';
            html += `<tr onclick="selectAvariya(${row.id})" style="cursor: pointer; background-color: ${bgColor}">`;
            html += `<td>${row.id || ''}</td>`;
            html += `<td>${row.equipment || ''}</td>`;
            html += `<td>${row.date || ''}</td>`;
            html += `<td>${row.description || ''}</td>`;
            html += `<td>${row.consequences || ''}</td>`;
            html += `<td>${row.status || ''}</td>`;
            html += `<td>${row.has_plan || ''}</td>`;
            html += '</tr>';
        });
        tbody.innerHTML = html;
    } catch (e) {
        console.error('Ошибка отображения аварий:', e);
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Ошибка загрузки данных</td></tr>';
    }
}

function updateStatistics(data) {
    console.log('Обновление статистики:', data);
    try {
        let stats = typeof data === 'string' ? JSON.parse(data) : data;
        
        const totalEquipment = document.getElementById('totalEquipment');
        const totalAvariya = document.getElementById('totalAvariya');
        const totalPlans = document.getElementById('totalPlans');
        const completedPlans = document.getElementById('completedPlans');
        const totalCost = document.getElementById('totalCost');
        const monthlyCost = document.getElementById('monthlyCost');
        
        if (totalEquipment) totalEquipment.textContent = stats.totalEquipment || 0;
        if (totalAvariya) totalAvariya.textContent = stats.totalAvariya || 0;
        if (totalPlans) totalPlans.textContent = stats.totalPlans || 0;
        if (completedPlans) completedPlans.textContent = stats.completedPlans || 0;
        if (totalCost) totalCost.textContent = stats.totalCost || '0.00';
        if (monthlyCost) monthlyCost.textContent = stats.monthlyCost || '0.00';
    } catch (e) {
        console.error('Ошибка обновления статистики:', e);
    }
}

function selectPlan(id) {
    selectedPlanId = id;
    document.querySelectorAll('#plansTable tbody tr').forEach(tr => {
        tr.classList.remove('selected');
    });
    if (event && event.currentTarget) {
        event.currentTarget.classList.add('selected');
    }
}

function selectAvariya(id) {
    selectedAvariyaId = id;
    document.querySelectorAll('#avariyaTable tbody tr').forEach(tr => {
        tr.classList.remove('selected');
    });
    if (event && event.currentTarget) {
        event.currentTarget.classList.add('selected');
    }
}

function selectEquipmentById(id) {
    const select = document.getElementById('equipmentSelect');
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
    const plansTab = document.querySelector('[data-tab="plans"]');
    if (plansTab) {
        plansTab.click();
    }
}

function addPlan() {
    const equipment = document.getElementById('equipmentSelect')?.value;
    const tip = document.getElementById('tipSelect')?.value;
    const startDate = document.getElementById('startDate')?.value;
    const endDate = document.getElementById('endDate')?.value;
    const responsible = document.getElementById('responsibleSelect')?.value;
    const status = document.getElementById('statusSelect')?.value;
    const cost = document.getElementById('cost')?.value;
    
    if (!equipment || !tip || !responsible || !startDate || !endDate || !cost) {
        alert('Заполните все поля!');
        return;
    }
    
    if (new Date(startDate) > new Date(endDate)) {
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
            equipment: parseInt(equipment),
            tip: parseInt(tip),
            startDate: startDate,
            endDate: endDate,
            responsible: parseInt(responsible),
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
    
    const equipment = document.getElementById('equipmentSelect')?.value;
    const tip = document.getElementById('tipSelect')?.value;
    const startDate = document.getElementById('startDate')?.value;
    const endDate = document.getElementById('endDate')?.value;
    const responsible = document.getElementById('responsibleSelect')?.value;
    const status = document.getElementById('statusSelect')?.value;
    const cost = document.getElementById('cost')?.value;
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'updatePlan',
            id: selectedPlanId,
            equipment: parseInt(equipment),
            tip: parseInt(tip),
            startDate: startDate,
            endDate: endDate,
            responsible: parseInt(responsible),
            status: status,
            cost: parseFloat(cost)
        }));
    }
}

function updateStatistics(data) {
    console.log('Обновление статистики:', data);
    try {
        let stats = typeof data === 'string' ? JSON.parse(data) : data;
        
        const totalEquipment = document.getElementById('totalEquipment');
        const totalAvariya = document.getElementById('totalAvariya');
        const totalPlans = document.getElementById('totalPlans');
        const completedPlans = document.getElementById('completedPlans');
        const overduePlans = document.getElementById('overduePlans');
        const totalCost = document.getElementById('totalCost');
        const monthlyCost = document.getElementById('monthlyCost');
        
        if (totalEquipment) totalEquipment.textContent = stats.totalEquipment || 0;
        if (totalAvariya) totalAvariya.textContent = stats.totalAvariya || 0;
        if (totalPlans) totalPlans.textContent = stats.totalPlans || 0;
        if (completedPlans) completedPlans.textContent = stats.completedPlans || 0;
        if (overduePlans) overduePlans.textContent = stats.overduePlans || 0;
        if (totalCost) totalCost.textContent = stats.totalCost || '0.00';
        if (monthlyCost) monthlyCost.textContent = stats.monthlyCost || '0.00';
        
        // Подсветка карточки просрочки, если есть просроченные планы
        const overdueCard = document.querySelector('.stat-card[style*="background: #ef4444"]');
        if (overdueCard && stats.overduePlans > 0) {
            overdueCard.style.animation = 'pulse 1s infinite';
        }
    } catch (e) {
        console.error('Ошибка обновления статистики:', e);
    }
}

function deletePlan() {
    if (selectedPlanId === -1) {
        alert('Выберите план для удаления!');
        return;
    }
    
    if (!confirm('Удалить выбранный план?')) return;
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'deletePlan',
            id: selectedPlanId
        }));
    }
}

function clearForm() {
    selectedPlanId = -1;
    
    const equipment = document.getElementById('equipmentSelect');
    const tip = document.getElementById('tipSelect');
    const responsible = document.getElementById('responsibleSelect');
    const status = document.getElementById('statusSelect');
    const cost = document.getElementById('cost');
    
    if (equipment) equipment.selectedIndex = 0;
    if (tip) tip.selectedIndex = 0;
    if (responsible) responsible.selectedIndex = 0;
    if (status) status.selectedIndex = 0;
    if (cost) cost.value = '';
    
    setDefaultDates();
    
    document.querySelectorAll('#plansTable tbody tr').forEach(tr => {
        tr.classList.remove('selected');
    });
}