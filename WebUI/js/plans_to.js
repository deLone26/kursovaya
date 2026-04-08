// Глобальные переменные
let employees = [];
let plans = [];
let currentView = 'day';
let currentDate = new Date();
let selectedDate = new Date();
let selectedEmployeeId = null;

// Инициализация
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    renderCalendar();
    loadInitialData();
});

function setupEventListeners() {
    document.getElementById('prevBtn').addEventListener('click', () => navigateDate(-1));
    document.getElementById('nextBtn').addEventListener('click', () => navigateDate(1));
    document.getElementById('searchBtn').addEventListener('click', searchPlans);
    document.getElementById('exportBtn').addEventListener('click', exportToExcel);
    document.getElementById('searchInput').addEventListener('keypress', (e) => {
        if (e.key === 'Enter') searchPlans();
    });
    
    document.querySelectorAll('.view-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            document.querySelectorAll('.view-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentView = btn.getAttribute('data-view');
            renderCalendar();
            loadPlans();
        });
    });
    
    document.querySelectorAll('.filter-all, .filter-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            document.querySelectorAll('.filter-all, .filter-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const filter = btn.getAttribute('data-filter');
            filterEmployees(filter);
        });
    });
}

function navigateDate(delta) {
    if (currentView === 'day') currentDate.setDate(currentDate.getDate() + delta);
    else if (currentView === 'week') currentDate.setDate(currentDate.getDate() + delta * 7);
    else if (currentView === 'month') currentDate.setMonth(currentDate.getMonth() + delta);
    else if (currentView === '3days') currentDate.setDate(currentDate.getDate() + delta * 3);
    else if (currentView === '8days') currentDate.setDate(currentDate.getDate() + delta * 8);
    
    selectedDate = new Date(currentDate);
    renderCalendar();
    loadPlans();
}

function renderCalendar() {
    const container = document.getElementById('calendarContainer');
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startOffset = (firstDay.getDay() + 6) % 7;
    
    let html = '<div class="calendar-grid">';
    const weekDays = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];
    weekDays.forEach(day => {
        html += `<div class="calendar-day-name">${day}</div>`;
    });
    
    for (let i = 0; i < startOffset; i++) {
        html += '<div class="calendar-day empty"></div>';
    }
    
    for (let day = 1; day <= lastDay.getDate(); day++) {
        const date = new Date(year, month, day);
        const isWeekend = date.getDay() === 0 || date.getDay() === 6;
        const isSelected = selectedDate && selectedDate.getDate() === day && selectedDate.getMonth() === month;
        const hasTasks = plans.some(p => {
            const planDate = new Date(p.start_date);
            return planDate.getDate() === day && planDate.getMonth() === month;
        });
        
        html += `
            <div class="calendar-day ${isWeekend ? 'weekend' : ''} ${isSelected ? 'selected' : ''} ${hasTasks ? 'has-tasks' : ''}" data-date="${year}-${month + 1}-${day}">
                <div class="day-number">${day}</div>
            </div>
        `;
    }
    
    html += '</div>';
    container.innerHTML = html;
    
    document.querySelectorAll('.calendar-day[data-date]').forEach(day => {
        day.addEventListener('click', (e) => {
            const dateStr = day.getAttribute('data-date');
            selectedDate = new Date(dateStr);
            renderCalendar();
            loadPlans();
        });
    });
    
    const monthNames = ['января', 'февраля', 'марта', 'апреля', 'мая', 'июня', 'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря'];
    document.getElementById('currentDateRange').textContent = `${currentDate.getDate()} ${monthNames[currentDate.getMonth()]} ${currentDate.getFullYear()}`;
}

function loadInitialData() {
    window.chrome.webview.postMessage(JSON.stringify({ action: 'getInitialData' }));
}

function loadPlans() {
    const params = {
        action: 'getPlans',
        startDate: selectedDate.toISOString().split('T')[0],
        view: currentView
    };
    if (selectedEmployeeId) params.employeeId = selectedEmployeeId;
    window.chrome.webview.postMessage(JSON.stringify(params));
}

function loadUrgentReminders() {
    window.chrome.webview.postMessage(JSON.stringify({ action: 'getUrgentReminders' }));
}

function renderEmployees(data) {
    const container = document.getElementById('employeesList');
    employees = data;
    
    if (!employees.length) {
        container.innerHTML = '<div class="loading">Нет сотрудников</div>';
        return;
    }
    
    container.innerHTML = employees.map(emp => `
        <div class="employee-item ${selectedEmployeeId === emp.id ? 'active' : ''}" data-id="${emp.id}">
            <span class="employee-name">${emp.fio}</span>
            <span class="employee-count">${emp.tasks_count || 0}</span>
        </div>
    `).join('');
    
    document.querySelectorAll('.employee-item').forEach(item => {
        item.addEventListener('click', () => {
            const id = parseInt(item.getAttribute('data-id'));
            selectedEmployeeId = selectedEmployeeId === id ? null : id;
            renderEmployees(employees);
            loadPlans();
        });
    });
}

function renderPlans(data) {
    plans = data;
    renderTimeline();
    updateStats();
}

