let selectedTaskId = -1;
let currentEmployeeId = -1;
let currentUserRole = '';
let currentUserLogin = '';

document.addEventListener('DOMContentLoaded', function() {
    setDefaultDates();
    setupEventListeners();
    setupModal();
    setupTabs();
    setupEquipmentSearch();

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadTasks', startDate: '', endDate: '', showAll: true }));
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEquipment' }));
    }
});

window.receiveFromCSharp = function(command, data) {
    try {
        switch(command) {
            case 'displayTasks':
                displayTasks(data);
                break;
            case 'updateStatistics':
                updateStatistics(data);
                break;
            case 'displayEquipment':
                displayEquipment(data);
                break;
            case 'showSuccess':
                showToast('✅ ' + data, 'success');
                break;
            case 'showError':
                showToast('❌ ' + data, 'error');
                break;
        }
    } catch (error) {
        console.error('Ошибка:', error);
    }
};

function setCurrentUser(id, login, role) {
    currentEmployeeId = id;
    currentUserLogin = login;
    currentUserRole = role;
    const userNameSpan = document.getElementById('userName');
    if (userNameSpan) {
        userNameSpan.textContent = 'Слесарь';
    }
}

function setupTabs() {
    const tabBtns = document.querySelectorAll('.tab-btn');
    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tabName = btn.dataset.tab;
            
            tabBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            document.getElementById('tasksTab').classList.remove('active');
            document.getElementById('passportsTab').classList.remove('active');
            
            if (tabName === 'tasks') {
                document.getElementById('tasksTab').classList.add('active');
            } else {
                document.getElementById('passportsTab').classList.add('active');
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEquipment' }));
                }
            }
        });
    });
}

function setupEquipmentSearch() {
    const searchInput = document.getElementById('equipmentSearch');
    const searchBtn = document.getElementById('searchEquipmentBtn');
    
    const doSearch = () => {
        const filter = searchInput ? searchInput.value : '';
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({ action: 'loadEquipment', filter: filter }));
        }
    };
    
    if (searchBtn) searchBtn.onclick = doSearch;
    if (searchInput) searchInput.onkeyup = (e) => { if (e.key === 'Enter') doSearch(); };
}

function displayEquipment(data) {
    const grid = document.getElementById('equipmentGrid');
    if (!grid) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        
        if (!items || items.length === 0) {
            grid.innerHTML = '<div class="loading">Оборудование не найдено</div>';
            return;
        }
        
        let html = '';
        items.forEach(eq => {
            let statusClass = 'status-working';
            let statusText = eq.status_name || 'В работе';
            if (statusText === 'В ремонте') statusClass = 'status-repair';
            if (statusText === 'На консервации') statusClass = 'status-conservation';
            
            html += `
                <div class="equipment-card" onclick="showPassport(${eq.id})">
                    <div class="equipment-title">${escapeHtml(eq.nazvanie || '')}</div>
                    <div class="equipment-info">🏭 Тип: ${escapeHtml(eq.tip || '-')}</div>
                    <div class="equipment-info">📐 Модель: ${escapeHtml(eq.model || '-')}</div>
                    <div class="equipment-info">🔢 Зав. номер: ${escapeHtml(eq.seriionmer || '-')}</div>
                    <div><span class="equipment-status ${statusClass}">${escapeHtml(statusText)}</span></div>
                </div>
            `;
        });
        grid.innerHTML = html;
    } catch (e) {
        console.error('Ошибка отображения оборудования:', e);
        grid.innerHTML = '<div class="loading">Ошибка загрузки оборудования</div>';
    }
}

function showPassport(equipmentId) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadPassport', id: equipmentId }));
    }
}

window.showPassport = showPassport;
window.closePassportModal = function() {
    document.getElementById('passportModal').style.display = 'none';
};
window.closeCompleteModal = function() {
    document.getElementById('completeModal').style.display = 'none';
};

// ... остальные функции (setDefaultDates, setupEventListeners, setupModal, displayTasks, updateStatistics, openCompleteModal, completeTask, showToast, escapeHtml) остаются теми же ...