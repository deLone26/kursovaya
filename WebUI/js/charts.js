let statusChart = null;
let workersChart = null;
let monthlyChart = null;
let overdueTOChart = null;
let overdueAccidentChart = null;
let accidentsChart = null;
let workerStatusChart = null;

document.addEventListener('DOMContentLoaded', function() {
    setDefaultDates();
    setupEventListeners();
    loadEmployees();
    loadChartData();
});

function setDefaultDates() {
    const today = new Date();
    const sixMonthsAgo = new Date(today.getFullYear(), today.getMonth() - 5, 1);
    
    const startDate = document.getElementById('startDate');
    const endDate = document.getElementById('endDate');
    
    if (startDate) startDate.value = sixMonthsAgo.toISOString().split('T')[0];
    if (endDate) endDate.value = today.toISOString().split('T')[0];
}

function setupEventListeners() {
    const refreshBtn = document.getElementById('refreshBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', function() {
            loadChartData();
        });
    }
}

function loadEmployees() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'getEmployees'
        }));
    }
}

function loadChartData() {
    const startDate = document.getElementById('startDate')?.value || '';
    const endDate = document.getElementById('endDate')?.value || '';
    const employeeId = document.getElementById('employeeFilter')?.value || '0';
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'getChartData',
            startDate: startDate,
            endDate: endDate,
            employeeId: parseInt(employeeId)
        }));
    }
}

window.receiveFromCSharp = function(command, data) {
    console.log('Received:', command, data);
    if (command === 'chartData') {
        updateCharts(data);
    } else if (command === 'fillEmployees') {
        fillEmployees(data);
    } else if (command === 'showError') {
        console.error('Ошибка:', data);
    }
};

function fillEmployees(data) {
    const select = document.getElementById('employeeFilter');
    if (!select) return;
    
    let items = typeof data === 'string' ? JSON.parse(data) : data;
    select.innerHTML = '<option value="0">Все сотрудники</option>';
    if (Array.isArray(items)) {
        items.forEach(item => {
            select.innerHTML += `<option value="${item.id}">${escapeHtml(item.name)}</option>`;
        });
    }
}

