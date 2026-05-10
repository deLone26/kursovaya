let selectedTaskId = null;
let selectedAccidentId = null;

function selectTaskRow(element, taskId) {
    document.querySelectorAll('#tasksTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
    selectedTaskId = taskId;
}

function selectAccidentRow(element, accidentId) {
    document.querySelectorAll('#accidentsTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    element.classList.add('selected');
    selectedAccidentId = accidentId;
}

let notifications = [];
let unreadCount = 0;

function setCurrentUser(id, login, role, name) {
    document.getElementById("userName").innerText = name;
}

function showTab(name) {
    document.querySelectorAll(".tab-content").forEach(x => x.classList.remove("active"));
    document.querySelectorAll(".tab").forEach(x => x.classList.remove("active"));

    if (name === "tasks") {
        document.getElementById("tasksTab").classList.add("active");
        document.querySelectorAll(".tab")[0].classList.add("active");
    }
    if (name === "history") {
        document.getElementById("historyTab").classList.add("active");
        document.querySelectorAll(".tab")[1].classList.add("active");
    }
    if (name === "accidents") {
        document.getElementById("accidentsTab").classList.add("active");
        document.querySelectorAll(".tab")[2].classList.add("active");
    }
    if (name === "stats") {
        document.getElementById("statsTab").classList.add("active");
        document.querySelectorAll(".tab")[3].classList.add("active");
    }
}

function displayTasks(data) {
    let tasks = JSON.parse(data);
    let body = document.getElementById("tasksTableBody");
    body.innerHTML = "";

    tasks.forEach(t => {
        let type = t.is_accident
            ? `<span class="type-badge type-avariya">Авария</span>`
            : `<span class="type-badge type-to">Техническое обслуживание</span>`;

        let statusHtml = "";

        if (t.status === "Зарегистрирован") {
            statusHtml = `
                <button class="status-btn status-registered" onclick="changeStatus(${t.id})">
                    Принять в работу
                </button>
            `;
        } else if (t.status === "В работе") {
            statusHtml = `
                <span class="status-badge status-working">В работе</span>
            `;
        } else if (t.status === "Завершен") {
            statusHtml = `
                <span class="status-badge status-completed">Завершено</span>
            `;
        } else {
            statusHtml = `
                <span class="status-badge status-registered">${t.status}</span>
            `;
        }

        let reportButton = (t.status === "В работе")
            ? `<button class="report-btn" onclick='openReport(${JSON.stringify(t).replace(/'/g, "\\'")})'>Отчёт</button>`
            : "-";

        body.innerHTML += `
            <tr onclick="selectTaskRow(this, ${t.id})" style="cursor:pointer">
                <td>${t.id}</td>
                <td>${type}</td>
                <td>${t.equipment_name}</td>
                <td>${t.description || '-'}</td>
                <td>${t.due_date || '-'}</td>
                <td>${statusHtml}</td>
                <td>${reportButton}</td>
            </tr>
        `;
    });
}

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
                <button class="confirm-btn confirm-yes">Да</button>
                <button class="confirm-btn confirm-no">Нет</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);

    modal.querySelector('.confirm-yes').onclick = () => {
        modal.remove();
        if (onConfirm) onConfirm();
    };

    modal.querySelector('.confirm-no').onclick = () => {
        modal.remove();
    };
}

function changeStatus(taskId) {
    showConfirmModal(
        "Подтверждение",
        "Вы уверены, что хотите принять эту задачу в работу?",
        function() {
            chrome.webview.postMessage(JSON.stringify({
                action: "changeStatus",
                taskId: taskId
            }));
        }
    );
}

function getSelectedTask() {
    if (selectedTaskId) {
        sendToCSharp('getTaskDetails', { taskId: selectedTaskId });
    } else {
        showCenterModal('Внимание', 'Выберите задачу из списка', 'error');
    }
}

function getSelectedAccident() {
    if (selectedAccidentId) {
        sendToCSharp('getAccidentDetails', { accidentId: selectedAccidentId });
    } else {
        showCenterModal('Внимание', 'Выберите аварию из списка', 'error');
    }
}

