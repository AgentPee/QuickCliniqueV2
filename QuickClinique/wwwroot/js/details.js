// Appointment Details JavaScript Functions

// Refresh appointment status in background
async function refreshAppointmentStatus() {
    try {
        // Get appointment ID from URL or page
        const appointmentId = getAppointmentIdFromPage();
        if (!appointmentId) {
            console.error('Could not find appointment ID');
            return;
        }

        const response = await fetch(`/Appointments/Details/${appointmentId}`);
        if (!response.ok) {
            throw new Error('Failed to fetch appointment details');
        }

        const html = await response.text();
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        
        // Update status banner
        const statusBanner = doc.querySelector('.status-banner');
        if (statusBanner) {
            const currentBanner = document.querySelector('.status-banner');
            if (currentBanner) {
                currentBanner.outerHTML = statusBanner.outerHTML;
            }
        }

        // Update action buttons
        const actionButtons = doc.querySelector('.action-buttons');
        if (actionButtons) {
            const currentButtons = document.querySelector('.action-buttons');
            if (currentButtons) {
                currentButtons.outerHTML = actionButtons.outerHTML;
            }
        }

        // Update status in info section
        const statusInfo = doc.querySelector('.info-section [data-status]');
        if (statusInfo) {
            const currentStatus = document.querySelector('.info-section [data-status]');
            if (currentStatus) {
                currentStatus.outerHTML = statusInfo.outerHTML;
            }
        }
    } catch (error) {
        console.error('Error refreshing appointment status:', error);
        // Fallback to full reload if refresh fails
        location.reload();
    }
}

// Get appointment ID from page
function getAppointmentIdFromPage() {
    // Try to get from URL
    const urlMatch = window.location.pathname.match(/\/Appointments\/Details\/(\d+)/);
    if (urlMatch) {
        return urlMatch[1];
    }
    
    // Try to get from data attribute
    const appointmentElement = document.querySelector('[data-appointment-id]');
    if (appointmentElement) {
        return appointmentElement.getAttribute('data-appointment-id');
    }
    
    // Try to get from hidden input
    const hiddenInput = document.querySelector('input[name="appointmentId"], input[id*="appointmentId"]');
    if (hiddenInput) {
        return hiddenInput.value;
    }
    
    return null;
}

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
                // Refresh appointment status in background
                refreshAppointmentStatus();
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
                // Refresh appointment status in background
                refreshAppointmentStatus();
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
                // Refresh appointment status in background
                refreshAppointmentStatus();
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
                // Refresh appointment status in background
                refreshAppointmentStatus();
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