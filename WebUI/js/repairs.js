let selectedTaskId = null;
let notifications = [];
let unreadCount = 0;
let shownNotificationIds = new Set();

// ========== ОСНОВНЫЕ ФУНКЦИИ ==========

function setCurrentUser(id, login, role, name) {
    document.getElementById("userName").innerText = name;
}

function showTab(name) {
    document.querySelectorAll(".tab-content").forEach(x => x.classList.remove("active"));
    document.querySelectorAll(".tab").forEach(x => x.classList.remove("active"));
    
    if (name === "tasks") {
        document.getElementById("tasksTab").classList.add("active");
        document.querySelectorAll(".tab")[0].classList.add("active");
        loadTasks();
    }
    if (name === "history") {
        document.getElementById("historyTab").classList.add("active");
        document.querySelectorAll(".tab")[1].classList.add("active");
        loadHistory();
    }
    if (name === "accidents") {
        document.getElementById("accidentsTab").classList.add("active");
        document.querySelectorAll(".tab")[2].classList.add("active");
        loadAccidentHistory();
    }
    if (name === "stats") {
        document.getElementById("statsTab").classList.add("active");
        document.querySelectorAll(".tab")[3].classList.add("active");
        loadStatistics();
    }
}

function sendToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action: action, ...data });
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(msg);
    }
}

// ========== ЗАГРУЗКА ДАННЫХ ==========

function loadTasks() {
    sendToCSharp("loadTasks");
}

function loadHistory() {
    sendToCSharp("loadHistory", {
        startDate: document.getElementById("historyStart").value,
        endDate: document.getElementById("historyEnd").value
    });
}

function loadAccidentHistory() {
    sendToCSharp("loadAccidentHistory", {
        startDate: document.getElementById("accidentStart").value,
        endDate: document.getElementById("accidentEnd").value
    });
}

function loadStatistics() {
    sendToCSharp("loadStatistics");
}

function loadSpareParts(equipmentId) {
    sendToCSharp("loadSpareParts", { equipmentId: equipmentId });
}

// ========== ОТОБРАЖЕНИЕ ДАННЫХ ==========

function displayTasks(data) {
    let tasks = JSON.parse(data);
    let body = document.getElementById("tasksTableBody");
    body.innerHTML = "";

    if (!tasks || tasks.length === 0) {
        body.innerHTML = '<tr><td colspan="7" class="loading">Нет активных задач</td></tr>';
        return;
    }

    tasks.forEach(t => {
        let isEmergency = t.is_urgent === true || t.is_accident === true;
        let type = isEmergency
            ? `<span class="type-badge type-avariya">🔴 Аварийный ремонт</span>`
            : `<span class="type-badge type-to">🔵 Техническое обслуживание</span>`;

        // Кнопка "Принять в работу" меняет текст если задача просрочена
        let statusHtml = "";
        if (t.status === "Просрочен" || t.is_overdue === true) {
            statusHtml = `<button class="status-btn overdue-btn" onclick="changeStatus(${t.id})">⚠️ Принять (просрочено)</button>`;
        } else if (t.status === "Зарегистрирован") {
            statusHtml = `<button class="status-btn" onclick="changeStatus(${t.id})">Принять в работу</button>`;
        } else if (t.status === "В работе") {
            statusHtml = `<span class="status-badge status-working">⚙️ В работе</span>`;
        } else if (t.status === "Завершен") {
            statusHtml = `<span class="status-badge status-completed">✅ Завершено</span>`;
        } else {
            statusHtml = `<span class="status-badge status-registered">${t.status}</span>`;
        }

        let rowClass = '';
        if (t.is_urgent) rowClass = 'urgent-task';
        if (t.is_overdue === true || t.status === "Просрочен") rowClass = 'overdue-task';

        let reportButton = (t.status === "В работе")
            ? `<button class="report-btn" onclick='openReport(${JSON.stringify(t).replace(/'/g, "\\'")})'>📝 Отчёт</button>`
            : "-";
        
        let description = t.description || '-';
        if (description.length > 60) description = description.substring(0, 57) + '...';

        body.innerHTML += `
            <tr class="${rowClass}" onclick="selectTaskRow(this, ${t.id})">
                <td>${t.id}</td>
                <td>${type}</td>
                <td>${escapeHtml(t.equipment_name)}</td>
                <td>${escapeHtml(description)}</td>
                <td>${t.due_date || '-'}</td>
                <td>${statusHtml}</td>
                <td>${reportButton}</td>
            </tr>
        `;
    });
}

