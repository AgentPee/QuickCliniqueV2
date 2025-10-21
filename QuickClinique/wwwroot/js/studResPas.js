// Toggle password visibility
function togglePassword(inputId) {
    const passwordInput = document.getElementById(inputId);
    const toggleIcon = passwordInput.parentElement.querySelector('.toggle-password');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.textContent = '??';
        toggleIcon.setAttribute('aria-label', 'Hide password');
    } else {
        passwordInput.type = 'password';
        toggleIcon.textContent = '???';
        toggleIcon.setAttribute('aria-label', 'Show password');
    }

    // Focus back on the input for better UX
    passwordInput.focus();
}

// Form submission loading state
document.addEventListener('DOMContentLoaded', function () {
    const resetPasswordForm = document.querySelector('.auth-form');

    if (resetPasswordForm) {
        resetPasswordForm.addEventListener('submit', function (e) {
            const submitBtn = this.querySelector('.submit-btn');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.classList.add('loading');
                submitBtn.disabled = true;
            }
        });
    }
});