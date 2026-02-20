// ================== ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ==================
let equipmentData = [];
let selectedEquipmentId = -1;

console.log("===== equipment.js загружен =====");

// ================== НАВИГАЦИЯ ==================
function navigateTo(page) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'navigate',
            page: page
        }));
    }
}

// ================== ЗАГРУЗКА ДАННЫХ ==================
function loadEquipment() {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'loadEquipment'
        }));
    }
}

// Функция для обновления таблицы из C#
window.updateEquipment = function(data) {
    console.log("updateEquipment вызван с данными:", data);
    
    if (data && data.equipment) {
        equipmentData = data.equipment;
        displayEquipment(equipmentData);
        updateStatistics();
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
        let statusText = 'Работает';
        
        if (eq.status_id === 2) {
            statusClass = 'status-repair';
            statusText = 'В ремонте';
        } else if (eq.status_id === 3) {
            statusClass = 'status-conservation';
            statusText = 'На консервации';
        } else if (eq.status_id === 4) {
            statusClass = 'status-emergency';
            statusText = 'Аварийное';
        }
        
        row.innerHTML = `
            <td>${eq.id}</td>
            <td>${eq.nazvanie || ''}</td>
            <td>${eq.tip || ''}</td>
            <td>${eq.model || ''}</td>
            <td>${eq.seriynomer || ''}</td>
            <td>${eq.mesto || ''}</td>
            <td><span class="status-badge ${statusClass}">${statusText}</span></td>
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
        document.getElementById('seriynomer').value = eq.seriynomer || '';
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
    
    if (!searchText) {
        displayEquipment(equipmentData);
        return;
    }
    
    const filtered = equipmentData.filter(eq => 
        (eq.nazvanie && eq.nazvanie.toLowerCase().includes(searchText)) ||
        eq.id.toString().includes(searchText) ||
        (eq.model && eq.model.toLowerCase().includes(searchText)) ||
        (eq.seriynomer && eq.seriynomer.toLowerCase().includes(searchText)) ||
        (eq.tip && eq.tip.toLowerCase().includes(searchText))
    );
    
    displayEquipment(filtered);
}

// ================== ДОБАВЛЕНИЕ ==================
function addEquipment() {
    if (!validateForm()) return;
    
    if (window.chrome?.webview) {
        const equipment = {
            nazvanie: document.getElementById('nazvanie').value,
            tip: document.getElementById('tip').value,
            model: document.getElementById('model').value,
            seriynomer: document.getElementById('seriynomer').value,
            mesto: document.getElementById('mesto').value,
            moshnost: document.getElementById('moshnost').value,
            davlenie: document.getElementById('davlenie').value,
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
            seriynomer: document.getElementById('seriynomer').value,
            mesto: document.getElementById('mesto').value,
            moshnost: document.getElementById('moshnost').value,
            davlenie: document.getElementById('davlenie').value,
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
    document.getElementById('seriynomer').value = '';
    document.getElementById('mesto').value = '';
    document.getElementById('moshnost').value = '';
    document.getElementById('davlenie').value = '';
    document.getElementById('proizvoditel').value = '';
    document.getElementById('dataUstanovki').value = '';
    document.getElementById('status').value = '1';
    
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
    
    return true;
}

// ================== СТАТИСТИКА ==================
function updateStatistics() {
    const totalEl = document.getElementById('totalEquipment');
    const activeEl = document.getElementById('activeEquipment');
    const repairEl = document.getElementById('repairEquipment');
    
    if (totalEl) totalEl.textContent = equipmentData.length;
    if (activeEl) activeEl.textContent = equipmentData.filter(eq => eq.status_id === 1).length;
    if (repairEl) repairEl.textContent = equipmentData.filter(eq => eq.status_id === 2 || eq.status_id === 4).length;
}

// ================== СООБЩЕНИЯ ==================
function showMessage(text, type = 'info') {
    // Можно реализовать красивые всплывающие сообщения
    alert(text);
}

// ================== ЗАГРУЗКА ПРИ СТАРТЕ ==================
document.addEventListener('DOMContentLoaded', function() {
    // Загружаем оборудование
    loadEquipment();
    
    // Получаем информацию о пользователе
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'getUserInfo'
        }));
    }
});

// Слушаем сообщения от C#
window.chrome?.webview?.addEventListener('message', event => {
    const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
    
    switch(data.action) {
        case 'userInfo':
            document.getElementById('userName').textContent = data.userName || 'Администратор';
            break;
            
        case 'equipmentLoaded':
            equipmentData = data.data;
            displayEquipment(equipmentData);
            updateStatistics();
            break;
            
        case 'success':
            showMessage(data.message, 'success');
            loadEquipment();
            clearForm();
            break;
            
        case 'warning':
            showMessage(data.message, 'warning');
            break;
            
        case 'error':
            showMessage(data.message, 'error');
            break;
    }
});