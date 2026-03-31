let selectedTaskId = -1;
let currentEmployeeId = -1;

document.addEventListener('DOMContentLoaded', function() {
    setDefaultDates();
    setupEventListeners();
    setupModal();

    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action: 'loadTasks', startDate: '', endDate: '', showAll: true }));
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
    const userNameSpan = document.getElementById('userName');
    if (userNameSpan) {
        userNameSpan.textContent = 'Слесарь';
    }
}

function setDefaultDates() {
    const today = new Date();
    const monthAgo = new Date();
    monthAgo.setMonth(monthAgo.getMonth() - 1);
    
    const formatDate = (date) => date.toISOString().split('T')[0];
    
    const startDate = document.getElementById('startDate');
    const endDate = document.getElementById('endDate');
    const actualStartDate = document.getElementById('actualStartDate');
    
    if (startDate) startDate.value = formatDate(monthAgo);
    if (endDate) endDate.value = formatDate(today);
    if (actualStartDate) actualStartDate.value = formatDate(today);
}

function setupEventListeners() {
    const applyFilterBtn = document.getElementById('applyFilterBtn');
    if (applyFilterBtn) {
        applyFilterBtn.addEventListener('click', () => {
            const startDate = document.getElementById('startDate')?.value || '';
            const endDate = document.getElementById('endDate')?.value || '';
            const showAll = document.getElementById('showAll')?.checked || false;
            
            if (window.chrome?.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'loadTasks',
                    startDate: startDate,
                    endDate: endDate,
                    showAll: showAll
                }));
            }
        });
    }
    
    const showAllCheckbox = document.getElementById('showAll');
    if (showAllCheckbox) {
        showAllCheckbox.addEventListener('change', function(e) {
            const start = document.getElementById('startDate');
            const end = document.getElementById('endDate');
            if (start) start.disabled = e.target.checked;
            if (end) end.disabled = e.target.checked;
        });
    }
}

function setupModal() {
    const modal = document.getElementById('completeModal');
    const closeBtn = document.querySelector('.close');
    const cancelBtn = document.getElementById('cancelModalBtn');
    
    if (closeBtn) {
        closeBtn.onclick = () => modal.style.display = 'none';
    }
    if (cancelBtn) {
        cancelBtn.onclick = () => modal.style.display = 'none';
    }
    
    const completeBtn = document.getElementById('completeTaskBtn');
    if (completeBtn) {
        completeBtn.addEventListener('click', completeTask);
    }
    
    window.onclick = (event) => {
        if (event.target === modal) {
            modal.style.display = 'none';
        }
    };
}

function displayTasks(data) {
    const tbody = document.getElementById('tasksTableBody');
    if (!tbody) return;
    
    try {
        let items = typeof data === 'string' ? JSON.parse(data) : data;
        
        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="loading">Нет заданий</td></tr>';
            return;
        }
        
        let html = '';
        items.forEach(row => {
            const statusClass = row.status === 'Завершен' ? 'status-completed' : (row.status === 'В работе' ? 'status-progress' : 'status-pending');
            const statusText = row.status === 'Завершен' ? 'Выполнено' : (row.status === 'В работе' ? 'В работе' : 'Ожидает');
            const isCompleted = row.status === 'Завершен';
            
            html += `<tr onclick="selectTask(${row.id})" style="cursor: pointer;">`;
            html += `<td>${row.id || ''}</td>`;
            html += `<td>${escapeHtml(row.equipment || '')}</td>`;
            html += `<td>${escapeHtml(row.tip || '')}</td>`;
            html += `<td>${row.start_date || ''} - ${row.end_date || 'не указана'}</td>`;
            html += `<td><span class="status-badge ${statusClass}">${statusText}</span></td>`;
            html += `<td>${!isCompleted ? `<button class="btn-complete" onclick="event.stopPropagation(); openCompleteModal(${row.id})">Выполнить</button>` : '✅ Выполнено'}</td>`;
            html += `</tr>`;
        });
        tbody.innerHTML = html;
    } catch (e) {
        console.error('Ошибка отображения заданий:', e);
        tbody.innerHTML = '<tr><td colspan="6" class="loading">Ошибка загрузки данных</td></tr>';
    }
}

function selectTask(id) {
    selectedTaskId = id;
    document.querySelectorAll('#tasksTableBody tr').forEach(tr => {
        tr.classList.remove('selected');
    });
    if (event && event.currentTarget) {
        event.currentTarget.classList.add('selected');
    }
}

function updateStatistics(data) {
    try {
        let stats = typeof data === 'string' ? JSON.parse(data) : data;
        
        const totalTasks = document.getElementById('totalTasks');
        const inProgressTasks = document.getElementById('inProgressTasks');
        const completedTasks = document.getElementById('completedTasks');
        
        if (totalTasks) totalTasks.textContent = stats.total || 0;
        if (inProgressTasks) inProgressTasks.textContent = stats.inProgress || 0;
        if (completedTasks) completedTasks.textContent = stats.completed || 0;
    } catch (e) {
        console.error('Ошибка обновления статистики:', e);
    }
}

function openCompleteModal(taskId) {
    selectedTaskId = taskId;
    document.getElementById('taskId').value = taskId;
    document.getElementById('completionDescription').value = '';
    document.getElementById('actualStartDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('actualEndDate').value = '';
    document.getElementById('completeModal').style.display = 'flex';
}

function completeTask() {
    const description = document.getElementById('completionDescription').value.trim();
    const actualStartDate = document.getElementById('actualStartDate').value;
    const actualEndDate = document.getElementById('actualEndDate').value;
    
    if (!description) {
        showToast('Введите описание выполненных работ!', 'warning');
        return;
    }
    
    if (!actualStartDate) {
        showToast('Введите дату начала работ!', 'warning');
        return;
    }
    
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            action: 'completeTask',
            id: selectedTaskId,
            description: description,
            actualStartDate: actualStartDate,
            actualEndDate: actualEndDate
        }));
    }
    
    document.getElementById('completeModal').style.display = 'none';
}

function showToast(message, type) {
    const toast = document.getElementById('toastMessage');
    if (!toast) return;
    
    toast.textContent = message;
    toast.className = `toast ${type}`;
    toast.style.display = 'block';
    
    setTimeout(() => {
        toast.style.display = 'none';
    }, 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}