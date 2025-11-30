// Analytics & Reporting JavaScript

let charts = {};
let analyticsData = {};

// Initialize analytics when page loads
document.addEventListener('DOMContentLoaded', function() {
    loadAnalyticsData();
    
    // Add smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href && href !== '#') {
                e.preventDefault();
                const target = document.querySelector(href);
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });
    
    // Add event listeners for all time range selectors
    const timeRangeSelectors = {
        'volumeTimeRange': 'appointmentVolume',
        'demographicsTimeRange': 'demographics',
        'visitFrequencyTimeRange': 'visitFrequency',
        'noShowCancellationTimeRange': 'noShowCancellation',
        'emergencyTimeRange': 'emergency',
        'reasonsForVisitTimeRange': 'reasonsForVisit'
    };
    
    Object.keys(timeRangeSelectors).forEach(selectId => {
        const select = document.getElementById(selectId);
        if (select) {
            select.addEventListener('change', function() {
                const chartType = timeRangeSelectors[selectId];
                loadChartData(chartType, this.value);
            });
        }
    });
});

// Load analytics data from server (initial load with default time ranges)
async function loadAnalyticsData() {
    try {
        const defaultTimeRange = 30;
        
        // Load all data with default time range
        analyticsData = await fetchAnalyticsData(defaultTimeRange);
        
        updateOverviewCards();
        renderCharts();
    } catch (error) {
        console.error('Error loading analytics data:', error);
    }
}

