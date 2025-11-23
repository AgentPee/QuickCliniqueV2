// Resend verification link
function resendVerificationLink() {
    const resendBtn = document.querySelector('.resend-btn');
    const resendBtnText = resendBtn.querySelector('.resend-btn-text');
    const resendBtnLoader = resendBtn.querySelector('.resend-btn-loader');
    
    // Get email from data attribute
    const formContainer = document.querySelector('.form-container');
    const email = formContainer?.getAttribute('data-email') || '';

    if (!email) {
        showMessage('Email address not found. Please access this page with an email parameter.', 'error');
        return;
    }

    // Disable button and show loading state
    resendBtn.disabled = true;
    resendBtn.classList.add('loading');
    resendBtnText.style.opacity = '0';
    resendBtnLoader.style.opacity = '1';

    // Get anti-forgery token if available
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    
    // Make AJAX request to resend verification link
    fetch('/Student/ResendVerificationLink', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: JSON.stringify({ email: email })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Show success message
            showMessage(data.message || 'Verification link has been resent to your email.', 'success');
            
            // Enable button after 60 seconds (cooldown)
            setTimeout(() => {
                resendBtn.disabled = false;
                resendBtn.classList.remove('loading');
                resendBtnText.style.opacity = '1';
                resendBtnLoader.style.opacity = '0';
            }, 60000);
        } else {
            showMessage(data.error || 'Failed to resend verification link. Please try again.', 'error');
            resendBtn.disabled = false;
            resendBtn.classList.remove('loading');
            resendBtnText.style.opacity = '1';
            resendBtnLoader.style.opacity = '0';
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showMessage('An error occurred. Please try again.', 'error');
        resendBtn.disabled = false;
        resendBtn.classList.remove('loading');
        resendBtnText.style.opacity = '1';
        resendBtnLoader.style.opacity = '0';
    });
}

// Show message function
function showMessage(message, type) {
    // Remove existing messages
    const existingMessages = document.querySelectorAll('.temp-message');
    existingMessages.forEach(msg => msg.remove());

    // Create new message
    const messageDiv = document.createElement('div');
    messageDiv.className = `temp-message ${type === 'success' ? 'success-message' : 'error-message'}`;
    messageDiv.textContent = message;
    
    // Insert before form
    const form = document.querySelector('.auth-form');
    form.parentNode.insertBefore(messageDiv, form);

    // Auto remove after 5 seconds
    setTimeout(() => {
        messageDiv.remove();
    }, 5000);
}

// Page loaded
document.addEventListener('DOMContentLoaded', function () {
    // No form submission needed - verification is done via email link
});

