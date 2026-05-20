let equipmentData = [];
let statusesData = [];
let selectedEquipmentId = -1;

document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM загружен");
    loadEquipment();
    loadStatuses();
    
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') searchEquipment();
        });
    }
});

function loadEquipment(filter = '') {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEquipment', filter: filter }));
    }
}

function loadStatuses() {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadStatuses' }));
    }
}

// Обработчик сообщений от C#
if (window.chrome?.webview) {
    window.chrome.webview.addEventListener('message', event => {
        const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        console.log("Получено от C#:", data.action);
        
        switch(data.action) {
            case 'equipmentLoaded':
                equipmentData = data.data;
                displayEquipment(equipmentData);
                updateStatistics();
                break;
            case 'statusesLoaded':
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

function displayEquipment(equipment) {
    const tbody = document.getElementById('equipmentTableBody');
    if (!tbody) return;
    
    if (!equipment || equipment.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading-row">Нет данных</td</tr>';
        return;
    }
    
    tbody.innerHTML = '';
    equipment.forEach(eq => {
        const row = document.createElement('tr');
        row.onclick = () => selectEquipment(eq.id);
        row.id = `eq-${eq.id}`;
        
        // Определяем класс статуса по статусу оборудования
        let statusClass = 'status-work';
        let statusName = eq.status_name || 'Работает';
        
        // По status_id из базы
        if (eq.status_id === 1) {
            statusClass = 'status-work';
        } else if (eq.status_id === 2) {
            statusClass = 'status-repair';
        } else if (eq.status_id === 3) {
            statusClass = 'status-notwork';
        } else if (eq.status_id === 4) {
            statusClass = 'status-overdue';
        }
        
        // Также по названию для подстраховки
        const statusNameLower = statusName.toLowerCase();
        if (statusNameLower.includes('ремонт')) {
            statusClass = 'status-repair';
        } else if (statusNameLower.includes('консерв')) {
            statusClass = 'status-conservation';
        } else if (statusNameLower.includes('авар')) {
            statusClass = 'status-emergency';
        } else if (statusNameLower.includes('просроч')) {
            statusClass = 'status-overdue';
        } else if (statusNameLower.includes('не работ')) {
            statusClass = 'status-notwork';
        }
        
        row.innerHTML = `
            <td>${eq.id}</td>
            <td>${escapeHtml(eq.nazvanie)}</td>
            <td>${escapeHtml(eq.tip)}</td>
            <td>${escapeHtml(eq.model)}</td>
            <td>${escapeHtml(eq.seriinomer)}</td>
            <td>${escapeHtml(eq.mesto)}</td>
            <td><span class="status-badge ${statusClass}">${escapeHtml(statusName)}</span></td>
        `;
        tbody.appendChild(row);
    });
    
    if (selectedEquipmentId !== -1) {
        const selectedRow = document.getElementById(`eq-${selectedEquipmentId}`);
        if (selectedRow) selectedRow.classList.add('selected');
    }
}

function selectEquipment(id) {
    selectedEquipmentId = id;
    document.getElementById('selectedEquipmentId').value = id;
    
    document.querySelectorAll('#equipmentTableBody tr').forEach(row => row.classList.remove('selected'));
    const selectedRow = document.getElementById(`eq-${id}`);
    if (selectedRow) selectedRow.classList.add('selected');
    
    const equipment = equipmentData.find(eq => eq.id === id);
    if (equipment) fillForm(equipment);
}

function fillForm(eq) {
    document.getElementById('nazvanie').value = eq.nazvanie || '';
    document.getElementById('tip').value = eq.tip || '';
    document.getElementById('model').value = eq.model || '';
    document.getElementById('seriinomer').value = eq.seriinomer || '';
    document.getElementById('mesto').value = eq.mesto || '';
    document.getElementById('moshnost').value = eq.moshnost || 0;
    document.getElementById('davlenie').value = eq.davlenie || 0;
    document.getElementById('proizvoditel').value = eq.proizvoditel || '';
    document.getElementById('dataUstanovki').value = eq.data_ustanovki || '';
    document.getElementById('status').value = eq.status_id || 1;
}

function searchEquipment() {
    const searchText = document.getElementById('searchInput').value.trim();
    if (!searchText) {
        displayEquipment(equipmentData);
        return;
    }
    
    const isNumeric = /^\d+$/.test(searchText);
    let filtered;
    
    if (isNumeric) {
        filtered = equipmentData.filter(eq => eq.id === parseInt(searchText));
    } else {
        const lower = searchText.toLowerCase();
        filtered = equipmentData.filter(eq => 
            (eq.nazvanie && eq.nazvanie.toLowerCase().includes(lower)) ||
            (eq.tip && eq.tip.toLowerCase().includes(lower)) ||
            (eq.model && eq.model.toLowerCase().includes(lower)) ||
            (eq.proizvoditel && eq.proizvoditel.toLowerCase().includes(lower))
        );
    }
    
    displayEquipment(filtered);
    if (filtered.length === 0) alert('Ничего не найдено');
}

function resetSearch() {
    document.getElementById('searchInput').value = '';
    displayEquipment(equipmentData);
}

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
        window.chrome.webview.postMessage(JSON.stringify({ action: 'addEquipment', data: equipment }));
    }
}

function updateEquipment() {
    if (selectedEquipmentId === -1) {
        alert('Выберите оборудование из таблицы!');
        return;
    }
    
    if (!validateForm()) return;
    
    const equipment = {
        id: selectedEquipmentId,           // маленькая буква
        nazvanie: document.getElementById('nazvanie').value,
        tip: document.getElementById('tip').value,
        model: document.getElementById('model').value,
        seriinomer: document.getElementById('seriinomer').value,
        mesto: document.getElementById('mesto').value,
        moshnost: parseFloat(document.getElementById('moshnost').value) || 0,
        davlenie: parseFloat(document.getElementById('davlenie').value) || 0,
        proizvoditel: document.getElementById('proizvoditel').value,
        data_ustanovki: document.getElementById('dataUstanovki').value,
        status_id: parseInt(document.getElementById('status').value)  // status_id
    };
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'updateEquipment', data: equipment }));
    }
}