function displayHistory(data) {
    let rows = JSON.parse(data);
    let body = document.getElementById("historyTableBody");
    body.innerHTML = "";
    
    if (!rows || rows.length === 0) {
        body.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    rows.forEach(r => {
        let deadlineClass = r.deadline_status === "Просрочена" ? "status-overdue" : "status-on-time";
        
        body.innerHTML += `
            <tr>
                <td>${escapeHtml(r.equipment || '-')}</td>
                <td>${r.start_date || '-'}</td>
                <td>${r.completion_date || '-'}</td>
                <td>${escapeHtml(r.description || '-')}</td>
                <td>${escapeHtml(r.replaced_part || '-')}</td>
                <td><span class="status-badge status-completed">✅ Завершено</span></td>
                <td><span class="status-badge ${deadlineClass}">${r.deadline_status === "Просрочена" ? '⚠️ Просрочена' : '✅ В срок'}</span></td>
            </tr>
        `;
    });
}

function displayAccidentHistory(data) {
    let rows = typeof data === 'string' ? JSON.parse(data) : data;
    let body = document.getElementById("accidentsTableBody");
    body.innerHTML = "";
    
    if (!rows || rows.length === 0) {
        body.innerHTML = '<tr><td colspan="7" class="loading">Нет данных</td></tr>';
        return;
    }
    
    rows.forEach(r => {
        let description = r.description || '-';
        if (description.length > 60) description = description.substring(0, 57) + '...';
        
        let spareParts = r.spare_parts || '-';
        if (spareParts !== '-' && spareParts.length > 25) {
            spareParts = spareParts.substring(0, 22) + '...';
        }
        
        let deadlineClass = r.was_overdue === true ? "status-overdue" : "status-on-time";
        let deadlineText = r.was_overdue === true ? '⚠️ Просрочена' : '✅ В срок';
        
        body.innerHTML += `
            <tr>
                <td>${escapeHtml(r.equipment || '-')}</td>
                <td>${r.date || '-'}</td>
                <td>${r.completion_date || '-'}</td>
                <td>${escapeHtml(description)}</td>
                <td>${escapeHtml(spareParts)}</td>
                <td><span class="status-badge status-completed">✅ Завершена</span></td>
                <td><span class="status-badge ${deadlineClass}">${deadlineText}</span></td>
            </tr>
        `;
    });
}

function displayStats(data) {
    let s = JSON.parse(data);
    
    animateNumber("statTotal", s.total || 0);
    animateNumber("statDone", s.completed || 0);
    animateNumber("statWork", s.inwork || 0);
    animateNumber("statOverdue", s.overdue || 0);
    animateNumber("statUrgent", s.urgent || 0);
    animateNumber("statToday", s.today || 0);
    
    let percent = s.percent || 0;
    animateNumber("statPercent", percent, "%");
    
    let progressFill = document.getElementById("progressFill");
    if (progressFill) progressFill.style.width = percent + "%";
    
    let avgDays = s.avg || 0;
    let avgText = avgDays >= 1 ? Math.round(avgDays) + " дн" : (avgDays > 0 ? "< 1 дн" : "0 дн");
    let avgElement = document.getElementById("statAvg");
    if (avgElement) avgElement.innerHTML = avgText;
}

function displaySpareParts(data) {
    let select = document.getElementById("reportParts");
    let parts = JSON.parse(data);
    select.innerHTML = '';
    if (parts.length === 0) {
        select.innerHTML = '<option disabled>Нет доступных запчастей</option>';
    } else {
        parts.forEach(p => {
            let option = document.createElement('option');
            option.value = p.id;
            option.text = p.name + (p.stock ? ` (остаток: ${p.stock})` : '');
            select.appendChild(option);
        });
    }
}

// ========== УВЕДОМЛЕНИЯ О ПРОСРОЧКЕ ==========

function showOverdueNotification(count) {
    if (count > 0) {
        let text = `У вас ${count} просроченных ${getDeclension(count, 'задача', 'задачи', 'задач')}!`;
        addNotification('⚠️ Просроченные задачи', text, 'error');
    }
}

function checkOverdueTasks(count) {
    if (count > 0) {
        showOverdueNotification(count);
    }
}

function getDeclension(number, one, two, five) {
    let n = Math.abs(number) % 100;
    if (n >= 5 && n <= 20) return five;
    n = n % 10;
    if (n === 1) return one;
    if (n >= 2 && n <= 4) return two;
    return five;
}

// ========== ДЕЙСТВИЯ С ЗАДАЧАМИ ==========

function selectTaskRow(element, taskId) {
    document.querySelectorAll('#tasksTableBody tr').forEach(row => row.classList.remove('selected'));
    element.classList.add('selected');
    selectedTaskId = taskId;
}

function changeStatus(taskId) {
    showConfirmModal(
        "Подтверждение",
        "Вы уверены, что хотите принять эту задачу в работу?",
        function() {
            sendToCSharp("changeStatus", { taskId: taskId });
        }
    );
}

function openReport(task) {
    document.getElementById("reportModal").style.display = "block";
    document.getElementById("reportTaskId").value = task.id;
    document.getElementById("reportEquipment").value = task.equipment_name;
    
    let now = new Date();
    let formattedStart = now.toISOString().slice(0, 16);
    document.getElementById("reportStartDate").value = task.start_work_date || formattedStart;
    document.getElementById("reportEndDate").value = formattedStart;
    document.getElementById("reportDescription").value = "";
    
    loadSpareParts(task.equipment_id);
}

function closeModal() {
    document.getElementById("reportModal").style.display = "none";
}

function submitReport() {
    let parts = [];
    let select = document.getElementById("reportParts");
    
    for (let i = 0; i < select.options.length; i++) {
        if (select.options[i].selected && select.options[i].value) {
            parts.push(parseInt(select.options[i].value));
        }
    }
    
    let startDate = document.getElementById("reportStartDate").value;
    let endDate = document.getElementById("reportEndDate").value;
    let description = document.getElementById("reportDescription").value;
    
    if (startDate && endDate && new Date(startDate) > new Date(endDate)) {
        showCenterModal("Ошибка валидации", "Дата начала не может быть позже даты окончания!", "error");
        return;
    }
    
    if (!description.trim()) {
        showCenterModal("Ошибка", "Введите описание выполненных работ!", "error");
        return;
    }
    
    sendToCSharp("submitReport", {
        taskId: parseInt(document.getElementById("reportTaskId").value),
        sparePartIds: parts,
        description: description,
        startDate: startDate,
        endDate: endDate
    });
    
    closeModal();
}

function logout() {
    sendToCSharp("logout");
}

// ========== УВЕДОМЛЕНИЯ ==========

function formatRelativeTime(date) {
    let now = new Date();
    let diff = Math.floor((now - date) / 1000 / 60);
    if (diff < 1) return "только что";
    if (diff < 60) return `${diff} мин назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч назад`;
    return `${Math.floor(diff / 1440)} дн назад`;
}

function addNotification(title, text, type = 'info', taskId = null) {
    let notifKey = `${title}-${text}-${taskId}`;
    if (shownNotificationIds.has(notifKey)) return;
    
    let notification = {
        id: Date.now(),
        title: title,
        text: text,
        time: formatRelativeTime(new Date()),
        type: type,
        taskId: taskId,
        isRead: false
    };
    
    shownNotificationIds.add(notifKey);
    notifications.unshift(notification);
    unreadCount++;
    updateNotificationUI();
}

function updateNotificationUI() {
    let countEl = document.getElementById("notificationCount");
    if (countEl) {
        countEl.innerText = unreadCount;
        countEl.style.display = unreadCount > 0 ? "flex" : "none";
    }
    
    let container = document.getElementById("notificationsContainer");
    if (!container) return;
    
    if (notifications.length === 0) {
        container.innerHTML = '<div class="notification-item">Нет уведомлений</div>';
        return;
    }
    
    let html = '';
    for (let i = 0; i < Math.min(notifications.length, 20); i++) {
        let n = notifications[i];
        let unreadClass = !n.isRead ? 'unread' : '';
        let titleClass = n.type === 'error' ? 'critical' : (n.type === 'success' ? 'success' : '');
        let icon = n.type === 'error' ? '🔴' : (n.type === 'success' ? '✅' : '🔵');
        
        html += `
            <div class="notification-item ${unreadClass}" data-id="${n.id}" data-task-id="${n.taskId || ''}" onclick="onNotificationClick(this)">
                <div class="notification-title ${titleClass}">${icon} ${escapeHtml(n.title)}</div>
                <div class="notification-text">${escapeHtml(n.text)}</div>
                <div class="notification-time">${n.time}</div>
            </div>
            <div class="notification-separator"></div>
        `;
    }
    
    html += `<div class="notification-footer"><a onclick="clearAllNotifications()">Очистить все</a></div>`;
    container.innerHTML = html;
}

function onNotificationClick(element) {
    let taskId = element.getAttribute('data-task-id');
    if (taskId && taskId !== '') {
        showTab('tasks');
        setTimeout(() => highlightTaskById(taskId), 100);
    }
    let id = parseInt(element.getAttribute('data-id'));
    markNotificationRead(id);
    document.getElementById("notificationPanel").style.display = "none";
}

function markNotificationRead(id) {
    let notif = notifications.find(n => n.id === id);
    if (notif && !notif.isRead) {
        notif.isRead = true;
        unreadCount--;
        updateNotificationUI();
    }
}

function clearAllNotifications() {
    notifications = [];
    unreadCount = 0;
    updateNotificationUI();
}

function toggleNotifications() {
    let panel = document.getElementById("notificationPanel");
    if (panel) panel.style.display = panel.style.display === "block" ? "none" : "block";
}

function showSuccess(text) {
    addNotification('✅ Успешно', text, 'success');
}

function showError(text) {
    if (text.includes('просрочен')) {
        addNotification('⚠️ Просроченные задачи', text, 'error');
    } else if (text.includes('срочная') || text.includes('авария')) {
        addNotification('🚨 Внимание!', text, 'error');
    } else {
        console.error(text);
    }
}

function showNewTasksNotification(data) {
    let tasks = typeof data === 'string' ? JSON.parse(data) : data;
    if (!tasks || tasks.length === 0) return;
    
    // Сортируем: сначала срочные
    tasks.sort((a, b) => (b.is_urgent ? 1 : 0) - (a.is_urgent ? 1 : 0));
    
    let oldModal = document.querySelector('.center-modal');
    if (oldModal) oldModal.remove();
    
    let urgentTasks = tasks.filter(t => t.is_urgent === true);
    let regularTasks = tasks.filter(t => t.is_urgent !== true);
    
    let urgentHtml = '';
    let regularHtml = '';
    
    if (urgentTasks.length > 0) {
        urgentHtml = '<div class="task-section urgent-section"><div class="section-title">🚨 Срочные задачи</div>';
        urgentTasks.forEach(t => {
            urgentHtml += `
                <div class="task-item urgent" onclick="closeModalAndShowTask(${t.id})">
                    <div class="task-icon">⚠️</div>
                    <div class="task-content">
                        <div class="task-equipment">${escapeHtml(t.equipment)}</div>
                        <div class="task-description">${escapeHtml(t.description || 'Срочное техническое обслуживание')}</div>
                        <div class="task-date">📅 Дата: ${t.due_date || 'Не указана'}</div>
                        <div class="task-type">🔧 Тип: ${t.type === 'Авария' ? 'Аварийный ремонт' : 'Плановое ТО'}</div>
                    </div>
                </div>
                <div class="task-separator"></div>
            `;
        });
        urgentHtml += '</div>';
    }
    
    if (regularTasks.length > 0) {
        regularHtml = '<div class="task-section"><div class="section-title">📋 Обычные задачи</div>';
        regularTasks.forEach(t => {
            regularHtml += `
                <div class="task-item regular" onclick="closeModalAndShowTask(${t.id})">
                    <div class="task-icon">ℹ️</div>
                    <div class="task-content">
                        <div class="task-equipment">${escapeHtml(t.equipment)}</div>
                        <div class="task-description">${escapeHtml(t.description || 'Плановое техническое обслуживание')}</div>
                        <div class="task-date">📅 Дата: ${t.due_date || 'Не указана'}</div>
                        <div class="task-type">🔧 Тип: ${t.type === 'Авария' ? 'Аварийный ремонт' : 'Плановое ТО'}</div>
                    </div>
                </div>
                <div class="task-separator"></div>
            `;
        });
        regularHtml += '</div>';
    }
    
    let modal = document.createElement('div');
    modal.className = 'center-modal info large';
    modal.innerHTML = `
        <div class="modal-header">
            <div class="modal-icon-info">📋</div>
            <div class="modal-title">НОВЫЕ ЗАДАЧИ (${tasks.length})</div>
        </div>
        <div class="modal-body">
            ${urgentHtml}
            ${regularHtml}
        </div>
        <div class="modal-footer">
            <button onclick="this.closest('.center-modal').remove()">Понятно</button>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Добавляем уведомления в панель
    tasks.forEach(task => {
        let type = task.is_urgent ? 'error' : 'info';
        let title = task.is_urgent ? '⚠️ СРОЧНАЯ ЗАДАЧА' : '📋 Новая задача';
        addNotification(title, `${task.equipment} - ${task.description || 'ТО'} (${task.due_date})`, type, task.id);
    });
}

function showTodayTasksList(data) {
    if (!data) return;
    let tasks = typeof data === 'string' ? JSON.parse(data) : data;
    if (!tasks || tasks.length === 0) return;
    
    let urgentTasks = tasks.filter(t => t.is_urgent === true);
    let regularTasks = tasks.filter(t => t.is_urgent !== true);
    
    let oldModal = document.querySelector('.center-modal');
    if (oldModal) oldModal.remove();
    
    let urgentHtml = '';
    let regularHtml = '';
    
    if (urgentTasks.length > 0) {
        urgentHtml = '<div class="task-section urgent-section"><div class="section-title">🚨 Срочные задачи</div>';
        urgentTasks.forEach(t => {
            urgentHtml += `
                <div class="task-item urgent" onclick="closeModalAndShowTask(${t.id})">
                    <div class="task-icon">⚠️</div>
                    <div class="task-content">
                        <div class="task-equipment">${escapeHtml(t.equipment)}</div>
                        <div class="task-description">${escapeHtml(t.description || 'Срочное техническое обслуживание')}</div>
                        <div class="task-date">📅 Дата: ${t.due_date || 'Не указана'}</div>
                        <div class="task-type">🔧 Тип: ${t.type === 'Авария' ? 'Аварийный ремонт' : 'Плановое ТО'}</div>
                    </div>
                </div>
                <div class="task-separator"></div>
            `;
        });
        urgentHtml += '</div>';
    }
    
    if (regularTasks.length > 0) {
        regularHtml = '<div class="task-section"><div class="section-title">📋 Обычные задачи</div>';
        regularTasks.forEach(t => {
            regularHtml += `
                <div class="task-item regular" onclick="closeModalAndShowTask(${t.id})">
                    <div class="task-icon">ℹ️</div>
                    <div class="task-content">
                        <div class="task-equipment">${escapeHtml(t.equipment)}</div>
                        <div class="task-description">${escapeHtml(t.description || 'Плановое техническое обслуживание')}</div>
                        <div class="task-date">📅 Дата: ${t.due_date || 'Не указана'}</div>
                        <div class="task-type">🔧 Тип: ${t.type === 'Авария' ? 'Аварийный ремонт' : 'Плановое ТО'}</div>
                    </div>
                </div>
                <div class="task-separator"></div>
            `;
        });
        regularHtml += '</div>';
    }
    
    let modal = document.createElement('div');
    modal.className = 'center-modal info large';
    modal.innerHTML = `
        <div class="modal-header">
            <div class="modal-icon-info">📅</div>
            <div class="modal-title">Задачи на сегодня (${tasks.length})</div>
        </div>
        <div class="modal-body">
            ${urgentHtml}
            ${regularHtml}
        </div>
        <div class="modal-footer">
            <button onclick="this.closest('.center-modal').remove()">Понятно</button>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    tasks.forEach(task => {
        if (task.is_urgent) {
            addNotification('⚠️ СРОЧНАЯ ЗАДАЧА НА СЕГОДНЯ', `${task.equipment} - ${task.description || 'ТО'} (${task.due_date})`, 'error', task.id);
        } else {
            addNotification('📅 ЗАДАЧА НА СЕГОДНЯ', `${task.equipment} - ${task.description || 'ТО'} (${task.due_date})`, 'info', task.id);
        }
    });
}

function closeModalAndShowTask(taskId) {
    let modal = document.querySelector('.center-modal');
    if (modal) modal.remove();
    showTab('tasks');
    setTimeout(() => highlightTaskById(taskId), 100);
}

function highlightTaskById(taskId) {
    let rows = document.querySelectorAll('#tasksTableBody tr');
    for (let row of rows) {
        let firstCell = row.cells[0];
        if (firstCell && firstCell.innerText == taskId) {
            rows.forEach(r => r.classList.remove('selected'));
            row.classList.add('selected');
            row.scrollIntoView({ behavior: 'smooth', block: 'center' });
            break;
        }
    }
}

// ========== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ==========

function showConfirmModal(title, message, onConfirm) {
    let oldModal = document.querySelector('.confirm-modal');
    if (oldModal) oldModal.remove();
    
    let modal = document.createElement('div');
    modal.className = 'confirm-modal';
    modal.innerHTML = `
        <div class="confirm-modal-content">
            <div class="confirm-modal-title">${escapeHtml(title)}</div>
            <div class="confirm-modal-text">${escapeHtml(message)}</div>
            <div class="confirm-modal-buttons">
                <button class="confirm-yes">Да</button>
                <button class="confirm-no">Нет</button>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    modal.querySelector('.confirm-yes').onclick = () => { modal.remove(); if (onConfirm) onConfirm(); };
    modal.querySelector('.confirm-no').onclick = () => modal.remove();
}

function showCenterModal(title, text, type = 'error') {
    let oldModal = document.querySelector('.center-modal');
    if (oldModal) oldModal.remove();
    
    let modal = document.createElement('div');
    modal.className = `center-modal ${type}`;
    modal.innerHTML = `
        <div class="modal-title">${escapeHtml(title)}</div>
        <div class="modal-text">${escapeHtml(text)}</div>
        <button onclick="this.parentElement.remove()">OK</button>
    `;
    document.body.appendChild(modal);
}

function animateNumber(elementId, targetValue, suffix = "") {
    let element = document.getElementById(elementId);
    if (!element) return;
    
    let startValue = parseInt(element.innerText) || 0;
    let duration = 500;
    let stepTime = 20;
    let steps = duration / stepTime;
    let stepValue = (targetValue - startValue) / steps;
    let current = startValue;
    let step = 0;
    
    let timer = setInterval(() => {
        step++;
        current += stepValue;
        if (step >= steps) {
            element.innerText = targetValue + suffix;
            clearInterval(timer);
        } else {
            element.innerText = Math.round(current) + suffix;
        }
    }, stepTime);
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

// ========== ПОЛУЧЕНИЕ ДАННЫХ ИЗ C# ==========

window.receiveFromCSharp = function(func, data) {
    console.log("Received from C#:", func, data);
    
    if (func === "displayTasks") displayTasks(data);
    if (func === "displayHistory") displayHistory(data);
    if (func === "displayAccidentHistory") displayAccidentHistory(data);
    if (func === "displayStats") displayStats(data);
    if (func === "displaySpareParts") displaySpareParts(data);
    if (func === "showSuccess") showSuccess(data);
    if (func === "showError") showError(data);
    if (func === "showNewTasks") showNewTasksNotification(data);
    if (func === "showTodayTasksList") showTodayTasksList(data);
    if (func === "checkOverdueTasks") checkOverdueTasks(parseInt(data));
};

// Закрытие панели уведомлений при клике вне
document.addEventListener('click', function(event) {
    let panel = document.getElementById("notificationPanel");
    let btn = document.getElementById("notificationBtn");
    if (panel && panel.style.display === "block") {
        if (btn && btn.contains(event.target)) return;
        if (!panel.contains(event.target)) panel.style.display = "none";
    }
});

// Инициализация
document.addEventListener('DOMContentLoaded', function() {
    loadTasks();
    loadHistory();
    loadAccidentHistory();
    loadStatistics();
});