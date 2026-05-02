let statusChart = null;
let workersChart = null;
let monthlyChart = null;
let costChart = null;
let accidentsChart = null;

document.addEventListener('DOMContentLoaded', function() {
    setDefaultDates();
    setupEventListeners();
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
        refreshBtn.addEventListener('click', loadChartData);
    }
}

function loadChartData() {
    const startDate = document.getElementById('startDate')?.value || '';
    const endDate = document.getElementById('endDate')?.value || '';
    
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'getChartData',
            startDate: startDate,
            endDate: endDate
        }));
    }
}

window.receiveFromCSharp = function(command, data) {
    if (command === 'chartData') {
        updateCharts(data);
    } else if (command === 'showError') {
        console.error('Ошибка:', data);
    }
};

function updateCharts(data) {
    let info = typeof data === 'string' ? JSON.parse(data) : data;
    
    document.getElementById('totalPlansStat').innerText = info.totalPlans || 0;
    document.getElementById('completedPlansStat').innerText = info.completedPlans || 0;
    document.getElementById('inProgressPlansStat').innerText = info.inProgressPlans || 0;
    document.getElementById('overduePlansStat').innerText = info.overduePlans || 0;
    
    // 1. Статусы планов ТО
    if (statusChart) statusChart.destroy();
    const statusCtx = document.getElementById('statusChart').getContext('2d');
    statusChart = new Chart(statusCtx, {
        type: 'doughnut',
        data: {
            labels: ['Выполнено', 'В работе', 'Просрочено'],
            datasets: [{
                data: [info.completedPlans || 0, info.inProgressPlans || 0, info.overduePlans || 0],
                backgroundColor: ['#22c55e', '#3b82f6', '#ef4444'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: { legend: { position: 'bottom' } }
        }
    });
    
    // 2. Загрузка сотрудников
    if (workersChart) workersChart.destroy();
    const workersCtx = document.getElementById('workersChart').getContext('2d');
    const workerNames = info.workerStats?.map(w => w.name) || [];
    const workerAssigned = info.workerStats?.map(w => w.assigned) || [];
    const workerCompleted = info.workerStats?.map(w => w.completed) || [];
    workersChart = new Chart(workersCtx, {
        type: 'bar',
        data: {
            labels: workerNames,
            datasets: [
                { label: 'Назначено работ', data: workerAssigned, backgroundColor: '#3b82f6', borderRadius: 8 },
                { label: 'Выполнено', data: workerCompleted, backgroundColor: '#22c55e', borderRadius: 8 }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
    
    // 3. Динамика выполнения (проценты)
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
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: { callback: function(v) { return v + '%'; } }
                }
            }
        }
    });
    
    // 4. Стоимость ремонтов по месяцам
    if (costChart) costChart.destroy();
    const costCtx = document.getElementById('costChart').getContext('2d');
    const costData = info.monthlyData?.map(m => m.cost || 0) || [];
    costChart = new Chart(costCtx, {
        type: 'bar',
        data: {
            labels: monthlyLabels,
            datasets: [{
                label: 'Стоимость ремонтов (руб)',
                data: costData,
                backgroundColor: '#10b981',
                borderRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { callback: function(v) { return v.toLocaleString(); } }
                }
            }
        }
    });
    
    // 5. Аварии по оборудованию
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
                borderRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: 'y',
            scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } }
        }
    });
}