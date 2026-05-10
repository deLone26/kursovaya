let selectedTaskId = null;
let selectedAccidentId = null;

function selectTaskRow(element, taskId) {
    // Убираем выделение со всех строк
    document.querySelectorAll('#tasksTableBody tr').forEach(row => {
        row.classList.remove('selected');
    });
    // Выделяем текущую строку
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

function setCurrentUser(id,login,role,name){
    document.getElementById("userName").innerText=name;
}

function showTab(name){

    document.querySelectorAll(".tab-content")
        .forEach(x=>x.classList.remove("active"));

    document.querySelectorAll(".tab")
        .forEach(x=>x.classList.remove("active"));

    if(name==="tasks"){
        document.getElementById("tasksTab").classList.add("active");
        document.querySelectorAll(".tab")[0].classList.add("active");
    }

    if(name==="history"){
        document.getElementById("historyTab").classList.add("active");
        document.querySelectorAll(".tab")[1].classList.add("active");
    }

    if(name==="accidents"){
        document.getElementById("accidentsTab").classList.add("active");
        document.querySelectorAll(".tab")[2].classList.add("active");
    }

    if(name==="stats"){
        document.getElementById("statsTab").classList.add("active");
        document.querySelectorAll(".tab")[3].classList.add("active");
    }
}

function displayTasks(data){

    let tasks=JSON.parse(data);

    let body=document.getElementById("tasksTableBody");

    body.innerHTML="";

    tasks.forEach(t=>{

        let type=t.is_accident
            ? `<span class="type-badge type-avariya">Авария</span>`
            : `<span class="type-badge type-to">Техническое обслуживание</span>`;

        // Определяем, какая кнопка статуса нужна
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


function getSelectedTask() {
    if (selectedTaskId) {
        sendToCSharp('getTaskDetails', { taskId: selectedTaskId });
    } else {
        showToast('Выберите задачу из списка', 'warning');
    }
}

function getSelectedAccident() {
    if (selectedAccidentId) {
        sendToCSharp('getAccidentDetails', { accidentId: selectedAccidentId });
    } else {
        showToast('Выберите аварию из списка', 'warning');
    }
}

function displayHistory(data){

    let rows=JSON.parse(data);

    rows.sort((a,b)=>{

        return new Date(b.sort_date)-new Date(a.sort_date);
    });

    let body=document.getElementById("historyTableBody");

    body.innerHTML="";

    rows.forEach(r=>{

        body.innerHTML+=`
            <tr>
                <td>${r.completion_date}</td>
                <td>${r.equipment}</td>
                <td>${r.description}</td>
                <td>${r.replaced_part}</td>
            </tr>
        `;
    });
}

function displayAccidents(data){

    let rows=JSON.parse(data);

    rows.sort((a,b)=>{

        return new Date(b.sort_date)-new Date(a.sort_date);
    });

    let body=document.getElementById("accidentsTableBody");

    body.innerHTML="";

    rows.forEach(r=>{

        body.innerHTML+=`
            <tr>
                <td>${r.date}</td>
                <td>${r.equipment}</td>
                <td>${r.description}</td>
                <td>${r.status}</td>
            </tr>
        `;
    });
}

function displayStats(data) {
    let s = JSON.parse(data);
    
    // Анимированное обновление чисел
    animateNumber("statTotal", s.total || 0);
    animateNumber("statDone", s.completed || 0);
    animateNumber("statWork", s.inwork || 0);
    animateNumber("statOverdue", s.overdue || 0);
    animateNumber("statUrgent", s.urgent || 0);
    animateNumber("statToday", s.today || 0);
    
    let percent = s.percent || 0;
    animateNumber("statPercent", percent, "%");
    
    // Обновляем прогресс-бар
    let progressFill = document.getElementById("progressFill");
    if (progressFill) {
        progressFill.style.width = percent + "%";
    }
    
    // Среднее время ремонта
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

// Функция анимации чисел
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
    
    // Загружаем запчасти
    loadSpareParts(task.equipment_id);
}

function closeModal(){
    document.getElementById("reportModal").style.display="none";
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
    
    // Проверка: дата начала не может быть позже даты окончания
    if (startDate && endDate && new Date(startDate) > new Date(endDate)) {
        showCenterModal("Ошибка валидации", "Дата начала не может быть позже даты окончания!", "error");
        return;
    }
    
    if (parts.length === 0) {
        showCenterModal("Ошибка", "Выберите хотя бы одну заменённую деталь!", "error");
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

function loadHistory(){

    chrome.webview.postMessage(
        JSON.stringify({

            action:"loadHistory",

            startDate:
                document.getElementById("historyStart").value,

            endDate:
                document.getElementById("historyEnd").value
        }));
}

function logout(){

    chrome.webview.postMessage(
        JSON.stringify({
            action:"logout"
        }));
}

function toggleNotifications() {
    let panel = document.getElementById("notificationPanel");
    
    if (panel.style.display === "block") {
        panel.style.display = "none";
    } else {
        panel.style.display = "block";
        markAllNotificationsAsRead();
    }
}

function addNotification(title, text, type = 'info') {
    let now = new Date();
    let timeText = formatRelativeTime(now);
    
    let notification = {
        id: Date.now(),
        title: title,
        text: text,
        time: timeText,
        timestamp: now,
        type: type,
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
    
    // Визуально снимаем пометку unread со всех уведомлений
    document.querySelectorAll('.notification-item').forEach(item => {
        item.classList.remove('unread');
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
            <div class="notification-item ${unreadClass}" data-id="${n.id}" onclick="markNotificationRead(${n.id})">
                <div class="notification-title ${titleClass}">${escapeHtml(n.title)}</div>
                <div class="notification-text">${escapeHtml(n.text)}</div>
                <div class="notification-time">${n.time}</div>
            </div>
        `;
    }
    
    html += `
        <div class="notification-footer">
            <a onclick="clearAllNotifications()">Все уведомления</a>
            <div><button class="clear-all-btn" onclick="clearAllNotifications()">Очистить все</button></div>
        </div>
    `;
    
    container.innerHTML = html;
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

// Переопределяем showSuccess и showError для красивого отображения
function showSuccess(text) {
    addNotification('Успешно', text, 'success');
}

function showError(text) {
    if (text.includes('срочная') || text.includes('авария')) {
        addNotification('🚨 Новая авария!', text, 'error');
    } else if (text.includes('Просрочен')) {
        addNotification('⚠️ Просроченные задачи', text, 'error');
    } else {
        addNotification('Ошибка', text, 'error');
    }
}

function showNewTasksNotification(data) {
    let tasks = typeof data === 'string' ? JSON.parse(data) : data;
    if (tasks && tasks.length > 0) {
        // Отделяем срочные и обычные ТО
        let urgentTasks = tasks.filter(t => t.is_urgent === true);
        let regularTasks = tasks.filter(t => t.is_urgent !== true);
        
        if (urgentTasks.length > 0) {
            urgentTasks.forEach(task => {
                addNotification('Срочное ТО', `${task.equipment} - срок ${task.due_date}`, 'error');
            });
        }
        
        if (regularTasks.length > 0) {
            addNotification('Новое техническое обслуживание', `${regularTasks.length} задач(а)`, 'info');
        }
        
        loadTasks();
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
    // Удаляем старые модальные окна
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
    
    // Автоматическое закрытие через 3 секунды
    setTimeout(() => {
        if (modal) modal.remove();
    }, 3000);
}

// Функция для получения названия типа уведомления
function getNotificationTypeText(type) {
    switch(type) {
        case 'error': return 'критическое';
        case 'success': return 'успех';
        default: return 'информация';
    }
}

function formatRelativeTime(date) {
    let now = new Date();
    let diff = Math.floor((now - date) / 1000 / 60);
    
    if (diff < 1) return "только что";
    if (diff < 60) return `${diff} мин назад`;
    if (diff < 1440) return `${Math.floor(diff / 60)} ч назад`;
    return `${Math.floor(diff / 1440)} дн назад`;
}

function showSuccess(text){
    addNotification(text);
}

function showError(text) {
    // Показываем ошибки только если они действительно важны для пользователя
    if (text.includes('Просрочен')) {
        addNotification('Просроченные задачи', text, 'error');
    } else if (text.includes('срочная') || text.includes('авария')) {
        addNotification('Внимание!', text, 'error');
    } else {
        // Остальные ошибки не показываем в уведомлениях
        console.error(text);
    }
}

window.receiveFromCSharp=function(func,data){
    if(func==="displayTasks") displayTasks(data);
    if(func==="displayHistory") displayHistory(data);
    if(func==="displayAccidents") displayAccidents(data);
    if(func==="displayStats") displayStats(data);
    if(func==="showSuccess") showSuccess(data);
    if(func==="showError") showError(data);
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