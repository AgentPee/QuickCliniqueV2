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
            <div class="patient-name"><i class="fas fa-user"></i> ${escapeHtml(currentPatient.patientName)}</div>
            <div class="queue-number">🎫 Queue #${currentPatient.queueNumber}</div>
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
                        <p><i class="fas fa-clock"></i> ${patient.startTime} - ${patient.endTime}</p>
                    </div>
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
async function showNextPatientModal() {
    try {
        // Fetch next patient information
        const response = await fetch('/Appointments/GetQueueData');
        const result = await response.json();
        
        if (result.success && result.data && result.data.waitingPatients && result.data.waitingPatients.length > 0) {
            const nextPatient = result.data.waitingPatients[0];
            
            // Display patient information
            document.getElementById('nextPatientName').textContent = nextPatient.patientName || 'Unknown';
            document.getElementById('nextPatientQueueNumber').textContent = nextPatient.queueNumber || '-';
            document.getElementById('nextPatientReason').textContent = nextPatient.reasonForVisit || 'Not specified';
            document.getElementById('nextPatientTime').textContent = 
                (nextPatient.startTime && nextPatient.endTime) 
                    ? `${nextPatient.startTime} - ${nextPatient.endTime}` 
                    : '-';
        } else {
            // No patient waiting
            document.getElementById('nextPatientName').textContent = 'No patient waiting';
            document.getElementById('nextPatientQueueNumber').textContent = '-';
            document.getElementById('nextPatientReason').textContent = '-';
            document.getElementById('nextPatientTime').textContent = '-';
        }
    } catch (error) {
        console.error('Error fetching next patient info:', error);
        document.getElementById('nextPatientName').textContent = 'Error loading patient info';
    }

    // Reset form
    const form = document.getElementById('nextPatientForm');
    if (form) {
        form.reset();
    }

    // Reset all input fields explicitly
    const inputs = [
        'triagePulseRate', 'triageBPSystolic', 'triageBPDiastolic', 
        'triageTemperature', 'triageOxygenSaturation', 
        'triageHeight', 'triageWeight', 'triageAllergies', 'triageNotes'
    ];
    inputs.forEach(id => {
        const input = document.getElementById(id);
        if (input) {
            input.value = '';
        }
    });

    // Reset hidden fields
    const hiddenInputs = ['triageBloodPressure', 'triageBMI'];
    hiddenInputs.forEach(id => {
        const input = document.getElementById(id);
        if (input) {
            input.value = '';
        }
    });

    // Reset all vital result displays
    const vitalResults = document.querySelectorAll('.vital-result-modal');
    vitalResults.forEach(result => {
        result.classList.add('vital-result-modal-hidden');
    });
    
    // Reset BMI vital result display
    const bmiVitalResult = document.getElementById('bmiVitalResultModal');
    if (bmiVitalResult) {
        bmiVitalResult.classList.add('vital-result-modal-hidden');
    }

    // Reset all vital result value and category displays
    const valueModals = document.querySelectorAll('.vital-result-value-modal');
    valueModals.forEach(el => el.textContent = '-');
    
    const categoryModals = document.querySelectorAll('.vital-result-category-modal');
    categoryModals.forEach(el => {
        el.textContent = 'Enter value';
        el.className = 'vital-result-category-modal';
    });

    // Reset the submit button to its normal state (in case it was in loading state)
    const submitBtn = document.querySelector('#nextPatientModal .btn-primary[onclick="submitNextPatient()"]');
    if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = '<i class="fas fa-forward"></i> Start Next Patient';
    }

    // Show the modal using Bootstrap 5
    const modalElement = document.getElementById('nextPatientModal');
    if (modalElement) {
        // Get existing modal instance or create new one
        let modal = bootstrap.Modal.getInstance(modalElement);
        if (!modal) {
            modal = new bootstrap.Modal(modalElement, {
                backdrop: false,  // Disable backdrop to prevent blocking clicks
                keyboard: true,
                focus: true
            });
        } else {
            // Update existing modal to disable backdrop
            modal._config.backdrop = false;
        }
        modal.show();
    }
}