// Load data for a specific chart based on its time range
async function loadChartData(chartType, timeRange) {
    try {
        const chartData = await fetchChartData(chartType, timeRange);
        
        // Update the specific chart data in analyticsData
        switch(chartType) {
            case 'appointmentVolume':
                analyticsData.appointmentVolume = chartData.appointmentVolume || [];
                renderAppointmentVolumeChart();
                updateVolumeStats();
                break;
            case 'demographics':
                analyticsData.ageDistribution = chartData.ageDistribution || analyticsData.ageDistribution;
                analyticsData.avgAge = chartData.avgAge || analyticsData.avgAge;
                analyticsData.commonAgeGroup = chartData.commonAgeGroup || analyticsData.commonAgeGroup;
                renderAgeDistributionChart();
                updateDemographicsStats();
                break;
            case 'visitFrequency':
                analyticsData.visitFrequency = chartData.visitFrequency || {};
                analyticsData.avgVisits = chartData.avgVisits || analyticsData.avgVisits;
                analyticsData.returnPatients = chartData.returnPatients || analyticsData.returnPatients;
                renderVisitFrequencyChart();
                updateFrequencyStats();
                break;
            case 'noShowCancellation':
                analyticsData.noShowCancellation = chartData.noShowCancellation || analyticsData.noShowCancellation;
                renderNoShowCancellationChart();
                updateCancellationStats();
                break;
            case 'emergency':
                analyticsData.emergencyStatistics = chartData.emergencyStatistics || analyticsData.emergencyStatistics;
                renderEmergencyTrendChart();
                updateEmergencyStats();
                break;
            case 'reasonsForVisit':
                analyticsData.reasonsForVisit = chartData.reasonsForVisit || analyticsData.reasonsForVisit;
                renderReasonsForVisitChart();
                updateReasonsStats();
                break;
        }
    } catch (error) {
        console.error(`Error loading ${chartType} chart data:`, error);
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

// Fetch data for a specific chart type
async function fetchChartData(chartType, timeRange) {
    try {
        const response = await fetch(`/Clinicstaff/GetAnalyticsData?timeRange=${timeRange}&chartType=${chartType}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            // Process the specific chart data
            const processedData = processAnalyticsData(result.data);
            
            // Return only the relevant data for this chart type
            switch(chartType) {
                case 'appointmentVolume':
                    return { appointmentVolume: processedData.appointmentVolume };
                case 'demographics':
                    return {
                        ageDistribution: processedData.ageDistribution,
                        avgAge: processedData.avgAge,
                        commonAgeGroup: processedData.commonAgeGroup
                    };
                case 'visitFrequency':
                    return {
                        visitFrequency: processedData.visitFrequency,
                        avgVisits: processedData.avgVisits,
                        returnPatients: processedData.returnPatients
                    };
                case 'noShowCancellation':
                    return { noShowCancellation: processedData.noShowCancellation };
                case 'emergency':
                    return { emergencyStatistics: processedData.emergencyStatistics };
                case 'reasonsForVisit':
                    return { reasonsForVisit: processedData.reasonsForVisit };
                default:
                    return {};
            }
        } else {
            console.error(`Failed to fetch ${chartType} chart data:`, result.error);
            return {};
        }
    } catch (error) {
        console.error(`Error fetching ${chartType} chart data:`, error);
        return {};
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
        
        emergencyStatistics: {
            emergenciesToday: data.emergencyStatistics?.emergenciesToday || 0,
            emergenciesThisWeek: data.emergencyStatistics?.emergenciesThisWeek || 0,
            emergenciesThisMonth: data.emergencyStatistics?.emergenciesThisMonth || 0,
            emergenciesThisYear: data.emergencyStatistics?.emergenciesThisYear || 0,
            emergencyVolume: (data.emergencyStatistics?.emergencyVolume || []).map(item => ({
                date: item.date,
                day: item.day,
                count: item.count
            }))
        },
        
        reasonsForVisit: {
            reasonsDistribution: data.reasonsForVisit?.reasonsDistribution || {},
            mostCommonReason: data.reasonsForVisit?.mostCommonReason || 'N/A',
            totalUniqueReasons: data.reasonsForVisit?.totalUniqueReasons || 0
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
        emergencyStatistics: {
            emergenciesToday: 0,
            emergenciesThisWeek: 0,
            emergenciesThisMonth: 0,
            emergenciesThisYear: 0,
            emergencyVolume: []
        },
        reasonsForVisit: {
            reasonsDistribution: {},
            mostCommonReason: 'N/A',
            totalUniqueReasons: 0
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
    const totalEmergencies = data.emergencyStatistics?.emergenciesThisYear || 0;
    document.getElementById('totalEmergencies').textContent = totalEmergencies.toLocaleString();
    document.getElementById('avgVisitsPerPatient').textContent = (data.avgVisits || 0).toFixed(1);
    document.getElementById('totalUniqueReasonsCard').textContent = (data.reasonsForVisit?.totalUniqueReasons || 0).toLocaleString();
}

// Render all charts
function renderCharts() {
    renderAppointmentVolumeChart();
    renderAgeDistributionChart();
    renderVisitFrequencyChart();
    renderNoShowCancellationChart();
    renderEmergencyTrendChart();
    renderReasonsForVisitChart();
    updateDemographicsStats();
    updateFrequencyStats();
    updateCancellationStats();
    updateVolumeStats();
    updateEmergencyStats();
    updateReasonsStats();
}

// Render Appointment Volume Trend Chart
function renderAppointmentVolumeChart() {
    const ctx = document.getElementById('appointmentVolumeChart');
    if (!ctx) return;
    
    // Destroy existing chart if it exists
    if (charts.appointmentVolume) {
        charts.appointmentVolume.destroy();
    }
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('volumeTimeRange')?.value || 30;
    const data = analyticsData.appointmentVolume || [];
    
    // Calculate total for percentage
    const totalAppointments = data.reduce((sum, d) => sum + d.count, 0);
    
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
                pointRadius: 7,
                pointHoverRadius: 9,
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
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        title: function(context) {
                            const index = context[0].dataIndex;
                            return data[index] ? data[index].date : '';
                        },
                        label: function(context) {
                            const index = context.dataIndex;
                            const pointData = data[index];
                            if (!pointData) return '';
                            
                            const count = pointData.count;
                            const percentage = totalAppointments > 0 
                                ? ((count / totalAppointments) * 100).toFixed(1) 
                                : '0.0';
                            
                            return [
                                `Day: ${pointData.day}`,
                                `Appointments: ${count}`,
                                `Percentage: ${percentage}%`
                            ];
                        }
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
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('demographicsTimeRange')?.value || 30;
    const data = analyticsData.ageDistribution || {};
    const labels = Object.keys(data).filter(key => data[key] > 0); // Only show groups with data
    const values = labels.map(key => data[key]);
    const totalPatients = values.reduce((sum, val) => sum + val, 0);
    
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
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const percentage = totalPatients > 0 
                                ? ((value / totalPatients) * 100).toFixed(1) 
                                : '0.0';
                            return [
                                `Age Group: ${label}`,
                                `Patients: ${value}`,
                                `Percentage: ${percentage}%`
                            ];
                        }
                    }
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
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('visitFrequencyTimeRange')?.value || 30;
    const data = analyticsData.visitFrequency || {};
    // Sort keys numerically, but keep '5+' at the end
    const sortedKeys = Object.keys(data).sort((a, b) => {
        if (a === '5+') return 1;
        if (b === '5+') return -1;
        return parseInt(a) - parseInt(b);
    });
    const totalPatients = Object.values(data).reduce((sum, val) => sum + val, 0);
    
    charts.visitFrequency = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: sortedKeys.map(v => v === '5+' ? '5+ visits' : v + ' visit' + (parseInt(v) > 1 ? 's' : '')),
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
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed.y || 0;
                            const percentage = totalPatients > 0 
                                ? ((value / totalPatients) * 100).toFixed(1) 
                                : '0.0';
                            return [
                                `Visit Frequency: ${label}`,
                                `Patients: ${value}`,
                                `Percentage: ${percentage}%`
                            ];
                        }
                    }
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
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('noShowCancellationTimeRange')?.value || 30;
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
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        title: function(context) {
                            return 'Appointment Status';
                        },
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
                            return [
                                `Status: ${label}`,
                                `Count: ${value}`,
                                `Percentage: ${percentage}%`,
                                `Total Appointments: ${total}`
                            ];
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
    
    // Calculate total patients (sum of all patients in visit frequency)
    const visitFrequency = data.visitFrequency || {};
    const totalPatientsVisited = Object.values(visitFrequency).reduce((sum, count) => sum + count, 0);
    
    // Calculate new patients (patients with exactly 1 visit)
    const newPatients = visitFrequency['1'] || 0;
    
    // Return patients (already calculated)
    const returnPatients = data.returnPatients || 0;
    
    document.getElementById('totalPatientsVisited').textContent = totalPatientsVisited.toLocaleString();
    document.getElementById('newPatients').textContent = newPatients.toLocaleString();
    document.getElementById('returnPatients').textContent = returnPatients.toLocaleString();
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

// Update appointment volume statistics
function updateVolumeStats() {
    const data = analyticsData.appointmentVolume || [];
    
    if (data.length === 0) {
        document.getElementById('totalAppointmentsVolume').textContent = '0';
        document.getElementById('avgAppointmentsPerDay').textContent = '0';
        document.getElementById('peakAppointmentDay').textContent = '-';
        return;
    }
    
    // Calculate total appointments
    const total = data.reduce((sum, item) => sum + item.count, 0);
    
    // Calculate average per day
    const avgPerDay = (total / data.length).toFixed(1);
    
    // Find peak day
    const peakDay = data.reduce((max, item) => item.count > max.count ? item : max, data[0]);
    const peakDayLabel = peakDay.day + ' (' + peakDay.count + ')';
    
    document.getElementById('totalAppointmentsVolume').textContent = total.toLocaleString();
    document.getElementById('avgAppointmentsPerDay').textContent = avgPerDay;
    document.getElementById('peakAppointmentDay').textContent = peakDayLabel;
}

// Update emergency statistics
function updateEmergencyStats() {
    const data = analyticsData.emergencyStatistics || {};
    const volumeData = data.emergencyVolume || [];
    
    // Update today's emergencies
    const emergenciesToday = data.emergenciesToday || 0;
    document.getElementById('emergenciesToday').textContent = emergenciesToday.toLocaleString();
    
    if (volumeData.length === 0) {
        document.getElementById('totalEmergenciesStats').textContent = '0';
        document.getElementById('avgEmergenciesPerDay').textContent = '0';
        document.getElementById('peakEmergencyDay').textContent = '-';
        return;
    }
    
    // Calculate total emergencies from volume data
    const total = volumeData.reduce((sum, item) => sum + item.count, 0);
    
    // Calculate average per day
    const avgPerDay = (total / volumeData.length).toFixed(1);
    
    // Find peak day
    const peakDay = volumeData.reduce((max, item) => item.count > max.count ? item : max, volumeData[0]);
    const peakDayLabel = peakDay.day + ' (' + peakDay.count + ')';
    
    document.getElementById('totalEmergenciesStats').textContent = total.toLocaleString();
    document.getElementById('avgEmergenciesPerDay').textContent = avgPerDay;
    document.getElementById('peakEmergencyDay').textContent = peakDayLabel;
}

// Render Emergency Trend Chart
function renderEmergencyTrendChart() {
    const ctx = document.getElementById('emergencyTrendChart');
    if (!ctx) return;
    
    if (charts.emergencyTrend) {
        charts.emergencyTrend.destroy();
    }
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('emergencyTimeRange')?.value || 30;
    const data = analyticsData.emergencyStatistics?.emergencyVolume || [];
    
    // Calculate total for percentage
    const totalEmergencies = data.reduce((sum, d) => sum + d.count, 0);
    
    charts.emergencyTrend = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.day + ' ' + d.date),
            datasets: [{
                label: 'Emergencies',
                data: data.map(d => d.count),
                borderColor: '#DC2626',
                backgroundColor: 'rgba(220, 38, 38, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointRadius: 7,
                pointHoverRadius: 9,
                pointBackgroundColor: '#DC2626',
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
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        title: function(context) {
                            const index = context[0].dataIndex;
                            return data[index] ? data[index].date : '';
                        },
                        label: function(context) {
                            const index = context.dataIndex;
                            const pointData = data[index];
                            if (!pointData) return '';
                            
                            const count = pointData.count;
                            const percentage = totalEmergencies > 0 
                                ? ((count / totalEmergencies) * 100).toFixed(1) 
                                : '0.0';
                            
                            return [
                                `Day: ${pointData.day}`,
                                `Emergencies: ${count}`,
                                `Percentage: ${percentage}%`
                            ];
                        }
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
                        stepSize: 1
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

// Render Reasons for Visit Chart
function renderReasonsForVisitChart() {
    const ctx = document.getElementById('reasonsForVisitChart');
    if (!ctx) return;
    
    if (charts.reasonsForVisit) {
        charts.reasonsForVisit.destroy();
    }
    
    // Get time range for this specific chart
    const timeRange = document.getElementById('reasonsForVisitTimeRange')?.value || 30;
    const data = analyticsData.reasonsForVisit?.reasonsDistribution || {};
    const reasons = Object.keys(data);
    const counts = reasons.map(reason => data[reason]);
    
    if (reasons.length === 0) {
        return;
    }
    
    // Generate colors for the chart
    const colors = [
        '#4ECDC4', '#44A08D', '#3BA89F', '#10B981', '#F59E0B',
        '#DC2626', '#8B5CF6', '#EC4899', '#14B8A6', '#6366F1'
    ];
    
    charts.reasonsForVisit = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: reasons,
            datasets: [{
                data: counts,
                backgroundColor: reasons.map((_, i) => colors[i % colors.length]),
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'right',
                    labels: {
                        padding: 15,
                        font: {
                            size: 11,
                            weight: '600'
                        },
                        boxWidth: 12,
                        boxHeight: 12
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.9)',
                    padding: 16,
                    titleFont: {
                        size: 15,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        title: function(context) {
                            return 'Visit Reason';
                        },
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = counts.reduce((a, b) => a + b, 0);
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0';
                            return [
                                `Reason: ${label}`,
                                `Visits: ${value}`,
                                `Percentage: ${percentage}%`,
                                `Total Visits: ${total}`
                            ];
                        }
                    }
                }
            }
        }
    });
}

// Update reasons statistics
function updateReasonsStats() {
    const data = analyticsData.reasonsForVisit;
    
    document.getElementById('mostCommonReason').textContent = data.mostCommonReason || 'N/A';
    document.getElementById('totalUniqueReasons').textContent = (data.totalUniqueReasons || 0).toLocaleString();
}

