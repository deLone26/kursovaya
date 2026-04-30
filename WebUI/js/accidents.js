let selectedAccidentId = -1;
let equipmentList = [];
let currentEmployeeId = 0;
let currentUserRole = '';
let currentUserFullName = '';

function setCurrentUser(id, login, role, fullName) {
    currentEmployeeId = id;
    currentUserRole = role;
    currentUserFullName = fullName;
    
    const userNameSpan = document.getElementById('userName');
    const userPanel = document.getElementById('userPanel');
    const statusGroup = document.getElementById('statusGroup');
    
    if (userNameSpan) {
        userNameSpan.innerHTML = '👤 ' + fullName;
    }
    
    if (userPanel) {
        if (role === 'app_boss' || role === 'app_admin' || role === 'boss' || role === 'admin') {
            userPanel.style.display = 'none';
        }
    }
    
    if (statusGroup) {
        if (role === 'app_operator') {
            statusGroup.style.display = 'none';
        }
    }
    
    console.log('User set:', id, fullName, role);
}

function logout() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'logout' }));
    }
}

document.addEventListener('DOMContentLoaded', function() {
    console.log('Accidents.js загружен');

    setDefaultDates();
    setupEventListeners();

    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) logoutBtn.onclick = logout;

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEquipment' }));
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadAccidents',
            startDate: document.getElementById('startDate')?.value || '',
            endDate: document.getElementById('endDate')?.value || '',
            showAll: document.getElementById('showAll')?.checked || false
        }));
    }
});

window.receiveFromCSharp = function(command, data) {
    console.log('Получено от C#:', command, data);
    try {
        switch(command) {
            case 'fillEquipment': fillEquipment(data); break;
            case 'displayAccidents': displayAccidents(data); break;
            case 'updateAccidentStatistics': updateStatistics(data); break;
            case 'showSuccess': showToast('✅ ' + data, 'success'); break;
            case 'showError': showToast('❌ ' + data, 'error'); break;
            case 'showWarning': showToast('⚠️ ' + data, 'warning'); break;
            default: console.log('Неизвестная команда:', command);
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

    const startDate = document.getElementById('startDate');
    const endDate = document.getElementById('endDate');
    const accidentDate = document.getElementById('accidentDate');
    const accidentTime = document.getElementById('accidentTime');

    if (startDate) startDate.value = formatDate(monthAgo);
    if (endDate) endDate.value = formatDate(today);
    if (accidentDate) accidentDate.value = formatDate(today);
    if (accidentTime) {
        const now = new Date();
        accidentTime.value = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
    }
}

function setupEventListeners() {
    const applyFilter = document.getElementById('applyFilterBtn');
    if (applyFilter) {
        applyFilter.addEventListener('click', () => {
            const startDate = document.getElementById('startDate')?.value || '';
            const endDate = document.getElementById('endDate')?.value || '';
            const showAll = document.getElementById('showAll')?.checked || false;
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'loadAccidents',
                    startDate: startDate,
                    endDate: endDate,
                    showAll: showAll
                }));
            }
        });
    }

    const showAll = document.getElementById('showAll');
    if (showAll) {
        showAll.addEventListener('change', function(e) {
            const start = document.getElementById('startDate');
            const end = document.getElementById('endDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'loadAccidents',
                    startDate: start?.value || '',
                    endDate: end?.value || '',
                    showAll: e.target.checked
                }));
            }
        });
    }

    const addBtn = document.getElementById('addBtn');
    if (addBtn) addBtn.addEventListener('click', addAccident);

    const updateBtn = document.getElementById('updateBtn');
    if (updateBtn) updateBtn.addEventListener('click', updateAccident);

    const deleteBtn = document.getElementById('deleteBtn');
    if (deleteBtn) deleteBtn.addEventListener('click', deleteAccident);

    const clearBtn = document.getElementById('clearBtn');
    if (clearBtn) clearBtn.addEventListener('click', clearForm);
}

function fillEquipment(data) {
    const select = document.getElementById('equipmentSelect');
    if (!select) return;
    try {
        equipmentList = typeof data === 'string' ? JSON.parse(data) : data;
        let html = '<option value="">Выберите оборудование</option>';
        if (Array.isArray(equipmentList)) {
            equipmentList.forEach(item => {
                html += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
            });
        }
        select.innerHTML = html;
    } catch (e) {
        console.error('Ошибка парсинга оборудования:', e);
    }
}

function displayAccidents(data) {
    const tbody = document.getElementById('accidentsTableBody');
    if (!tbody) return;
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
            return;
        }
        
        let displayItems = items;
        if (currentUserRole === 'app_operator') {
            displayItems = items.filter(item => item.status !== 'Завершена');
        }
        
        if (displayItems.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Нет активных аварий</td></tr>';
            return;
        }
        
        let html = '';
        displayItems.forEach(row => {
            const isSelected = selectedAccidentId == row.id;
            const statusClass = getStatusClass(row.status);
            const planSymbol = row.has_plan === '✅' ? '✅' : '❌';
            const planClass = row.has_plan === '✅' ? 'plan-badge yes' : 'plan-badge no';
            
            html += `<tr data-id="${row.id}" class="${isSelected ? 'selected' : ''}" onclick="selectAccident(${row.id})">`;
            html += `<td>${row.id || ''}</td>`;
            html += `<td>${escapeHtml(row.equipment || '')}</td>`;
            html += `<td>${row.date || ''}</td>`;
            html += `<td>${escapeHtml(row.description || '')}</td>`;
            html += `<td>${escapeHtml(row.consequences || '')}</td>`;
            html += `<td><span class="status-badge ${statusClass}">${row.status || ''}</span></td>`;
            html += `<td class="${planClass}">${planSymbol}</td>`;
            html += `</tr>`;
        });
        tbody.innerHTML = html;
        
        // Обновляем статистику после отображения
        updateStatisticsFromItems(items);
    } catch (e) {
        console.error('Ошибка отображения аварий:', e);
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Ошибка загрузки данных</td></tr>';
    }
}

