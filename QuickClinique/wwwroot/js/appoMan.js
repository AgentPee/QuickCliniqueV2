// Filter functionality
document.addEventListener('DOMContentLoaded', function () {
    const statusFilter = document.getElementById('statusFilter');
    const dateFilter = document.getElementById('dateFilter');
    const searchPatient = document.getElementById('searchPatient');
    const searchEmergency = document.getElementById('searchEmergency');

    if (statusFilter) {
        statusFilter.addEventListener('change', filterTable);
    }
    if (dateFilter) {
        dateFilter.addEventListener('change', filterTable);
    }
    if (searchPatient) {
        searchPatient.addEventListener('input', filterTable);
    }
    if (searchEmergency) {
        searchEmergency.addEventListener('input', filterEmergencyTable);
    }
});

function filterTable() {
    const statusFilter = document.getElementById('statusFilter').value.toLowerCase();
    const dateFilter = document.getElementById('dateFilter').value;
    const searchPatient = document.getElementById('searchPatient').value.toLowerCase();

    const rows = document.querySelectorAll('#appointmentsTable tbody tr');

    rows.forEach(row => {
        const status = row.dataset.status.toLowerCase();
        const date = row.dataset.date;
        const patient = row.dataset.patient;
        const studentId = row.dataset.studentId ? row.dataset.studentId.toString() : '';

        const statusMatch = !statusFilter || status.includes(statusFilter);
        const dateMatch = !dateFilter || date === dateFilter;
        const patientMatch = !searchPatient || patient.includes(searchPatient);
        const idMatch = !searchPatient || studentId.includes(searchPatient);

        if (statusMatch && dateMatch && (patientMatch || idMatch)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function clearFilters() {
    document.getElementById('statusFilter').value = '';
    document.getElementById('dateFilter').value = '';
    document.getElementById('searchPatient').value = '';
    filterTable();
}

// Filter emergency table
function filterEmergencyTable() {
    const searchInput = document.getElementById('searchEmergency');
    if (!searchInput) return;
    
    const searchTerm = searchInput.value.toLowerCase().trim();
    const rows = document.querySelectorAll('#emergenciesTable tbody tr');

    let visibleCount = 0;
    rows.forEach(row => {
        const studentName = row.dataset.studentName || '';
        const studentId = row.dataset.studentId || '';

        if (!searchTerm) {
            // Show all rows if search is empty
            row.style.display = '';
            visibleCount++;
        } else {
            // Filter by name or ID
            const nameMatch = studentName.includes(searchTerm);
            const idMatch = studentId.includes(searchTerm);

            if (nameMatch || idMatch) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        }
    });

    // Update count
    const countEl = document.getElementById('emergencyTableCount');
    if (countEl) {
        const total = rows.length;
        if (searchTerm) {
            countEl.textContent = `Showing ${visibleCount} of ${total} emergency${total !== 1 ? 's' : ''}`;
        } else {
            countEl.textContent = `Showing ${total} emergency${total !== 1 ? 's' : ''}`;
        }
    }
}

// Refresh appointments data in background without page reload
async function refreshAppointments() {
    try {
        const response = await fetch('/Appointments/GetManageData');
        const result = await response.json();
        
        if (result.success && result.data) {
            // Update statistics
            updateManageStats(result.data.stats);
            
            // Update appointments table
            updateAppointmentsTable(result.data.appointments);
            
            // Update emergencies table
            if (result.data.emergencies) {
                updateEmergenciesTable(result.data.emergencies);
            }
        }
    } catch (error) {
        console.error('Error refreshing appointments:', error);
    }
}

// Update management statistics
function updateManageStats(stats) {
    const totalEl = document.getElementById('totalAppointments');
    const pendingEl = document.getElementById('pendingAppointments');
    const confirmedEl = document.getElementById('confirmedAppointments');
    const todayEl = document.getElementById('todayAppointments');
    
    if (totalEl) totalEl.textContent = stats.totalAppointments;
    if (pendingEl) pendingEl.textContent = stats.pendingAppointments;
    if (confirmedEl) confirmedEl.textContent = stats.confirmedAppointments;
    if (todayEl) todayEl.textContent = stats.todayAppointments;
}

// Update appointments table
function updateAppointmentsTable(appointments) {
    const tbody = document.querySelector('#appointmentsTable tbody');
    if (!tbody) return;
    
    // Clear existing rows (except the "no appointments" row if it exists)
    const existingRows = tbody.querySelectorAll('tr');
    existingRows.forEach(row => row.remove());
    
    if (appointments.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9" class="text-center">
                    <div class="no-appointments">
                        <i class="fas fa-calendar-times"></i>
                        <h5>No Appointments Found</h5>
                        <p>There are no appointments to display at this time.</p>
                    </div>
                </td>
            </tr>
        `;
        return;
    }
    
    // Sort appointments by schedule date descending, then start time descending, then queue number descending
    const sortedAppointments = appointments.sort((a, b) => {
        // First, sort by schedule date (descending - latest first)
        const dateA = a.scheduleDate ? new Date(a.scheduleDate) : new Date(0);
        const dateB = b.scheduleDate ? new Date(b.scheduleDate) : new Date(0);
        if (dateB.getTime() !== dateA.getTime()) {
            return dateB.getTime() - dateA.getTime();
        }
        // If dates are equal, sort by start time (descending - latest first)
        const timeA = a.startTime || '';
        const timeB = b.startTime || '';
        if (timeB !== timeA) {
            return timeB.localeCompare(timeA);
        }
        // If times are equal, sort by queue number (descending - highest first)
        const queueA = a.queueNumber || 0;
        const queueB = b.queueNumber || 0;
        if (queueB !== queueA) {
            return queueB - queueA;
        }
        // If queue numbers are equal, sort by date booked (descending - latest first)
        const bookedA = a.dateBooked ? new Date(a.dateBooked) : new Date(0);
        const bookedB = b.dateBooked ? new Date(b.dateBooked) : new Date(0);
        return bookedB.getTime() - bookedA.getTime();
    });
    
    // Update table count
    const tableCountEl = document.getElementById('tableCount');
    if (tableCountEl) {
        tableCountEl.textContent = `Showing ${sortedAppointments.length} appointments`;
    }
    
    // Add new rows
    sortedAppointments.forEach(appointment => {
        const statusClass = appointment.appointmentStatus.toLowerCase().replace(' ', '-');
        const statusIcon = getStatusIcon(appointment.appointmentStatus);
        
        const rowHtml = `
            <tr data-status="${appointment.appointmentStatus}" data-date="${appointment.scheduleDateFilter || appointment.scheduleDate}" data-patient="${appointment.patientName.toLowerCase()}" data-student-id="${appointment.studentIdNumber || ''}">
                <td>
                    <span class="id-badge"><i class="fas fa-id-card"></i> ${appointment.studentIdNumber || 'N/A'}</span>
                </td>
                <td>
                    <div class="patient-info">
                        <i class="fas fa-user-circle patient-icon"></i>
                        <span>${escapeHtml(appointment.patientName)}</span>
                    </div>
                </td>
                <td>
                    <div class="date-info">
                        <div class="date-primary">${appointment.scheduleDate}</div>
                        <div class="appointment-time">
                            <i class="fas fa-clock"></i> ${appointment.startTime} - ${appointment.endTime}
                        </div>
                    </div>
                </td>
                <td>
                    <span class="status-badge status-${statusClass}">
                        ${statusIcon}
                        ${escapeHtml(appointment.appointmentStatus)}
                    </span>
                </td>
                <td>
                    <div class="queue-info-cell">
                        <span class="queue-badge">#${appointment.queueNumber}</span>
                        <div class="queue-status">${escapeHtml(appointment.queueStatus)}</div>
                    </div>
                </td>
                <td>
                    <span class="reason-badge">
                        <i class="fas ${getReasonIcon(appointment.reasonForVisit)}"></i> ${escapeHtml(appointment.reasonForVisit)}
                    </span>
                </td>
                <td>
                    <div class="symptoms-text" title="${escapeHtml(appointment.symptoms)}">
                        <i class="fas fa-notes-medical"></i> ${escapeHtml(appointment.symptoms)}
                    </div>
                </td>
                <td>
                    <div class="date-booked">
                        <i class="fas fa-calendar"></i> ${appointment.dateBooked}
                    </div>
                </td>
                <td>
                    <div class="action-buttons">
                        ${getActionButtons(appointment)}
                    </div>
                </td>
            </tr>
        `;
        tbody.insertAdjacentHTML('beforeend', rowHtml);
    });
    
    // Re-apply filters if any are active
    filterTable();
}

// Get status icon
function getStatusIcon(status) {
    switch (status) {
        case "Pending": return '<i class="fas fa-clock"></i>';
        case "Confirmed": return '<i class="fas fa-check"></i>';
        case "In Progress": return '<i class="fas fa-spinner"></i>';
        case "Completed": return '<i class="fas fa-check-circle"></i>';
        case "Cancelled": return '<i class="fas fa-times-circle"></i>';
        default: return '';
    }
}

// Get reason icon based on reason for visit
function getReasonIcon(reason) {
    if (!reason) return 'fa-stethoscope'; // default icon
    
    const reasonLower = reason.toLowerCase();
    
    if (reasonLower.includes('medical-checkup') || reasonLower.includes('general medical')) {
        return 'fa-heartbeat';
    } else if (reasonLower.includes('dental-checkup') || reasonLower.includes('dental') && !reasonLower.includes('cleaning') && !reasonLower.includes('pasta') && !reasonLower.includes('extraction')) {
        return 'fa-tooth';
    } else if (reasonLower.includes('dental-cleaning') || reasonLower.includes('prophylaxis')) {
        return 'fa-spray-can';
    } else if (reasonLower.includes('dental-pasta') || reasonLower.includes('fillings')) {
        return 'fa-fill-drip';
    } else if (reasonLower.includes('dental-extraction') || reasonLower.includes('extraction')) {
        return 'fa-tooth';
    } else if (reasonLower.includes('consultation')) {
        return 'fa-user-md';
    } else if (reasonLower.includes('bp-monitoring') || reasonLower.includes('blood pressure')) {
        return 'fa-heartbeat';
    }
    
    return 'fa-stethoscope'; // default icon
}

// Get action buttons for appointment
function getActionButtons(appointment) {
    let buttons = '';
    
    if (appointment.appointmentStatus === "Pending") {
        buttons += `<button class="btn btn-success btn-sm" onclick="updateStatus(${appointment.appointmentId}, 'Confirmed')" title="Confirm Appointment">
            <i class="fas fa-check"></i>
        </button>`;
    }
    
    if (appointment.appointmentStatus === "Confirmed") {
        buttons += `<button class="btn btn-primary btn-sm" onclick="showStartAppointmentModal(${appointment.appointmentId})" title="Start Appointment">
            <i class="fas fa-play"></i>
        </button>`;
    }
    
    if (appointment.appointmentStatus === "In Progress") {
        buttons += `<button class="btn btn-success btn-sm" onclick="completeAppointment(${appointment.appointmentId}, ${appointment.patientId})" title="Complete Appointment">
            <i class="fas fa-check-circle"></i>
        </button>`;
    }
    
    if (appointment.appointmentStatus !== "Completed" && appointment.appointmentStatus !== "Cancelled") {
        buttons += `<button class="btn btn-warning btn-sm" onclick="showCancelAppointmentModal(${appointment.appointmentId})" title="Cancel Appointment">
            <i class="fas fa-times"></i>
        </button>`;
    }
    
    buttons += `<a href="/Appointments/Details/${appointment.appointmentId}" class="btn btn-info btn-sm" title="View Details">
        <i class="fas fa-eye"></i>
    </a>`;
    
    return buttons;
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function updateStatus(appointmentId, status) {
    // Don't allow direct cancellation - use the cancel modal instead
    if (status === 'Cancelled') {
        showCancelAppointmentModal(appointmentId);
        return;
    }

    if (!confirm(`Are you sure you want to change the appointment status to "${status}"?`)) {
        return;
    }

    try {
        console.log('Updating appointment:', appointmentId, 'to status:', status);

        const response = await fetch('/Appointments/UpdateStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                appointmentId: appointmentId,
                status: status
            })
        });

        console.log('Response status:', response.status);

        if (!response.ok) {
            console.error('HTTP error:', response.status, response.statusText);
            alert('Server error: ' + response.status + ' ' + response.statusText);
            return;
        }

        const result = await response.json();
        console.log('Response data:', result);

        if (result.success) {
            alert(result.message);
            // Refresh appointments data in background
            setTimeout(() => refreshAppointments(), 500);
        } else {
            alert('Error: ' + result.error);
        }
    } catch (error) {
        console.error('Error updating status:', error);
        alert('An error occurred while updating the appointment status.\n\nError: ' + error.message);
    }
}

// Show the cancel appointment modal
function showCancelAppointmentModal(appointmentId) {
    // Set the appointment ID
    document.getElementById('cancelAppointmentId').value = appointmentId;
    
    // Reset form - clear any previously selected reason
    const form = document.getElementById('cancelAppointmentForm');
    form.reset();
    document.getElementById('cancelAppointmentId').value = appointmentId;

    // Show the modal using Bootstrap 5
    const modalElement = document.getElementById('cancelAppointmentModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

// Submit cancel appointment with reason
async function submitCancelAppointment() {
    const appointmentId = document.getElementById('cancelAppointmentId').value;
    
    if (!appointmentId) {
        alert('Error: Missing appointment ID. Please try again.');
        return;
    }

    // Get selected cancellation reason
    const reasonRadios = document.querySelectorAll('input[name="cancellationReason"]:checked');
    if (reasonRadios.length === 0) {
        alert('Please select a cancellation reason.');
        return;
    }

    const reason = reasonRadios[0].value;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const response = await fetch('/Appointments/CancelAppointment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: new URLSearchParams({
                appointmentId: appointmentId,
                reason: reason,
                __RequestVerificationToken: token
            })
        });

        const result = await response.json();

        if (result.success) {
            // Hide modal
            const modalElement = document.getElementById('cancelAppointmentModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            
            alert(result.message || 'Appointment cancelled successfully!');
            // Refresh appointments data in background
            setTimeout(() => refreshAppointments(), 500);
        } else {
            alert('Error: ' + (result.error || 'Failed to cancel appointment'));
        }
    } catch (error) {
        console.error('Error cancelling appointment:', error);
        alert('An error occurred while cancelling the appointment. Please try again.');
    }
}

// Complete appointment with medical record
function completeAppointment(appointmentId, patientId) {
    // Set the appointment and patient IDs in the modal
    document.getElementById('completeAppointmentId').value = appointmentId;
    document.getElementById('completePatientId').value = patientId;

    // Reset form
    document.getElementById('completeAppointmentForm').reset();
    document.getElementById('completeAppointmentId').value = appointmentId;
    document.getElementById('completePatientId').value = patientId;

    // Show the modal using Bootstrap 5
    const modalElement = document.getElementById('completeAppointmentModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

async function submitCompleteAppointment() {
    const form = document.getElementById('completeAppointmentForm');

    // Validate required fields
    const diagnosis = document.getElementById('diagnosis').value.trim();
    if (!diagnosis) {
        alert('Please enter a diagnosis before completing the appointment.');
        document.getElementById('diagnosis').focus();
        return;
    }

    const medications = document.getElementById('medications').value.trim();
    if (!medications) {
        alert('Please enter medications/treatment information before completing the appointment.');
        document.getElementById('medications').focus();
        return;
    }

    const appointmentId = document.getElementById('completeAppointmentId').value;
    const patientId = document.getElementById('completePatientId').value;

    console.log('Raw values from form:');
    console.log('  appointmentId:', appointmentId);
    console.log('  patientId:', patientId);

    // Validate IDs are present
    if (!appointmentId || !patientId) {
        alert('Error: Missing appointment or patient ID. Please try again.');
        console.error('Missing IDs - appointmentId:', appointmentId, 'patientId:', patientId);
        return;
    }

    // Prepare form data (only diagnosis and medications)
    const formData = {
        appointmentId: parseInt(appointmentId),
        patientId: parseInt(patientId),
        diagnosis: diagnosis,
        medications: medications
    };

    console.log('Submitting completion data:', formData);
    console.log('Parsed appointmentId:', formData.appointmentId);
    console.log('Parsed patientId:', formData.patientId);

    try {
        const response = await fetch('/Appointments/CompleteAppointment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        console.log('Response status:', response.status);

        if (!response.ok) {
            console.error('HTTP error:', response.status, response.statusText);
            alert('Server error: ' + response.status + ' ' + response.statusText);
            return;
        }

        const result = await response.json();
        console.log('Response data:', result);

        if (result.success) {
            // Hide modal using Bootstrap 5
            const modalElement = document.getElementById('completeAppointmentModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            alert('Appointment completed successfully! Medical record has been created.');
            // Refresh appointments data in background
            setTimeout(() => refreshAppointments(), 500);
        } else {
            console.error('Error from server:', result.error, result.details);
            let errorMsg = 'Error: ' + (result.error || 'Failed to complete appointment');
            if (result.details) {
                errorMsg += '\nDetails: ' + JSON.stringify(result.details);
            }
            alert(errorMsg);
        }
    } catch (error) {
        console.error('Error completing appointment:', error);
        alert('An error occurred while completing the appointment. Please try again.\n\nError: ' + error.message);
    }
}

// Show the start appointment modal (triage modal)
function showStartAppointmentModal(appointmentId) {
    // Set the appointment ID
    document.getElementById('startAppointmentId').value = appointmentId;
    
    // Reset form
    document.getElementById('nextPatientForm').reset();
    document.getElementById('startAppointmentId').value = appointmentId;

    // Show the modal using Bootstrap 5
    const modalElement = document.getElementById('nextPatientModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

// Submit start appointment with triage data
async function submitStartAppointment() {
    try {
        const appointmentId = document.getElementById('startAppointmentId').value;
        
        if (!appointmentId) {
            alert('Error: Missing appointment ID. Please try again.');
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Collect triage data from form
        const age = document.getElementById('triageAge').value ? parseInt(document.getElementById('triageAge').value) : null;
        const gender = document.getElementById('triageGender').value || null;
        const bmi = document.getElementById('triageBMI').value ? parseFloat(document.getElementById('triageBMI').value) : null;
        const allergies = document.getElementById('triageAllergies').value.trim() || null;
        const triageNotes = document.getElementById('triageNotes').value.trim() || null;

        // Update appointment status to "In Progress" with triage data
        const response = await fetch('/Appointments/UpdateStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({
                appointmentId: parseInt(appointmentId),
                status: 'In Progress',
                age: age,
                gender: gender,
                bmi: bmi,
                allergies: allergies,
                triageNotes: triageNotes
            })
        });

        const result = await response.json();

        if (result.success) {
            // Hide modal
            const modalElement = document.getElementById('nextPatientModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            
            alert(result.message || 'Appointment started successfully!');
            // Refresh appointments data in background
            setTimeout(() => refreshAppointments(), 500);
        } else {
            alert('Error: ' + (result.error || 'Failed to start appointment'));
        }
    } catch (error) {
        console.error('Error starting appointment:', error);
        alert('An error occurred while starting the appointment.');
    }
}

// Reset form when modal is hidden
document.addEventListener('DOMContentLoaded', function () {
    const modalElement = document.getElementById('completeAppointmentModal');
    if (modalElement) {
        modalElement.addEventListener('hidden.bs.modal', function () {
            document.getElementById('completeAppointmentForm').reset();
        });
    }

    // Reset next patient modal when hidden
    const nextPatientModalElement = document.getElementById('nextPatientModal');
    if (nextPatientModalElement) {
        nextPatientModalElement.addEventListener('hidden.bs.modal', function () {
            document.getElementById('nextPatientForm').reset();
        });
    }

    // Reset cancel appointment modal when hidden
    const cancelModalElement = document.getElementById('cancelAppointmentModal');
    if (cancelModalElement) {
        cancelModalElement.addEventListener('hidden.bs.modal', function () {
            document.getElementById('cancelAppointmentForm').reset();
        });
    }

    // Load emergencies on page load
    loadEmergencies();
});

// Load emergencies table
async function loadEmergencies() {
    try {
        const response = await fetch('/Appointments/GetManageData');
        const result = await response.json();
        
        if (result.success && result.data && result.data.emergencies) {
            updateEmergenciesTable(result.data.emergencies);
        }
    } catch (error) {
        console.error('Error loading emergencies:', error);
    }
}

// Update emergencies table
function updateEmergenciesTable(emergencies) {
    const tbody = document.getElementById('emergenciesTableBody');
    const countEl = document.getElementById('emergencyTableCount');
    
    if (!tbody) return;
    
    // Clear existing rows
    tbody.innerHTML = '';
    
    if (emergencies.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center">
                    <div class="no-appointments">
                        <i class="fas fa-shield-alt"></i>
                        <h5>No Emergency Schedules</h5>
                        <p>There are no emergency requests at this time.</p>
                    </div>
                </td>
            </tr>
        `;
        if (countEl) countEl.textContent = 'No emergencies';
        return;
    }
    
    if (countEl) {
        countEl.textContent = `Showing ${emergencies.length} emergency${emergencies.length !== 1 ? 's' : ''}`;
    }
    
    // Sort emergencies by created date descending (most recent first)
    const sortedEmergencies = emergencies.sort((a, b) => {
        const dateA = a.createdAt ? new Date(a.createdAt) : new Date(0);
        const dateB = b.createdAt ? new Date(b.createdAt) : new Date(0);
        return dateB.getTime() - dateA.getTime();
    });
    
    // Add rows
    sortedEmergencies.forEach(emergency => {
        const statusClass = emergency.isResolved ? 'resolved' : 'active';
        const statusText = emergency.isResolved ? 'Resolved' : 'Active';
        const statusBadgeClass = emergency.isResolved ? 'status-completed' : 'status-pending';
        
        const createdAt = emergency.createdAt 
            ? new Date(emergency.createdAt).toLocaleString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            })
            : 'Unknown';
        
        const row = document.createElement('tr');
        row.setAttribute('data-status', statusClass);
        row.setAttribute('data-resolved', emergency.isResolved ? 'true' : 'false');
        row.setAttribute('data-student-name', (emergency.studentName || 'Unknown').toLowerCase());
        row.setAttribute('data-student-id', (emergency.studentIdNumber || '').toString());
        row.innerHTML = `
            <td>
                <div class="patient-info">
                    <i class="fas fa-user-circle patient-icon"></i>
                    <span>${escapeHtml(emergency.studentName || 'Unknown')}</span>
                </div>
            </td>
            <td>
                <span class="queue-badge">${emergency.studentIdNumber || 'N/A'}</span>
            </td>
            <td>
                <div class="symptoms-text" title="${escapeHtml(emergency.location || '')}">
                    <i class="fas fa-map-marker-alt"></i> ${escapeHtml(emergency.location || 'Not specified')}
                </div>
            </td>
            <td>
                <div class="symptoms-text" title="${escapeHtml(emergency.needs || '')}">
                    <i class="fas fa-tools"></i> ${escapeHtml(emergency.needs || 'Not specified')}
                </div>
            </td>
            <td>
                <div class="date-booked">
                    <i class="fas fa-clock"></i> ${createdAt}
                </div>
            </td>
            <td>
                <span class="status-badge ${statusBadgeClass}">
                    ${emergency.isResolved ? '<i class="fas fa-check-circle"></i>' : '<i class="fas fa-exclamation-triangle"></i>'}
                    ${statusText}
                </span>
            </td>
        `;
        tbody.appendChild(row);
    });
    
    // Apply any active search filter
    const searchEmergency = document.getElementById('searchEmergency');
    if (searchEmergency && searchEmergency.value) {
        filterEmergencyTable();
    }
}