// ================== ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ==================
let equipmentData = [];
let statusesData = [];
let selectedEquipmentId = -1;

// ================== ИНИЦИАЛИЗАЦИЯ ==================
document.addEventListener('DOMContentLoaded', function() {
    loadEquipment();
    loadStatuses();
    
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchEquipment();
            }
        });
    }
});

// ================== ЗАГРУЗКА ОБОРУДОВАНИЯ ==================
function loadEquipment(filter = '') {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadEquipment',
            filter: filter
        }));
    }
}

// ================== ЗАГРУЗКА СТАТУСОВ ==================
function loadStatuses() {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadStatuses'
        }));
    }
}

// ================== ОБНОВЛЕНИЕ ТАБЛИЦЫ ИЗ C# ==================
window.updateEquipment = function(data) {
    if (data && data.data) {
        equipmentData = data.data;
        displayEquipment(equipmentData);
        updateStatistics();
    }
};

// ================== ЗАГРУЗКА СТАТУСОВ В ВЫПАДАЮЩИЙ СПИСОК ==================
window.loadStatusesToSelect = function(data) {
    if (data && data.data) {
        statusesData = data.data;
        const select = document.getElementById('status');
        if (select) {
            select.innerHTML = '';
            statusesData.forEach(status => {
                const option = document.createElement('option');
                option.value = status.id;
                option.textContent = status.nazvanie;
                select.appendChild(option);
            });
        }
    }
};

