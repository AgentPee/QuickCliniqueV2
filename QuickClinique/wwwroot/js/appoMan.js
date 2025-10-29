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

function refreshAppointments() {
    location.reload();
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
            location.reload(); // Refresh to show updated status
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

    // Prepare form data
    const formData = {
        appointmentId: parseInt(appointmentId),
        patientId: parseInt(patientId),
        diagnosis: diagnosis,
        medications: document.getElementById('medications').value.trim() || null,
        allergies: document.getElementById('allergies').value.trim() || null,
        age: document.getElementById('patientAge').value ? parseInt(document.getElementById('patientAge').value) : null,
        gender: document.getElementById('patientGender').value || null,
        bmi: document.getElementById('patientBMI').value ? parseFloat(document.getElementById('patientBMI').value) : null,
        additionalNotes: document.getElementById('additionalNotes').value.trim() || null
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

// Reset form when modal is hidden
document.addEventListener('DOMContentLoaded', function () {
    const modalElement = document.getElementById('completeAppointmentModal');
    if (modalElement) {
        modalElement.addEventListener('hidden.bs.modal', function () {
            document.getElementById('completeAppointmentForm').reset();
        });
    }
});