function updateStatisticsFromItems(items) {
    const total = items.length;
    const inProgress = items.filter(i => i.status === 'В работе').length;
    const completed = items.filter(i => i.status === 'Завершена').length;
    const needPlan = items.filter(i => i.status === 'Требует ремонта').length;
    
    document.getElementById('totalAccidents').textContent = total;
    document.getElementById('inProgressAccidents').textContent = inProgress;
    document.getElementById('completedAccidents').textContent = completed;
    document.getElementById('needPlanAccidents').textContent = needPlan;
}

function getStatusClass(status) {
    switch(status) {
        case 'Зарегистрирована': return 'status-registered';
        case 'В работе': return 'status-work';
        case 'Завершена': return 'status-completed';
        case 'Требует ремонта': return 'status-need-repair';
        default: return '';
    }
}

function updateStatistics(data) {
    try {
        let stats = typeof data === 'string' ? JSON.parse(data) : data;
        document.getElementById('totalAccidents').textContent = stats.total || 0;
        document.getElementById('inProgressAccidents').textContent = stats.inProgress || 0;
        document.getElementById('completedAccidents').textContent = stats.completed || 0;
        document.getElementById('needPlanAccidents').textContent = stats.needPlan || 0;
    } catch (e) {
        console.error('Ошибка обновления статистики:', e);
    }
}

function selectAccident(id) {
    selectedAccidentId = parseInt(id);
    document.querySelectorAll('#accidentsTableBody tr').forEach(tr => {
        tr.classList.remove('selected');
        if (tr.getAttribute('data-id') == id) {
            tr.classList.add('selected');
            const cells = tr.cells;
            if (cells.length >= 7) {
                const equipmentName = cells[1].textContent;
                const equipmentSelect = document.getElementById('equipmentSelect');
                for (let i = 0; i < equipmentSelect.options.length; i++) {
                    if (equipmentSelect.options[i].text === equipmentName) {
                        equipmentSelect.selectedIndex = i;
                        break;
                    }
                }
                const dateTime = cells[2].textContent;
                if (dateTime) {
                    const parts = dateTime.split(' ');
                    if (parts.length >= 2) {
                        const dateInput = document.getElementById('accidentDate');
                        const timeInput = document.getElementById('accidentTime');
                        if (dateInput) dateInput.value = parts[0];
                        if (timeInput) timeInput.value = parts[1];
                    }
                }
                const description = document.getElementById('description');
                const consequences = document.getElementById('consequences');
                if (description) description.value = cells[3].textContent;
                if (consequences) consequences.value = cells[4].textContent;
            }
        }
    });
}

function addAccident() {
    const equipment = document.getElementById('equipmentSelect')?.value;
    const date = document.getElementById('accidentDate')?.value;
    const time = document.getElementById('accidentTime')?.value;
    const description = document.getElementById('description')?.value.trim();
    const consequences = document.getElementById('consequences')?.value.trim();

    if (!equipment) { showToast('Выберите оборудование!', 'warning'); return; }
    if (!description) { showToast('Введите описание аварии!', 'warning'); return; }
    if (!date) { showToast('Выберите дату аварии!', 'warning'); return; }
    if (!time) { showToast('Выберите время аварии!', 'warning'); return; }

    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'addAccident',
            equipment: parseInt(equipment),
            date: date,
            time: time,
            description: description,
            consequences: consequences || ''
        }));
    }
}

function updateAccident() {
    if (selectedAccidentId === -1) { showToast('Выберите запись для обновления!', 'warning'); return; }
    if (!confirm('Обновить выбранную запись?')) return;

    const equipment = document.getElementById('equipmentSelect')?.value;
    const date = document.getElementById('accidentDate')?.value;
    const time = document.getElementById('accidentTime')?.value;
    const description = document.getElementById('description')?.value.trim();
    const consequences = document.getElementById('consequences')?.value.trim();

    if (!equipment) { showToast('Выберите оборудование!', 'warning'); return; }
    if (!description) { showToast('Введите описание аварии!', 'warning'); return; }

    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'updateAccident',
            id: selectedAccidentId,
            equipment: parseInt(equipment),
            date: date,
            time: time,
            description: description,
            consequences: consequences || ''
        }));
    }
}

function deleteAccident() {
    if (selectedAccidentId === -1) { showToast('Выберите запись для удаления!', 'warning'); return; }
    if (!confirm('Удалить выбранную запись?')) return;
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'deleteAccident',
            id: selectedAccidentId
        }));
    }
}

function clearForm() {
    selectedAccidentId = -1;
    const equipment = document.getElementById('equipmentSelect');
    const description = document.getElementById('description');
    const consequences = document.getElementById('consequences');
    if (equipment) equipment.selectedIndex = 0;
    if (description) description.value = '';
    if (consequences) consequences.value = '';
    setDefaultDates();
    document.querySelectorAll('#accidentsTableBody tr').forEach(tr => tr.classList.remove('selected'));
}

function showToast(message, type = 'info') {
    const toast = document.getElementById('toastMessage');
    if (!toast) return;
    toast.textContent = message;
    toast.className = `toast ${type}`;
    toast.style.display = 'block';
    setTimeout(() => { toast.style.display = 'none'; }, 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}