// ================== ОТОБРАЖЕНИЕ ОБОРУДОВАНИЯ В ТАБЛИЦЕ ==================
function displayEquipment(equipment) {
    const tbody = document.getElementById('equipmentTableBody');
    
    if (!tbody) return;
    
    if (!equipment || equipment.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="loading-row">
                    <i class="fas fa-industry"></i>
                    <p>Нет данных об оборудовании</p>
                </td>
            </tr>
        `;
        return;
    }
    
    tbody.innerHTML = '';
    
    equipment.forEach(eq => {
        const row = document.createElement('tr');
        row.onclick = () => selectEquipment(eq.id);
        row.id = `eq-${eq.id}`;
        
        let statusClass = 'status-work';
        let statusName = eq.status_name || 'Работает';
        
        if (statusName.toLowerCase().includes('ремонт')) {
            statusClass = 'status-repair';
        } else if (statusName.toLowerCase().includes('консерв')) {
            statusClass = 'status-conservation';
        } else if (statusName.toLowerCase().includes('авар')) {
            statusClass = 'status-emergency';
        }
        
        row.innerHTML = `
            <td>${eq.id}</td>
            <td>${escapeHtml(eq.nazvanie || '')}</td>
            <td>${escapeHtml(eq.tip || '')}</td>
            <td>${escapeHtml(eq.model || '')}</td>
            <td>${escapeHtml(eq.seriinomer || '')}</td>
            <td>${escapeHtml(eq.mesto || '')}</td>
            <td><span class="status-badge ${statusClass}">${escapeHtml(statusName)}</span></td>
        `;
        
        tbody.appendChild(row);
    });
    
    // Восстанавливаем выделение
    if (selectedEquipmentId !== -1) {
        const selectedRow = document.getElementById(`eq-${selectedEquipmentId}`);
        if (selectedRow) {
            selectedRow.classList.add('selected');
        }
    }
}

// ================== ВЫБОР ОБОРУДОВАНИЯ ==================
function selectEquipment(id) {
    selectedEquipmentId = id;
    document.getElementById('selectedEquipmentId').value = id;
    
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('selected');
    });
    
    const selectedRow = document.getElementById(`eq-${id}`);
    if (selectedRow) {
        selectedRow.classList.add('selected');
    }
    
    const equipment = equipmentData.find(eq => eq.id === id);
    if (equipment) {
        fillForm(equipment);
    }
}

// ================== ЗАПОЛНЕНИЕ ФОРМЫ ==================
function fillForm(equipment) {
    if (!equipment) return;
    
    document.getElementById('nazvanie').value = equipment.nazvanie || '';
    document.getElementById('tip').value = equipment.tip || '';
    document.getElementById('model').value = equipment.model || '';
    document.getElementById('seriinomer').value = equipment.seriinomer || '';
    document.getElementById('mesto').value = equipment.mesto || '';
    document.getElementById('moshnost').value = equipment.moshnost || 0;
    document.getElementById('davlenie').value = equipment.davlenie || 0;
    document.getElementById('proizvoditel').value = equipment.proizvoditel || '';
    
    if (equipment.data_ustanovki) {
        let dateStr = equipment.data_ustanovki;
        if (dateStr.includes('.')) {
            let parts = dateStr.split('.');
            dateStr = `${parts[2]}-${parts[1]}-${parts[0]}`;
        }
        document.getElementById('dataUstanovki').value = dateStr.substring(0, 10);
    } else {
        document.getElementById('dataUstanovki').value = '';
    }
    
    document.getElementById('status').value = equipment.status_id || 1;
}

// ================== ПОИСК (точное совпадение по ID) ==================
function searchEquipment() {
    const searchText = document.getElementById('searchInput').value.trim();
    
    if (!searchText) {
        displayEquipment(equipmentData);
        return;
    }
    
    // Проверяем, является ли поисковый запрос числом (ID)
    const isNumeric = /^\d+$/.test(searchText);
    
    let filtered;
    if (isNumeric) {
        const searchId = parseInt(searchText);
        // Поиск по точному ID
        filtered = equipmentData.filter(eq => eq.id === searchId);
    } else {
        const searchLower = searchText.toLowerCase();
        // Поиск по текстовым полям
        filtered = equipmentData.filter(eq => {
            return (eq.nazvanie && eq.nazvanie.toLowerCase().includes(searchLower)) ||
                   (eq.tip && eq.tip.toLowerCase().includes(searchLower)) ||
                   (eq.model && eq.model.toLowerCase().includes(searchLower)) ||
                   (eq.proizvoditel && eq.proizvoditel.toLowerCase().includes(searchLower)) ||
                   (eq.seriinomer && eq.seriinomer.toLowerCase().includes(searchLower));
        });
    }
    
    displayEquipment(filtered);
    
    if (filtered.length === 0) {
        alert('Ничего не найдено');
    }
}

// ================== СБРОС ПОИСКА ==================
function resetSearch() {
    document.getElementById('searchInput').value = '';
    displayEquipment(equipmentData);
}

// ================== ДОБАВЛЕНИЕ ==================
function addEquipment() {
    if (!validateForm()) return;
    
    const equipment = {
        nazvanie: document.getElementById('nazvanie').value,
        tip: document.getElementById('tip').value,
        model: document.getElementById('model').value,
        seriinomer: document.getElementById('seriinomer').value,
        mesto: document.getElementById('mesto').value,
        moshnost: parseFloat(document.getElementById('moshnost').value) || 0,
        davlenie: parseFloat(document.getElementById('davlenie').value) || 0,
        proizvoditel: document.getElementById('proizvoditel').value,
        data_ustanovki: document.getElementById('dataUstanovki').value,
        status_id: parseInt(document.getElementById('status').value)
    };
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'addEquipment',
            data: equipment
        }));
    }
}

// ================== ОБНОВЛЕНИЕ ==================
function updateEquipment() {
    if (selectedEquipmentId === -1) {
        alert('Выберите оборудование для редактирования!');
        return;
    }
    
    if (!validateForm()) return;
    
    const equipment = {
        id: selectedEquipmentId,
        nazvanie: document.getElementById('nazvanie').value,
        tip: document.getElementById('tip').value,
        model: document.getElementById('model').value,
        seriinomer: document.getElementById('seriinomer').value,
        mesto: document.getElementById('mesto').value,
        moshnost: parseFloat(document.getElementById('moshnost').value) || 0,
        davlenie: parseFloat(document.getElementById('davlenie').value) || 0,
        proizvoditel: document.getElementById('proizvoditel').value,
        data_ustanovki: document.getElementById('dataUstanovki').value,
        status_id: parseInt(document.getElementById('status').value)
    };
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'updateEquipment',
            data: equipment
        }));
    }
}

// ================== УДАЛЕНИЕ ==================
function deleteEquipment() {
    if (selectedEquipmentId === -1) {
        alert('Выберите оборудование для удаления!');
        return;
    }
    
    if (confirm('Вы уверены, что хотите удалить это оборудование?')) {
        if (window.chrome?.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                action: 'deleteEquipment',
                id: selectedEquipmentId
            }));
        }
    }
}

// ================== ОЧИСТКА ФОРМЫ ==================
function clearForm() {
    selectedEquipmentId = -1;
    document.getElementById('selectedEquipmentId').value = '-1';
    document.getElementById('nazvanie').value = '';
    document.getElementById('tip').value = '';
    document.getElementById('model').value = '';
    document.getElementById('seriinomer').value = '';
    document.getElementById('mesto').value = '';
    document.getElementById('moshnost').value = '';
    document.getElementById('davlenie').value = '';
    document.getElementById('proizvoditel').value = '';
    document.getElementById('dataUstanovki').value = '';
    
    if (statusesData.length > 0) {
        document.getElementById('status').value = statusesData[0].id;
    }
    
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('selected');
    });
}

// ================== ПРОВЕРКА ФОРМЫ ==================
function validateForm() {
    const nazvanie = document.getElementById('nazvanie').value.trim();
    const tip = document.getElementById('tip').value.trim();
    const model = document.getElementById('model').value.trim();
    const dataUstanovki = document.getElementById('dataUstanovki').value;
    
    if (!nazvanie) {
        alert('Введите название оборудования!');
        return false;
    }
    
    if (!tip) {
        alert('Введите тип оборудования!');
        return false;
    }
    
    if (!model) {
        alert('Введите модель оборудования!');
        return false;
    }
    
    if (!dataUstanovki) {
        alert('Введите дату установки!');
        return false;
    }
    
    return true;
}

// ================== СТАТИСТИКА ==================
function updateStatistics() {
    const totalEl = document.getElementById('totalEquipment');
    const activeEl = document.getElementById('activeEquipment');
    const repairEl = document.getElementById('repairEquipment');
    
    if (totalEl) totalEl.textContent = equipmentData.length;
    
    const active = equipmentData.filter(eq => {
        const status = (eq.status_name || '').toLowerCase();
        return status.includes('работает') || status.includes('work');
    }).length;
    if (activeEl) activeEl.textContent = active;
    
    const repair = equipmentData.filter(eq => {
        const status = (eq.status_name || '').toLowerCase();
        return status.includes('ремонт') || status.includes('авар');
    }).length;
    if (repairEl) repairEl.textContent = repair;
}

// ================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ==================
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ================== СЛУШАЕМ СООБЩЕНИЯ ОТ C# ==================
if (window.chrome?.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        
        switch(data.action) {
            case 'equipmentLoaded':
                if (Array.isArray(data.data)) {
                    equipmentData = data.data;
                    displayEquipment(equipmentData);
                    updateStatistics();
                }
                break;
                
            case 'statusesLoaded':
                if (Array.isArray(data.data)) {
                    statusesData = data.data;
                    const select = document.getElementById('status');
                    if (select) {
                        select.innerHTML = '';
                        statusesData.forEach(status => {
                            const option = document.createElement('option');
                            option.value = status.id;
                            option.textContent = status.nazvanie;
                            select.appendChild(option);
                        });
                    }
                }
                break;
                
            case 'success':
                alert('✅ ' + data.message);
                loadEquipment();
                clearForm();
                break;
                
            case 'error':
                alert('❌ ' + data.message);
                break;
        }
    });
}