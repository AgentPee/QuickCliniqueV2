// Analytics & Reporting JavaScript

let charts = {};
let analyticsData = {};

// Initialize analytics when page loads
document.addEventListener('DOMContentLoaded', function() {
    loadAnalyticsData();
    
    // Add event listener for time range change
    const timeRangeSelect = document.getElementById('volumeTimeRange');
    if (timeRangeSelect) {
        timeRangeSelect.addEventListener('change', function() {
            loadAnalyticsData();
        });
    }
});

// Load analytics data from server
async function loadAnalyticsData() {
    try {
        const timeRange = document.getElementById('volumeTimeRange')?.value || 30;
        
        // For now, we'll use mock data since we don't have a backend endpoint yet
        // In a real implementation, you would fetch from an API endpoint
        analyticsData = await fetchAnalyticsData(timeRange);
        
        updateOverviewCards();
        renderCharts();
    } catch (error) {
        console.error('Error loading analytics data:', error);
    }
}

// Fetch analytics data from API
async function fetchAnalyticsData(timeRange) {
    try {
        const response = await fetch(`/Clinicstaff/GetAnalyticsData?timeRange=${timeRange}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            return processAnalyticsData(result.data);
        } else {
            console.error('Failed to fetch analytics data:', result.error);
            return getEmptyData();
        }
    } catch (error) {
        console.error('Error fetching analytics data:', error);
        return getEmptyData();
    }
}

// Process and format analytics data from API
function processAnalyticsData(data) {
    // Process appointment volume - ensure proper formatting
    const appointmentVolume = data.appointmentVolume.map(item => ({
        date: item.date,
        day: item.day,
        count: item.count
    }));

    // Process age distribution - ensure all age groups are represented
    const ageGroups = ['Under 18', '18-25', '26-35', '36-45', '46-55', '56-65', '65+'];
    const ageDistribution = {};
    ageGroups.forEach(group => {
        ageDistribution[group] = data.ageDistribution[group] || 0;
    });

    // Process visit frequency - group 5+ visits together
    const visitFrequency = {};
    Object.keys(data.visitFrequency || {}).forEach(key => {
        const visitCount = parseInt(key);
        if (visitCount >= 5) {
            if (!visitFrequency['5+']) {
                visitFrequency['5+'] = 0;
            }
            visitFrequency['5+'] += data.visitFrequency[key];
        } else {
            visitFrequency[key] = data.visitFrequency[key];
        }
    });

    // Calculate no-show rate percentage
    const totalAppointments = data.totalAppointments || 0;
    const noShows = data.noShowCancellation?.noShows || 0;
    const noShowRate = totalAppointments > 0 ? (noShows / totalAppointments * 100) : 0;

    return {
        totalAppointments: data.totalAppointments || 0,
        totalPatients: data.totalPatients || 0,
        noShowRate: noShowRate,
        avgSatisfaction: data.avgSatisfaction || 0,
        
        appointmentVolume: appointmentVolume,
        ageDistribution: ageDistribution,
        avgAge: data.avgAge || 0,
        commonAgeGroup: data.commonAgeGroup || 'N/A',
        
        visitFrequency: visitFrequency,
        avgVisits: data.avgVisits || 0,
        returnPatients: data.returnPatients || 0,
        
        noShowCancellation: {
            noShows: data.noShowCancellation?.noShows || 0,
            cancellations: data.noShowCancellation?.cancellations || 0,
            completed: data.noShowCancellation?.completed || 0
        },
        
        satisfactionRatings: data.satisfactionRatings || {
            '5': 0,
            '4': 0,
            '3': 0,
            '2': 0,
            '1': 0
        },
        totalFeedback: data.totalFeedback || 0,
        positiveFeedback: data.positiveFeedback || 0
    };
}

// Return empty data structure if fetch fails
function getEmptyData() {
    return {
        totalAppointments: 0,
        totalPatients: 0,
        noShowRate: 0,
        avgSatisfaction: 0,
        appointmentVolume: [],
        ageDistribution: {
            'Under 18': 0,
            '18-25': 0,
            '26-35': 0,
            '36-45': 0,
            '46-55': 0,
            '56-65': 0,
            '65+': 0
        },
        avgAge: 0,
        commonAgeGroup: 'N/A',
        visitFrequency: {},
        avgVisits: 0,
        returnPatients: 0,
        noShowCancellation: {
            noShows: 0,
            cancellations: 0,
            completed: 0
        },
        satisfactionRatings: {
            '5': 0,
            '4': 0,
            '3': 0,
            '2': 0,
            '1': 0
        },
        totalFeedback: 0,
        positiveFeedback: 0
    };
}

// Update overview cards
function updateOverviewCards() {
    const data = analyticsData;
    
    document.getElementById('totalAppointments').textContent = data.totalAppointments.toLocaleString();
    document.getElementById('totalPatients').textContent = data.totalPatients.toLocaleString();
    document.getElementById('noShowRate').textContent = data.noShowRate.toFixed(1) + '%';
}

// Render all charts
function renderCharts() {
    renderAppointmentVolumeChart();
    renderAgeDistributionChart();
    renderVisitFrequencyChart();
    renderNoShowCancellationChart();
    updateDemographicsStats();
    updateFrequencyStats();
    updateCancellationStats();
}

// Render Appointment Volume Trend Chart
function renderAppointmentVolumeChart() {
    const ctx = document.getElementById('appointmentVolumeChart');
    if (!ctx) return;
    
    // Destroy existing chart if it exists
    if (charts.appointmentVolume) {
        charts.appointmentVolume.destroy();
    }
    
    const data = analyticsData.appointmentVolume;
    
    charts.appointmentVolume = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.day + ' ' + d.date),
            datasets: [{
                label: 'Appointments',
                data: data.map(d => d.count),
                borderColor: '#4ECDC4',
                backgroundColor: 'rgba(78, 205, 196, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointHoverRadius: 6,
                pointBackgroundColor: '#4ECDC4',
                pointBorderColor: '#FFFFFF',
                pointBorderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: {
                        size: 14,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    },
                    ticks: {
                        stepSize: 5
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
}

// Render Age Distribution Chart
function renderAgeDistributionChart() {
    const ctx = document.getElementById('ageDistributionChart');
    if (!ctx) return;
    
    if (charts.ageDistribution) {
        charts.ageDistribution.destroy();
    }
    
    const data = analyticsData.ageDistribution;
    const labels = Object.keys(data).filter(key => data[key] > 0); // Only show groups with data
    const values = labels.map(key => data[key]);
    
    charts.ageDistribution = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: [
                    '#4ECDC4',
                    '#44A08D',
                    '#3BA89F',
                    '#10B981',
                    '#F59E0B',
                    '#DC2626',
                    '#8B5CF6'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12,
                            weight: '600'
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12
                }
            }
        }
    });
}

// Render Visit Frequency Chart
function renderVisitFrequencyChart() {
    const ctx = document.getElementById('visitFrequencyChart');
    if (!ctx) return;
    
    if (charts.visitFrequency) {
        charts.visitFrequency.destroy();
    }
    
    const data = analyticsData.visitFrequency;
    // Sort keys numerically, but keep '5+' at the end
    const sortedKeys = Object.keys(data).sort((a, b) => {
        if (a === '5+') return 1;
        if (b === '5+') return -1;
        return parseInt(a) - parseInt(b);
    });
    
    charts.visitFrequency = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: sortedKeys.map(v => v === '5+' ? '5+' : v + ' visit' + (parseInt(v) > 1 ? 's' : '')),
            datasets: [{
                label: 'Patients',
                data: sortedKeys.map(key => data[key]),
                backgroundColor: '#10B981',
                borderRadius: 8,
                borderSkipped: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
}

// Render No-Show & Cancellation Chart
function renderNoShowCancellationChart() {
    const ctx = document.getElementById('noShowCancellationChart');
    if (!ctx) return;
    
    if (charts.noShowCancellation) {
        charts.noShowCancellation.destroy();
    }
    
    const data = analyticsData.noShowCancellation || { noShows: 0, cancellations: 0, completed: 0 };
    const total = data.noShows + data.cancellations + data.completed;
    
    charts.noShowCancellation = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['No-Shows', 'Cancellations', 'Completed'],
            datasets: [{
                data: [data.noShows, data.cancellations, data.completed],
                backgroundColor: [
                    '#DC2626',
                    '#F59E0B',
                    '#10B981'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12,
                            weight: '600'
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    callbacks: {
                        label: function(context) {
                            const value = context.parsed;
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return context.label + ': ' + value + ' (' + percentage + '%)';
                        }
                    }
                }
            }
        }
    });
}


// Update demographics statistics
function updateDemographicsStats() {
    const data = analyticsData;
    
    document.getElementById('avgAge').textContent = data.avgAge.toFixed(1) + ' years';
    document.getElementById('commonAgeGroup').textContent = data.commonAgeGroup + ' years';
}

// Update frequency statistics
function updateFrequencyStats() {
    const data = analyticsData;
    
    document.getElementById('avgVisits').textContent = data.avgVisits.toFixed(1);
    document.getElementById('returnPatients').textContent = data.returnPatients.toLocaleString();
}

// Update cancellation statistics
function updateCancellationStats() {
    const data = analyticsData.noShowCancellation;
    const total = data.noShows + data.cancellations + data.completed;
    
    document.getElementById('totalNoShows').textContent = data.noShows.toLocaleString();
    document.getElementById('noShowPercentage').textContent = ((data.noShows / total) * 100).toFixed(1) + '%';
    
    document.getElementById('totalCancellations').textContent = data.cancellations.toLocaleString();
    document.getElementById('cancellationPercentage').textContent = ((data.cancellations / total) * 100).toFixed(1) + '%';
    
    document.getElementById('totalCompleted').textContent = data.completed.toLocaleString();
    document.getElementById('completedPercentage').textContent = ((data.completed / total) * 100).toFixed(1) + '%';
}