function displayHistory(data) {
    let rows = JSON.parse(data);
    rows.sort((a, b) => new Date(b.sort_date) - new Date(a.sort_date));
    let body = document.getElementById("historyTableBody");
    body.innerHTML = "";
    rows.forEach(r => {
        body.innerHTML += `<table>
            <td>${r.completion_date || '-'}</td>
            <td>${escapeHtml(r.equipment || '-')}</td>
            <td>${escapeHtml(r.description || '-')}</td>
            <td>${escapeHtml(r.replaced_part || '-')}</td>
        </tr>`;
    });
}

function displayAccidents(data) {
    let rows = JSON.parse(data);
    rows.sort((a, b) => new Date(b.sort_date) - new Date(a.sort_date));
    let body = document.getElementById("accidentsTableBody");
    body.innerHTML = "";
    rows.forEach(r => {
        body.innerHTML += `<tr>
            <td>${r.date || '-'}</td>
            <td>${escapeHtml(r.equipment || '-')}</td>
            <td>${escapeHtml(r.description || '-')}</td>
            <td>${escapeHtml(r.status || '-')}</td>
        </tr>`;
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
    if (progressFill) {
        progressFill.style.width = percent + "%";
    }

    let avgHours = s.avg || 0;
    let avgText = "";
    if (avgHours >= 24) {
        let days = Math.floor(avgHours / 24);
        let hours = Math.round(avgHours % 24);
        avgText = days + " дн " + hours + " ч";
    } else if (avgHours >= 1) {
        avgText = Math.round(avgHours) + " ч";
    } else if (avgHours > 0) {
        avgText = "< 1 ч";
    } else {
        avgText = "0 ч";
    }

    let avgElement = document.getElementById("statAvg");
    if (avgElement) {
        avgElement.innerHTML = avgText;
    }
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

function openReport(task) {
    document.getElementById("reportModal").style.display = "block";
    document.getElementById("reportTaskId").value = task.id;
    document.getElementById("reportEquipment").value = task.equipment_name;

    let now = new Date();
    let formattedStart = now.getFullYear() + "-" +
        String(now.getMonth() + 1).padStart(2, '0') + "-" +
        String(now.getDate()).padStart(2, '0') + "T" +
        String(now.getHours()).padStart(2, '0') + ":" +
        String(now.getMinutes()).padStart(2, '0');

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

    chrome.webview.postMessage(JSON.stringify({
        action: "submitReport",
        taskId: parseInt(document.getElementById("reportTaskId").value),
        sparePartIds: parts,
        description: description,
        startDate: startDate,
        endDate: endDate
    }));

    closeModal();
}

function loadSpareParts(equipmentId) {
    chrome.webview.postMessage(JSON.stringify({
        action: "loadSpareParts",
        equipmentId: equipmentId
    }));
}

function loadHistory() {
    chrome.webview.postMessage(JSON.stringify({
        action: "loadHistory",
        startDate: document.getElementById("historyStart").value,
        endDate: document.getElementById("historyEnd").value
    }));
}

function logout() {
    chrome.webview.postMessage(JSON.stringify({ action: "logout" }));
}

// ========== УВЕДОМЛЕНИЯ ==========

function toggleNotifications() {
    let panel = document.getElementById("notificationPanel");
    if (!panel) return;

    if (panel.style.display === "block") {
        panel.style.display = "none";
    } else {
        panel.style.display = "block";
    }
}

function addNotification(title, text, type = 'info', taskId = null) {
    let now = new Date();
    let timeText = formatRelativeTime(now);

    let notification = {
        id: Date.now(),
        title: title,
        text: text,
        time: timeText,
        timestamp: now,
        type: type,
        taskId: taskId,
        isRead: false
    };

    notifications.unshift(notification);
    unreadCount++;
    updateNotificationUI();
}

function markAllNotificationsAsRead() {
    unreadCount = 0;
    document.getElementById("notificationCount").innerText = "0";
    document.getElementById("notificationCount").style.display = "none";

    document.querySelectorAll('.notification-item').forEach(item => {
        item.classList.remove('unread');
    });

    notifications.forEach(n => {
        n.isRead = true;
    });
}

function updateNotificationUI() {
    let countEl = document.getElementById("notificationCount");
    countEl.innerText = unreadCount;
    countEl.style.display = unreadCount > 0 ? "flex" : "none";

    let container = document.getElementById("notificationsContainer");

    if (notifications.length === 0) {
        container.innerHTML = `<div class="notification-item" style="text-align:center; color:#9ca3af;">Нет уведомлений</div>`;
        return;
    }

    let html = '';

    for (let i = 0; i < Math.min(notifications.length, 20); i++) {
        let n = notifications[i];
        let unreadClass = !n.isRead ? 'unread' : '';
        let titleClass = '';

        if (n.type === 'error') titleClass = 'critical';
        if (n.type === 'success') titleClass = 'success';

        html += `
            <div class="notification-item ${unreadClass}" 
                 data-id="${n.id}" 
                 data-task-id="${n.taskId || ''}"
                 onclick="onNotificationClick(this)">
                <div class="notification-title ${titleClass}">${escapeHtml(n.title)}</div>
                <div class="notification-text">${escapeHtml(n.text)}</div>
                <div class="notification-time">${n.time}</div>
            </div>
        `;
    }

    html += `
        <div class="notification-footer">
            <a onclick="clearAllNotifications()">Очистить все</a>
        </div>
    `;

    container.innerHTML = html;
}

function onNotificationClick(element) {
    let taskId = element.getAttribute('data-task-id');
    if (taskId && taskId !== '') {
        showTab('tasks');
        highlightTaskById(taskId);
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
    document.getElementById("notificationCount").style.display = "none";
}

function showSuccess(text) {
    addNotification('Успешно', text, 'success');
}

function showError(text) {
    if (text.includes('Просрочен')) {
        addNotification('Просроченные задачи', text, 'error');
    } else if (text.includes('срочная') || text.includes('авария')) {
        addNotification('Внимание!', text, 'error');
    } else {
        console.error(text);
    }
}

function showNewTasksNotification(data) {
    let tasks = typeof data === 'string' ? JSON.parse(data) : data;
    if (tasks && tasks.length > 0) {
        let urgentTasks = tasks.filter(t => t.is_urgent === true);
        let regularTasks = tasks.filter(t => t.is_urgent !== true);

        if (urgentTasks.length > 0) {
            urgentTasks.forEach(task => {
                addNotification('Срочное ТО', `${task.equipment} - срок ${task.due_date}`, 'error', task.id);
            });
        }

        if (regularTasks.length > 0) {
            addNotification('Новое техническое обслуживание', `${regularTasks.length} задач(а)`, 'info', regularTasks[0]?.id);
        }
    }
}

// Закрытие панели при клике вне её
document.addEventListener('click', function(event) {
    let panel = document.getElementById("notificationPanel");
    let btn = document.getElementById("notificationBtn");

    if (panel && panel.style.display === "block") {
        if (btn && btn.contains(event.target)) {
            return;
        }
        if (!panel.contains(event.target)) {
            panel.style.display = "none";
        }
    }
});

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

function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/[&<>]/g, function(m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
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

    setTimeout(() => {
        if (modal) modal.remove();
    }, 3000);
}

function formatRelativeTime(date) {
    let now = new Date();
    let diff = Math.floor((now - date) / 1000 / 60);

    if (diff < 1) return "только что";
    if (diff < 60) return `${diff} мин назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч назад`;
    return `${Math.floor(diff / 1440)} дн назад`;
}

// ========== ПОЛУЧЕНИЕ ДАННЫХ ИЗ C# ==========

window.receiveFromCSharp = function(func, data) {
    if (func === "displayTasks") displayTasks(data);
    if (func === "displayHistory") displayHistory(data);
    if (func === "displayAccidents") displayAccidents(data);
    if (func === "displayStats") displayStats(data);
    if (func === "showSuccess") showSuccess(data);
    if (func === "showError") showError(data);
    if (func === "displaySpareParts") {
        let select = document.getElementById("reportParts");
        let parts = JSON.parse(data);
        select.innerHTML = '';
        if (parts.length === 0) {
            select.innerHTML = '<option disabled>Нет доступных запчастей</option>';
        } else {
            parts.forEach(p => {
                let option = document.createElement('option');
                option.value = p.id;
                option.text = p.name;
                select.appendChild(option);
            });
        }
    }
};

// Инициализация после загрузки DOM
document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM загружен");
    let notifBtn = document.getElementById("notificationBtn");
    console.log("Кнопка уведомлений:", notifBtn);
});