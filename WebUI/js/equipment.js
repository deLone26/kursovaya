// ================== ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ==================
let equipmentData = [];
let statusesData = [];
let selectedEquipmentId = -1;

console.log("===== equipment.js загружен =====");

// ================== ИНИЦИАЛИЗАЦИЯ ==================
document.addEventListener('DOMContentLoaded', function() {
    loadEquipment();
});

// ================== ЗАГРУЗКА ДАННЫХ ==================
function loadEquipment(filter = '') {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadEquipment',
            filter: filter
        }));
    }
}

// ================== ОБНОВЛЕНИЕ ТАБЛИЦЫ ИЗ C# ==================
window.updateEquipment = function(data) {
    console.log("updateEquipment вызван с данными:", data);
    
    if (data && data.data) {
        equipmentData = data.data;
        displayEquipment(equipmentData);
        updateStatistics();
    }
};

// ================== ЗАГРУЗКА СТАТУСОВ ==================
window.loadStatuses = function(data) {
    if (data && data.data) {
        statusesData = data.data;
        const select = document.getElementById('status');
        select.innerHTML = '';
        
        statusesData.forEach(status => {
            const option = document.createElement('option');
            option.value = status.id;
            option.textContent = status.nazvanie;
            select.appendChild(option);
        });
    }
};

// ================== ОТОБРАЖЕНИЕ ОБОРУДОВАНИЯ ==================
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
        
        // Определяем класс для статуса
        let statusClass = 'status-work';
        
        if (eq.status_name?.toLowerCase().includes('ремонт')) {
            statusClass = 'status-repair';
        } else if (eq.status_name?.toLowerCase().includes('консерв')) {
            statusClass = 'status-conservation';
        } else if (eq.status_name?.toLowerCase().includes('авар')) {
            statusClass = 'status-emergency';
        }
        
        row.innerHTML = `
            <td>${eq.id}</td>
            <td>${eq.nazvanie || ''}</td>
            <td>${eq.tip || ''}</td>
            <td>${eq.model || ''}</td>
            <td>${eq.seriinomer || ''}</td>
            <td>${eq.mesto || ''}</td>
            <td><span class="status-badge ${statusClass}">${eq.status_name || 'Работает'}</span></td>
        `;
        
        tbody.appendChild(row);
    });
}

// ================== ВЫБОР ОБОРУДОВАНИЯ ==================
function selectEquipment(id) {
    selectedEquipmentId = id;
    document.getElementById('selectedEquipmentId').value = id;
    
    // Подсветка строки
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('selected');
    });
    
    const selectedRow = document.getElementById(`eq-${id}`);
    if (selectedRow) {
        selectedRow.classList.add('selected');
    }
    
    // Заполняем форму
    const eq = equipmentData.find(e => e.id === id);
    if (eq) {
        document.getElementById('nazvanie').value = eq.nazvanie || '';
        document.getElementById('tip').value = eq.tip || '';
        document.getElementById('model').value = eq.model || '';
        document.getElementById('seriinomer').value = eq.seriinomer || '';
        document.getElementById('mesto').value = eq.mesto || '';
        document.getElementById('moshnost').value = eq.moshnost || '';
        document.getElementById('davlenie').value = eq.davlenie || '';
        document.getElementById('proizvoditel').value = eq.proizvoditel || '';
        if (eq.data_ustanovki) {
            document.getElementById('dataUstanovki').value = eq.data_ustanovki.substring(0, 10);
        }
        document.getElementById('status').value = eq.status_id || 1;
    }
}

// ================== ПОИСК ==================
function searchEquipment() {
    const searchText = document.getElementById('searchInput').value.toLowerCase();
    loadEquipment(searchText);
}

// ================== ДОБАВЛЕНИЕ ==================
function addEquipment() {
    if (!validateForm()) return;
    
    if (window.chrome?.webview) {
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
        
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'addEquipment',
            data: equipment
        }));
    }
}

// ================== ОБНОВЛЕНИЕ ==================
function updateEquipment() {
    if (selectedEquipmentId === -1) {
        showMessage('Выберите оборудование!', 'warning');
        return;
    }
    
    if (!validateForm()) return;
    
    if (window.chrome?.webview) {
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
        
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'updateEquipment',
            data: equipment
        }));
    }
}

// ================== УДАЛЕНИЕ ==================
function deleteEquipment() {
    if (selectedEquipmentId === -1) {
        showMessage('Выберите оборудование!', 'warning');
        return;
    }
    
    if (confirm('Вы уверены, что хотите удалить оборудование?')) {
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
    
    // Снимаем выделение
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('selected');
    });
}

// ================== ПРОВЕРКА ФОРМЫ ==================
function validateForm() {
    const nazvanie = document.getElementById('nazvanie').value.trim();
    const tip = document.getElementById('tip').value.trim();
    const model = document.getElementById('model').value.trim();
    const moshnost = document.getElementById('moshnost').value;
    const davlenie = document.getElementById('davlenie').value;
    const dataUstanovki = document.getElementById('dataUstanovki').value;
    
    if (!nazvanie) {
        showMessage('Введите название оборудования!', 'warning');
        return false;
    }
    
    if (!tip) {
        showMessage('Введите тип оборудования!', 'warning');
        return false;
    }
    
    if (!model) {
        showMessage('Введите модель оборудования!', 'warning');
        return false;
    }
    
    if (moshnost && isNaN(parseFloat(moshnost))) {
        showMessage('Мощность должна быть числом!', 'warning');
        return false;
    }
    
    if (davlenie && isNaN(parseFloat(davlenie))) {
        showMessage('Давление должно быть числом!', 'warning');
        return false;
    }
    
    if (!dataUstanovki) {
        showMessage('Введите дату установки!', 'warning');
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
    
    const active = equipmentData.filter(eq => 
        eq.status_name?.toLowerCase().includes('работает')).length;
    if (activeEl) activeEl.textContent = active;
    
    const repair = equipmentData.filter(eq => 
        eq.status_name?.toLowerCase().includes('ремонт') || 
        eq.status_name?.toLowerCase().includes('авар')).length;
    if (repairEl) repairEl.textContent = repair;
}

// ================== СООБЩЕНИЯ ==================
function showMessage(text, type = 'info') {
    alert(text);
}

// ================== СЛУШАЕМ СООБЩЕНИЯ ОТ C# ==================
if (window.chrome?.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        console.log("Получено от C#:", data);
        
        switch(data.action) {
            case 'equipmentLoaded':
                equipmentData = data.data;
                displayEquipment(equipmentData);
                updateStatistics();
                break;
                
            case 'statusesLoaded':
                statusesData = data.data;
                const select = document.getElementById('status');
                select.innerHTML = '';
                statusesData.forEach(status => {
                    const option = document.createElement('option');
                    option.value = status.id;
                    option.textContent = status.nazvanie;
                    select.appendChild(option);
                });
                break;
                
            case 'success':
                showMessage('✅ ' + data.message, 'success');
                loadEquipment();
                clearForm();
                break;
                
            case 'warning':
                showMessage('⚠️ ' + data.message, 'warning');
                break;
                
            case 'error':
                showMessage('❌ ' + data.message, 'error');
                break;
        }
    });
}