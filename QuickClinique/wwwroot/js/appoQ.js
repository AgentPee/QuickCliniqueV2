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
    location.reload();
}

async function nextInQueue() {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const response = await fetch('/Appointments/NextInQueue', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-CSRF-TOKEN': token
            }
        });

        const result = await response.json();

        if (result.success) {
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

async function completeAppointment(appointmentId) {
    if (!confirm('Are you sure you want to mark this appointment as completed?')) {
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
                status: 'Completed'
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
        console.error('Error completing appointment:', error);
        alert('An error occurred while completing the appointment.');
    }
}

// Start auto-refresh by default
document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('autoRefreshToggle').checked = true;
    startAutoRefresh();
});