function renderUrgentReminders(data) {
    const container = document.getElementById('urgentList');
    
    if (!data.length) {
        container.innerHTML = '<div class="loading">Нет срочных напоминаний</div>';
        return;
    }
    
    container.innerHTML = data.map(item => {
        let daysClass = '';
        let itemClass = '';
        if (item.days_left < 0) {
            itemClass = 'critical';
            daysClass = 'days-critical';
        } else if (item.days_left <= 2) {
            itemClass = 'critical';
            daysClass = 'days-critical';
        } else if (item.days_left <= 5) {
            itemClass = 'warning';
            daysClass = 'days-warning';
        }
        
        const daysText = item.days_left < 0 ? `Просрочен на ${Math.abs(item.days_left)} дн.` : `${item.days_left} дн.`;
        
        return `
            <div class="urgent-item ${itemClass}">
                <div class="urgent-date">📅 Срок: ${item.end_date}</div>
                <div class="urgent-title">${item.equipment}</div>
                <div class="urgent-equipment">📋 ${item.tip}</div>
                <div class="urgent-days ${daysClass}">⏰ ${daysText}</div>
            </div>
        `;
    }).join('');
}

function renderTimeline() {
    const container = document.getElementById('timelineContainer');
    const selectedPlans = plans.filter(p => {
        const planDate = new Date(p.start_date);
        return planDate.toDateString() === selectedDate.toDateString();
    });
    
    let html = `
        <div class="timeline-header">
            <div class="timeline-title">📊 График работ на ${selectedDate.toLocaleDateString('ru-RU')}</div>
        </div>
        <div class="timeline-wrapper">
            <div class="hours-column">
    `;
    
    for (let hour = 0; hour < 24; hour++) {
        html += `<div class="hour-marker">${hour.toString().padStart(2, '0')}:00</div>`;
    }
    
    html += `</div><div class="events-column" style="position: relative; min-height: 1440px;">`;
    
    selectedPlans.forEach(plan => {
        const startDate = new Date(plan.start_date);
        const endDate = new Date(plan.end_date);
        const startMinutes = startDate.getHours() * 60 + startDate.getMinutes();
        const durationMinutes = (endDate - startDate) / (1000 * 60);
        
        let priorityClass = '';
        if (plan.is_overdue) priorityClass = 'critical';
        else if (plan.days_left <= 3) priorityClass = 'warning';
        
        html += `
            <div class="event-item ${priorityClass}" style="top: ${startMinutes}px; height: ${durationMinutes}px;">
                <div class="event-title">${plan.equipment} - ${plan.tip || 'ТО'}</div>
                <div class="event-time">${startDate.toLocaleTimeString('ru-RU', {hour:'2-digit', minute:'2-digit'})} - ${endDate.toLocaleTimeString('ru-RU', {hour:'2-digit', minute:'2-digit'})}</div>
                <div class="event-responsible">👤 ${plan.responsible || 'Не назначен'}</div>
            </div>
        `;
    });
    
    // Current time line
    const now = new Date();
    if (selectedDate.toDateString() === now.toDateString()) {
        const nowMinutes = now.getHours() * 60 + now.getMinutes();
        html += `<div class="current-time-line" style="top: ${nowMinutes}px;"></div>`;
    }
    
    html += `</div></div>`;
    container.innerHTML = html;
}

function updateStats() {
    const todayPlans = plans.filter(p => {
        const planDate = new Date(p.start_date);
        return planDate.toDateString() === selectedDate.toDateString();
    });
    
    const overduePlans = plans.filter(p => p.is_overdue === true);
    const criticalPlans = plans.filter(p => p.days_left <= 2 && p.days_left >= 0 && !p.is_overdue);
    
    document.getElementById('totalToday').textContent = todayPlans.length;
    document.getElementById('overdueCount').textContent = overduePlans.length;
    document.getElementById('criticalCount').textContent = criticalPlans.length;
}

function filterEmployees(filter) {
    if (filter === 'all') {
        selectedEmployeeId = null;
    } else if (filter === 'without') {
        const withoutEmp = employees.find(e => e.fio === 'Без ответственного');
        selectedEmployeeId = withoutEmp ? withoutEmp.id : null;
    }
    renderEmployees(employees);
    loadPlans();
}

function searchPlans() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    if (!searchTerm) {
        loadPlans();
        return;
    }
    
    const filtered = plans.filter(p => 
        p.equipment?.toLowerCase().includes(searchTerm) ||
        p.tip?.toLowerCase().includes(searchTerm) ||
        p.responsible?.toLowerCase().includes(searchTerm)
    );
    renderPlans(filtered);
}

function exportToExcel() {
    window.chrome.webview.postMessage(JSON.stringify({ action: 'exportToExcel' }));
}

// Receive messages from C#
window.receiveFromCSharp = function(command, data) {
    switch(command) {
        case 'employeesData':
            renderEmployees(data);
            break;
        case 'plansData':
            renderPlans(data);
            break;
        case 'urgentData':
            renderUrgentReminders(data);
            break;
        case 'initialData':
            renderEmployees(data.employees);
            renderPlans(data.plans);
            renderUrgentReminders(data.urgent);
            break;
        case 'showError':
            console.error('Ошибка: ' + data);
            break;
    }
};