function updateCharts(data) {
    let info = typeof data === 'string' ? JSON.parse(data) : data;
    
    console.log('Chart data:', info);
    
    // Статистика на карточках
    document.getElementById('totalPlansStat').innerText = info.totalPlans || 0;
    document.getElementById('completedPlansStat').innerText = info.completedPlans || 0;
    document.getElementById('inProgressPlansStat').innerText = info.inProgressPlans || 0;
    document.getElementById('overduePlansStat').innerText = info.overduePlans || 0;
    document.getElementById('totalAccidentsStat').innerText = info.totalAccidents || 0;
    document.getElementById('overdueAccidentsStat').innerText = info.overdueAccidents || 0;
    
    // 1. Статусы планов ТО (круговая)
    if (statusChart) statusChart.destroy();
    const statusCtx = document.getElementById('statusChart').getContext('2d');
    statusChart = new Chart(statusCtx, {
        type: 'doughnut',
        data: {
            labels: ['📋 Зарегистрированы', '⚙️ В работе', '⚠️ Просрочены'],
            datasets: [{
                data: [info.registeredPlans || 0, info.inProgressPlans || 0, info.overduePlans || 0],
                backgroundColor: ['#f59e0b', '#3b82f6', '#ef4444'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: { legend: { position: 'bottom', labels: { font: { size: 11 } } } }
        }
    });
    
    // 2. Загрузка сотрудников (горизонтальная)
    if (workersChart) workersChart.destroy();
    const workersCtx = document.getElementById('workersChart').getContext('2d');
    const workerNames = info.workerStats?.map(w => w.name) || [];
    const workerAssigned = info.workerStats?.map(w => w.assigned) || [];
    workersChart = new Chart(workersCtx, {
        type: 'bar',
        data: {
            labels: workerNames,
            datasets: [{
                label: 'Назначено задач',
                data: workerAssigned,
                backgroundColor: '#3b82f6',
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: 'y',
            scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
    
    // 3. Динамика выполнения планов ТО
    if (monthlyChart) monthlyChart.destroy();
    const monthlyCtx = document.getElementById('monthlyChart').getContext('2d');
    const monthlyLabels = info.monthlyData?.map(m => m.month) || [];
    const monthlyPercent = info.monthlyData?.map(m => m.percent || 0) || [];
    monthlyChart = new Chart(monthlyCtx, {
        type: 'line',
        data: {
            labels: monthlyLabels,
            datasets: [{
                label: 'Процент выполнения (%)',
                data: monthlyPercent,
                borderColor: '#22c55e',
                backgroundColor: 'rgba(34, 197, 94, 0.1)',
                fill: true,
                tension: 0.3
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: { y: { beginAtZero: true, max: 100, ticks: { callback: v => v + '%' } } }
        }
    });
    
    // 4. Просрочки ТО по месяцам
    if (overdueTOChart) overdueTOChart.destroy();
    const overdueTOCtx = document.getElementById('overdueTOChart').getContext('2d');
    const overdueTOData = info.monthlyData?.map(m => m.overdueCount || 0) || [];
    overdueTOChart = new Chart(overdueTOCtx, {
        type: 'bar',
        data: {
            labels: monthlyLabels,
            datasets: [{
                label: 'Количество просроченных ТО',
                data: overdueTOData,
                backgroundColor: '#ef4444',
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
    
    // 5. Просрочки аварий по месяцам
    if (overdueAccidentChart) overdueAccidentChart.destroy();
    const overdueAccidentCtx = document.getElementById('overdueAccidentChart').getContext('2d');
    const accidentMonthlyLabels = info.accidentMonthlyData?.map(m => m.month) || [];
    const overdueAccidentData = info.accidentMonthlyData?.map(m => m.overdueCount || 0) || [];
    overdueAccidentChart = new Chart(overdueAccidentCtx, {
        type: 'bar',
        data: {
            labels: accidentMonthlyLabels,
            datasets: [{
                label: 'Количество просроченных аварий',
                data: overdueAccidentData,
                backgroundColor: '#f97316',
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
    
    // 6. Аварии по оборудованию
    if (accidentsChart) accidentsChart.destroy();
    const accidentsCtx = document.getElementById('accidentsChart').getContext('2d');
    const accidentsLabels = info.topAccidentsByEquipment?.map(a => a.name) || [];
    const accidentsData = info.topAccidentsByEquipment?.map(a => a.count) || [];
    accidentsChart = new Chart(accidentsCtx, {
        type: 'bar',
        data: {
            labels: accidentsLabels,
            datasets: [{
                label: 'Количество аварий',
                data: accidentsData,
                backgroundColor: '#ef4444',
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: 'y',
            scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
    
    // 7. Статусы задач по сотрудникам
    if (workerStatusChart) workerStatusChart.destroy();
    const workerStatusCtx = document.getElementById('workerStatusChart').getContext('2d');
    const workerStatusNames = info.workerStatusStats?.map(w => w.name) || [];
    const workerRegistered = info.workerStatusStats?.map(w => w.registered || 0) || [];
    const workerInProgressStatus = info.workerStatusStats?.map(w => w.inProgress || 0) || [];
    const workerOverdueStatus = info.workerStatusStats?.map(w => w.overdue || 0) || [];
    
    workerStatusChart = new Chart(workerStatusCtx, {
        type: 'bar',
        data: {
            labels: workerStatusNames,
            datasets: [
                { label: 'Зарегистрировано', data: workerRegistered, backgroundColor: '#f59e0b', borderRadius: 6 },
                { label: 'В работе', data: workerInProgressStatus, backgroundColor: '#3b82f6', borderRadius: 6 },
                { label: 'Просрочено', data: workerOverdueStatus, backgroundColor: '#ef4444', borderRadius: 6 }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } },
            plugins: { legend: { position: 'top' } }
        }
    });
}

function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/[&<>]/g, m => m === '&' ? '&amp;' : (m === '<' ? '&lt;' : '&gt;'));
}