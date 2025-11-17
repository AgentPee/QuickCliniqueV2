let autoRefreshInterval;

// Auto-refresh functionality
document.getElementById('autoRefreshToggle').addEventListener('change', function () {
    if (this.checked) {
        startAutoRefresh();
    } else {
        stopAutoRefresh();
    }
});

function startAutoRefresh() {
    autoRefreshInterval = setInterval(refreshQueue, 10000); // Refresh every 10 seconds
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
    }
}

// Refresh queue data in background without page reload
async function refreshQueue() {
    // Check if any modal is currently open - don't interrupt if user is filling out a form
    const openModals = document.querySelectorAll('.modal.show');
    if (openModals.length > 0) {
        console.log('Modal is open, skipping auto-refresh to avoid interruption');
        return;
    }
    
    try {
        const response = await fetch('/Appointments/GetQueueData');
        const result = await response.json();
        
        if (result.success && result.data) {
            // Update statistics
            updateQueueStats(result.data.stats);
            
            // Update current patient section
            updateCurrentPatient(result.data.currentPatient);
            
            // Update waiting queue
            updateWaitingQueue(result.data.waitingPatients);
        }
    } catch (error) {
        console.error('Error refreshing queue:', error);
    }
}

// Update queue statistics
function updateQueueStats(stats) {
    const totalWaitingEl = document.getElementById('totalWaiting');
    const inProgressEl = document.getElementById('inProgress');
    const completedTodayEl = document.getElementById('completedToday');
    
    if (totalWaitingEl) totalWaitingEl.textContent = stats.totalWaiting;
    if (inProgressEl) inProgressEl.textContent = stats.inProgress;
    if (completedTodayEl) completedTodayEl.textContent = stats.completedToday;
}

// Update current patient section
function updateCurrentPatient(currentPatient) {
    const currentPatientSection = document.querySelector('.current-patient');
    if (!currentPatientSection) return;
    
    if (currentPatient) {
        currentPatientSection.innerHTML = `
            <h3><i class="fas fa-user-md"></i> Currently Serving</h3>
            <div class="patient-name">${escapeHtml(currentPatient.patientName)}</div>
            <div class="queue-number">Queue #${currentPatient.queueNumber}</div>
            <div class="queue-controls">
                ${currentPatient.hasWaitingPatients ? 
                    `<button class="btn btn-primary btn-large" onclick="showNextPatientModal()">
                        <i class="fas fa-forward"></i> Next Patient
                    </button>` :
                    `<button class="btn btn-success btn-large" onclick="completeCurrentAppointment(${currentPatient.appointmentId}, ${currentPatient.patientId})">
                        <i class="fas fa-check-circle"></i> Complete Appointment
                    </button>`
                }
            </div>
        `;
    } else {
        currentPatientSection.innerHTML = `
            <h3><i class="fas fa-user-md"></i> No Patient Currently Being Served</h3>
            <div class="queue-controls">
                <button class="btn btn-primary btn-large" onclick="showNextPatientModal()">
                    <i class="fas fa-play"></i> Start Next Patient
                </button>
            </div>
        `;
    }
}

// Update waiting queue list
function updateWaitingQueue(waitingPatients) {
    const waitingQueueSection = document.querySelector('.waiting-queue');
    if (!waitingQueueSection) return;
    
    // Remove existing queue items (keep the header)
    const existingItems = waitingQueueSection.querySelectorAll('.queue-item, .no-patients');
    existingItems.forEach(item => item.remove());
    
    if (waitingPatients.length === 0) {
        waitingQueueSection.insertAdjacentHTML('beforeend', `
            <div class="no-patients">
                <i class="fas fa-user-friends"></i>
                <h5>No patients waiting in queue</h5>
                <p>All appointments for today have been processed or no appointments are scheduled.</p>
            </div>
        `);
        return;
    }
    
    // Add new queue items
    waitingPatients.forEach(patient => {
        const queueItemHtml = `
            <div class="queue-item">
                <div class="queue-info">
                    <div class="queue-number-badge">#${patient.queueNumber}</div>
                    <div class="patient-details">
                        <h5>${escapeHtml(patient.patientName)}</h5>
                        <p>${escapeHtml(patient.reasonForVisit)}</p>
                        <p><i class="fas fa-clock"></i> ${patient.startTime} - ${patient.endTime}</p>
                    </div>
                </div>
                <div>
                    <button class="btn btn-primary btn-sm" onclick="startAppointment(${patient.appointmentId})">
                        <i class="fas fa-play"></i> Start
                    </button>
                </div>
            </div>
        `;
        waitingQueueSection.insertAdjacentHTML('beforeend', queueItemHtml);
    });
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Show the next patient triage modal
function showNextPatientModal() {
    // Reset form
    document.getElementById('nextPatientForm').reset();

    // Show the modal using Bootstrap 5
    const modalElement = document.getElementById('nextPatientModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
}

// Submit next patient triage and complete current patient
async function submitNextPatient() {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Collect triage data from form
        const pulseRate = document.getElementById('triagePulseRate').value ? parseInt(document.getElementById('triagePulseRate').value) : null;
        const bloodPressure = document.getElementById('triageBloodPressure').value.trim() || null;
        const temperature = document.getElementById('triageTemperature').value ? parseFloat(document.getElementById('triageTemperature').value) : null;
        const oxygenSaturation = document.getElementById('triageOxygenSaturation').value ? parseInt(document.getElementById('triageOxygenSaturation').value) : null;
        const bmi = document.getElementById('triageBMI').value ? parseFloat(document.getElementById('triageBMI').value) : null;
        const allergies = document.getElementById('triageAllergies').value.trim() || null;
        const triageNotes = document.getElementById('triageNotes').value.trim() || null;

        const response = await fetch('/Appointments/NextInQueue', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({
                pulseRate: pulseRate,
                bloodPressure: bloodPressure,
                temperature: temperature,
                oxygenSaturation: oxygenSaturation,
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
            
            alert(result.message);
            // Refresh queue data in background
            setTimeout(() => refreshQueue(), 500);
        } else {
            alert('Error: ' + result.message);
        }
    } catch (error) {
        console.error('Error moving to next patient:', error);
        alert('An error occurred while moving to the next patient.');
    }
}

// Show complete appointment modal for current patient
function completeCurrentAppointment(appointmentId, patientId) {
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

async function startAppointment(appointmentId) {
    if (!confirm('Are you sure you want to start this appointment?')) {
        return;
    }

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const response = await fetch('/Appointments/UpdateStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-CSRF-TOKEN': token
            },
            body: JSON.stringify({
                appointmentId: appointmentId,
                status: 'In Progress'
            })
        });

        const result = await response.json();

        if (result.success) {
            alert(result.message);
            // Refresh queue data in background
            setTimeout(() => refreshQueue(), 500);
        } else {
            alert('Error: ' + result.error);
        }
    } catch (error) {
        console.error('Error starting appointment:', error);
        alert('An error occurred while starting the appointment.');
    }
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
            // Refresh queue data in background
            setTimeout(() => refreshQueue(), 500);
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

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    // Start auto-refresh by default
    document.getElementById('autoRefreshToggle').checked = true;
    startAutoRefresh();

    // Reset complete appointment modal when hidden
    const completeModalElement = document.getElementById('completeAppointmentModal');
    if (completeModalElement) {
        completeModalElement.addEventListener('hidden.bs.modal', function () {
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