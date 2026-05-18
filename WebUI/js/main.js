let currentRole = '';

window.setUserRole = function(role) {
    currentRole = role;
    const roleNames = {
        'app_admin': 'Администратор',
        'app_boss': 'Начальник цеха',
        'app_slesar': 'Слесарь',
        'app_operator': 'Оператор'
    };
    const roleName = roleNames[role] || 'Сотрудник';
    const userGreeting = document.getElementById('userGreeting');
    if (userGreeting) {
        userGreeting.innerHTML = `👋 Здравствуйте, ${roleName}!`;
    }
    loadStatistics();
};

function openSection(section) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'openSection', 
            section: section 
        }));
    }
}

function openReport(reportType) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'openReport', 
            reportType: reportType 
        }));
    }
}

function openReference(refType) {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'openReference', 
            refType: refType 
        }));
    }
}

function loadStatistics() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'loadMainStatistics' 
        }));
    }
}

window.displayMainStatistics = function(data) {
    const stats = typeof data === 'string' ? JSON.parse(data) : data;
    
    const totalPlansEl = document.getElementById('statTotalPlans');
    const completedPlansEl = document.getElementById('statCompletedPlans');
    const overduePlansEl = document.getElementById('statOverduePlans');
    const percentEl = document.getElementById('statPercent');
    
    if (totalPlansEl) totalPlansEl.innerHTML = stats.totalPlans || 0;
    if (completedPlansEl) completedPlansEl.innerHTML = stats.completedPlans || 0;
    if (overduePlansEl) overduePlansEl.innerHTML = stats.overduePlans || 0;
    
    const total = stats.totalPlans || 0;
    const completed = stats.completedPlans || 0;
    let percent = 0;
    
    if (total > 0) {
        percent = Math.round((completed / total) * 100);
        if (percent > 100) percent = 100;
    }
    
    if (percentEl) {
        percentEl.innerHTML = `${percent}%`;
        if (percent >= 80) percentEl.style.color = '#10b981';
        else if (percent >= 50) percentEl.style.color = '#f59e0b';
        else percentEl.style.color = '#dc2626';
    }
};

document.addEventListener('DOMContentLoaded', () => {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'mainPageReady' 
        }));
    }
});