// Submit next patient triage and complete current patient
async function submitNextPatient() {
    try {
        // Disable submit button to prevent double submission
        const submitBtn = document.querySelector('#nextPatientModal .btn-primary[onclick="submitNextPatient()"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        }

        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenElement) {
            alert('Security token not found. Please refresh the page and try again.');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-forward"></i> Start Next Patient';
            }
            return;
        }
        const token = tokenElement.value;

        // Collect triage data from form with null safety
        const pulseRateInput = document.getElementById('triagePulseRate');
        const pulseRate = pulseRateInput && pulseRateInput.value ? parseInt(pulseRateInput.value) : null;
        
        const bloodPressureInput = document.getElementById('triageBloodPressure');
        const bloodPressure = bloodPressureInput && bloodPressureInput.value ? bloodPressureInput.value.trim() : null;
        
        const temperatureInput = document.getElementById('triageTemperature');
        const temperature = temperatureInput && temperatureInput.value ? parseFloat(temperatureInput.value) : null;
        
        const oxygenSaturationInput = document.getElementById('triageOxygenSaturation');
        const oxygenSaturation = oxygenSaturationInput && oxygenSaturationInput.value ? parseInt(oxygenSaturationInput.value) : null;
        
        const bmiInput = document.getElementById('triageBMI');
        const bmi = bmiInput && bmiInput.value ? parseFloat(bmiInput.value) : null;
        
        const allergiesInput = document.getElementById('triageAllergies');
        const allergies = allergiesInput && allergiesInput.value ? allergiesInput.value.trim() : null;
        
        const triageNotesInput = document.getElementById('triageNotes');
        const triageNotes = triageNotesInput && triageNotesInput.value ? triageNotesInput.value.trim() : null;

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

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (result.success) {
            // Hide modal
            const modalElement = document.getElementById('nextPatientModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            
            alert(result.message || 'Next patient started successfully!');
            // Refresh queue data in background
            setTimeout(() => refreshQueue(), 500);
        } else {
            alert('Error: ' + (result.message || 'Failed to start next patient'));
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-forward"></i> Start Next Patient';
            }
        }
    } catch (error) {
        console.error('Error moving to next patient:', error);
        alert('An error occurred while moving to the next patient. Please try again.');
        
        // Re-enable submit button
        const submitBtn = document.querySelector('#nextPatientModal .btn-primary[onclick="submitNextPatient()"]');
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="fas fa-forward"></i> Start Next Patient';
        }
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
    let modal = bootstrap.Modal.getInstance(modalElement);
    if (!modal) {
        modal = new bootstrap.Modal(modalElement, {
            backdrop: false,  // Disable backdrop to prevent blocking clicks
            keyboard: true,
            focus: true
        });
    } else {
        modal._config.backdrop = false;
    }
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
            const form = document.getElementById('nextPatientForm');
            if (form) {
                form.reset();
            }
            
            // Reset all input fields
            const inputs = [
                'triagePulseRate', 'triageBPSystolic', 'triageBPDiastolic', 
                'triageTemperature', 'triageOxygenSaturation', 
                'triageHeight', 'triageWeight', 'triageAllergies', 'triageNotes'
            ];
            inputs.forEach(id => {
                const input = document.getElementById(id);
                if (input) input.value = '';
            });

            // Reset hidden fields
            ['triageBloodPressure', 'triageBMI'].forEach(id => {
                const input = document.getElementById(id);
                if (input) input.value = '';
            });

            // Reset all vital result displays
            document.querySelectorAll('.vital-result-modal').forEach(result => {
                result.classList.add('vital-result-modal-hidden');
            });
            
            // Reset vital result values
            document.querySelectorAll('.vital-result-value-modal').forEach(el => el.textContent = '-');
            document.querySelectorAll('.vital-result-category-modal').forEach(el => {
                el.textContent = 'Enter value';
                el.className = 'vital-result-category-modal';
            });

            // Reset the submit button to its normal state
            const submitBtn = document.querySelector('#nextPatientModal .btn-primary[onclick="submitNextPatient()"]');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-forward"></i> Start Next Patient';
            }
        });
    }
});