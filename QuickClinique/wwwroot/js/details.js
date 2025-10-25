// Appointment Details JavaScript Functions

function confirmAppointment(appointmentId) {
    if (!confirm('Are you sure you want to confirm this appointment?')) {
        return;
    }

    $.ajax({
        url: '/Appointments/ConfirmAppointment',
        type: 'POST',
        data: {
            appointmentId: appointmentId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert('Appointment confirmed successfully!');
                location.reload();
            } else {
                alert('Error: ' + (response.error || 'Failed to confirm appointment'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error confirming appointment:', error);
            alert('An error occurred. Please try again.');
        }
    });
}

function cancelAppointment(appointmentId) {
    if (!confirm('Are you sure you want to cancel this appointment? This action cannot be undone.')) {
        return;
    }

    const reason = prompt('Please enter a reason for cancellation (optional):');

    $.ajax({
        url: '/Appointments/CancelAppointment',
        type: 'POST',
        data: {
            appointmentId: appointmentId,
            reason: reason || 'Cancelled by clinic staff',
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert('Appointment cancelled successfully!');
                location.reload();
            } else {
                alert('Error: ' + (response.error || 'Failed to cancel appointment'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error cancelling appointment:', error);
            alert('An error occurred. Please try again.');
        }
    });
}

function startAppointment(appointmentId) {
    if (!confirm('Start this appointment? The patient will be marked as being served.')) {
        return;
    }

    $.ajax({
        url: '/Appointments/UpdateStatus',
        type: 'POST',
        data: {
            appointmentId: appointmentId,
            status: 'In Progress',
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert('Appointment started!');
                location.reload();
            } else {
                alert('Error: ' + (response.error || 'Failed to start appointment'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error starting appointment:', error);
            alert('An error occurred. Please try again.');
        }
    });
}

function completeAppointment(appointmentId) {
    if (!confirm('Mark this appointment as completed?')) {
        return;
    }

    $.ajax({
        url: '/Appointments/UpdateStatus',
        type: 'POST',
        data: {
            appointmentId: appointmentId,
            status: 'Completed',
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                alert('Appointment completed!');
                location.reload();
            } else {
                alert('Error: ' + (response.error || 'Failed to complete appointment'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error completing appointment:', error);
            alert('An error occurred. Please try again.');
        }
    });
}

// Add smooth transitions for better user experience
document.addEventListener('DOMContentLoaded', function () {
    // Add loading states to buttons
    const buttons = document.querySelectorAll('.btn');
    buttons.forEach(button => {
        button.addEventListener('click', function (e) {
            if (this.getAttribute('onclick')) {
                // Add loading state
                const originalText = this.innerHTML;
                this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
                this.disabled = true;

                // Revert after 5 seconds if something goes wrong
                setTimeout(() => {
                    this.innerHTML = originalText;
                    this.disabled = false;
                }, 5000);
            }
        });
    });

    // Add fade-in animation to page elements
    const elements = document.querySelectorAll('.details-header, .status-banner, .info-section, .action-buttons');
    elements.forEach((element, index) => {
        element.style.opacity = '0';
        element.style.transform = 'translateY(20px)';
        element.style.transition = 'opacity 0.5s ease, transform 0.5s ease';

        setTimeout(() => {
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        }, index * 100);
    });
});