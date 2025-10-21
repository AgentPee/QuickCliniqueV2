// Loading state for buttons
function setLoadingState(button, isLoading) {
    if (isLoading) {
        button.classList.add('loading');
        button.disabled = true;
    } else {
        button.classList.remove('loading');
        button.disabled = false;
    }
}

// Show message function
function showMessage(message, type = 'success') {
    const existingMessages = document.querySelectorAll('.success-message, .error-message');
    existingMessages.forEach(msg => msg.remove());

    const messageDiv = document.createElement('div');
    messageDiv.className = type === 'success' ? 'success-message' : 'error-message';
    messageDiv.textContent = message;
    messageDiv.style.display = 'block';

    const activeForm = document.querySelector('.auth-form');
    activeForm.parentNode.insertBefore(messageDiv, activeForm);

    setTimeout(() => {
        messageDiv.style.opacity = '0';
        setTimeout(() => {
            messageDiv.remove();
        }, 300);
    }, 5000);
}

// Enhanced input validation with real-time feedback
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

// Email input validation
document.addEventListener('DOMContentLoaded', function () {
    const emailInput = document.querySelector('input[type="email"]');

    if (emailInput) {
        emailInput.addEventListener('blur', function () {
            if (this.value && !validateEmail(this.value)) {
                this.style.borderColor = '#e74c3c';
                this.style.boxShadow = '0 0 0 3px rgba(231, 76, 60, 0.1)';
            } else if (this.value) {
                this.style.borderColor = '#2ecc71';
                this.style.boxShadow = '0 0 0 3px rgba(46, 204, 113, 0.1)';
            }
        });

        emailInput.addEventListener('input', function () {
            this.style.borderColor = '#e1e5e9';
            this.style.boxShadow = 'none';
        });
    }

    // Form submission handling - ONLY UI ENHANCEMENTS
    const form = document.getElementById('forgotPasswordForm');
    const submitBtn = form?.querySelector('.submit-btn');

    if (form && submitBtn) {
        form.addEventListener('submit', function (e) {
            const email = document.querySelector('input[type="email"]').value.trim();

            // Basic client-side validation - ONLY UI, doesn't prevent form submission
            if (!validateEmail(email)) {
                // Just show visual feedback, don't prevent default
                emailInput.style.borderColor = '#e74c3c';
                emailInput.style.boxShadow = '0 0 0 3px rgba(231, 76, 60, 0.1)';
            }

            // Set loading state for better UX
            setLoadingState(submitBtn, true);

            // Let the form submit naturally - NO FUNCTIONALITY CHANGES
            // The server-side validation and processing remains exactly the same
        });
    }

    // Auto-hide success messages after 5 seconds
    const successMessage = document.querySelector('.success-message');
    if (successMessage && successMessage.textContent.trim() !== '') {
        setTimeout(() => {
            successMessage.style.opacity = '0';
            setTimeout(() => {
                successMessage.remove();
            }, 300);
        }, 5000);
    }
});

// Keyboard navigation support
document.addEventListener('keydown', function (e) {
    // Enter key to submit forms
    if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
        const form = e.target.closest('form');
        if (form && form.checkValidity()) {
            const submitBtn = form.querySelector('.submit-btn');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.click();
            }
        }
    }

    // Escape key to clear forms
    if (e.key === 'Escape') {
        const activeForm = document.querySelector('form');
        if (activeForm) {
            activeForm.reset();
            const firstInput = activeForm.querySelector('input');
            if (firstInput) firstInput.focus();
        }
    }
});