// Filter functionality
document.addEventListener('DOMContentLoaded', function () {
    const statusFilter = document.getElementById('statusFilter');
    const dateFilter = document.getElementById('dateFilter');
    const searchPatient = document.getElementById('searchPatient');

    if (statusFilter) {
        statusFilter.addEventListener('change', filterTable);
    }
    if (dateFilter) {
        dateFilter.addEventListener('change', filterTable);
    }
    if (searchPatient) {
        searchPatient.addEventListener('input', filterTable);
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

        const statusMatch = !statusFilter || status.includes(statusFilter);
        const dateMatch = !dateFilter || date === dateFilter;
        const patientMatch = !searchPatient || patient.includes(searchPatient);

        if (statusMatch && dateMatch && patientMatch) {
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
                <td colspan="8" class="text-center">
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
    
    // Update table count
    const tableCountEl = document.getElementById('tableCount');
    if (tableCountEl) {
        tableCountEl.textContent = `Showing ${appointments.length} appointments`;
    }
    
    // Add new rows
    appointments.forEach(appointment => {
        const statusClass = appointment.appointmentStatus.toLowerCase().replace(' ', '-');
        const statusIcon = getStatusIcon(appointment.appointmentStatus);
        
        const rowHtml = `
            <tr data-status="${appointment.appointmentStatus}" data-date="${appointment.scheduleDate}" data-patient="${appointment.patientName.toLowerCase()}">
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
                    <span class="reason-badge">${escapeHtml(appointment.reasonForVisit)}</span>
                </td>
                <td>
                    <div class="symptoms-text" title="${escapeHtml(appointment.symptoms)}">
                        ${escapeHtml(appointment.symptoms)}
                    </div>
                </td>
                <td>
                    <div class="date-booked">
                        ${appointment.dateBooked}
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
        buttons += `<button class="btn btn-warning btn-sm" onclick="updateStatus(${appointment.appointmentId}, 'Cancelled')" title="Cancel Appointment">
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
});