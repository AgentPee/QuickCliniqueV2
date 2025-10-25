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
        error: function () {
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
        error: function () {
            alert('An error occurred. Please try again.');
        }
    });
}

// Add some interactive effects
$(document).ready(function () {
    // Animate table rows on load
    $('tbody tr').each(function (i) {
        $(this).delay(i * 100).animate({ opacity: 1 }, 400);
    });

    // Add hover effects for status badges
    $('.status-badge').hover(
        function () {
            $(this).css('transform', 'scale(1.05)');
        },
        function () {
            $(this).css('transform', 'scale(1)');
        }
    );

    // Add loading state to buttons
    $('.btn-success, .btn-danger').on('click', function () {
        const originalText = $(this).html();
        $(this).html('<i class="fas fa-spinner fa-spin"></i> Processing...').prop('disabled', true);

        // Re-enable button after 3 seconds if something goes wrong
        setTimeout(() => {
            $(this).html(originalText).prop('disabled', false);
        }, 3000);
    });
});