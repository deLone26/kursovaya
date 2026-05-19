let currentRole = '';

window.setUserRole = function(role) {
    currentRole = role;
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
            action: 'exportReportDirectly', 
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
    
    document.getElementById('statTotalPlans').innerHTML = stats.totalPlans || 0;
    document.getElementById('statCompletedPlans').innerHTML = stats.completedPlans || 0;
    document.getElementById('statOverduePlans').innerHTML = stats.overduePlans || 0;
    
    const total = stats.totalPlans || 0;
    const completed = stats.completedPlans || 0;
    let percent = total > 0 ? Math.round((completed / total) * 100) : 0;
    if (percent > 100) percent = 100;
    
    const percentEl = document.getElementById('statPercent');
    percentEl.innerHTML = `${percent}%`;
    if (percent >= 80) percentEl.style.color = '#10b981';
    else if (percent >= 50) percentEl.style.color = '#f59e0b';
    else percentEl.style.color = '#dc2626';
};

document.addEventListener('DOMContentLoaded', () => {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ 
            action: 'mainPageReady' 
        }));
    }
});