// Form submission loading state
document.addEventListener('DOMContentLoaded', function () {
    const forgotPasswordForm = document.querySelector('.auth-form');

    if (forgotPasswordForm) {
        forgotPasswordForm.addEventListener('submit', function (e) {
            const submitBtn = this.querySelector('.submit-btn');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.classList.add('loading');
                submitBtn.disabled = true;
            }
        });
    }
});