function deleteEquipment() {
    if (selectedEquipmentId === -1) {
        alert('Выберите оборудование для удаления!');
        return;
    }
    
    if (confirm('Удалить оборудование?')) {
        if (window.chrome?.webview) {
            window.chrome.webview.postMessage(JSON.stringify({ action: 'deleteEquipment', id: selectedEquipmentId }));
        }
    }
}

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
    if (statusesData.length > 0) document.getElementById('status').value = statusesData[0].id;
    
    document.querySelectorAll('#equipmentTableBody tr').forEach(row => row.classList.remove('selected'));
}

function validateForm() {
    if (!document.getElementById('nazvanie').value.trim()) { alert('Введите название!'); return false; }
    if (!document.getElementById('tip').value.trim()) { alert('Введите тип!'); return false; }
    if (!document.getElementById('model').value.trim()) { alert('Введите модель!'); return false; }
    if (!document.getElementById('dataUstanovki').value) { alert('Введите дату установки!'); return false; }
    return true;
}

function updateStatistics() {
    const totalEl = document.getElementById('totalEquipment');
    const activeEl = document.getElementById('activeEquipment');
    const repairEl = document.getElementById('repairEquipment');
    
    if (totalEl) totalEl.textContent = equipmentData.length;
    const active = equipmentData.filter(eq => (eq.status_name || '').toLowerCase().includes('работает')).length;
    const repair = equipmentData.filter(eq => (eq.status_name || '').toLowerCase().includes('ремонт')).length;
    if (activeEl) activeEl.textContent = active;
    if (repairEl) repairEl.textContent = repair;
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