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

function refreshQueue() {
    // Check if any modal is currently open - don't interrupt if user is filling out a form
    const openModals = document.querySelectorAll('.modal.show');
    if (openModals.length > 0) {
        console.log('Modal is open, skipping auto-refresh to avoid interruption');
        return;
    }
    
    location.reload();
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
        const age = document.getElementById('triageAge').value ? parseInt(document.getElementById('triageAge').value) : null;
        const gender = document.getElementById('triageGender').value || null;
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
            
            alert(result.message);
            location.reload();
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
            location.reload();
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
            location.reload(); // Refresh to show updated status
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