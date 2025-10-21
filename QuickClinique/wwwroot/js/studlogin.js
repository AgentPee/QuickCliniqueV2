﻿// Toggle between login and registration forms
function showLogin() {
    document.getElementById('loginFormWrapper').classList.add('active');
    document.getElementById('registerFormWrapper').classList.remove('active');
    document.getElementById('loginToggle').classList.add('active');
    document.getElementById('registerToggle').classList.remove('active');
    document.getElementById('tagline').textContent = 'Your Health, Our Priority';

    // Focus on first input
    setTimeout(() => {
        document.getElementById('loginIdNumber').focus();
    }, 300);
}

function showRegister() {
    document.getElementById('registerFormWrapper').classList.add('active');
    document.getElementById('loginFormWrapper').classList.remove('active');
    document.getElementById('registerToggle').classList.add('active');
    document.getElementById('loginToggle').classList.remove('active');
    document.getElementById('tagline').textContent = 'Join Our Healthcare Community';

    // Focus on first input
    setTimeout(() => {
        document.getElementById('name').focus();
    }, 300);
}

// Toggle password visibility
function togglePassword(inputId) {
    const passwordInput = document.getElementById(inputId);
    const toggleIcon = passwordInput.parentElement.querySelector('.toggle-password');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.textContent = '🙈';
        toggleIcon.setAttribute('aria-label', 'Hide password');
    } else {
        passwordInput.type = 'password';
        toggleIcon.textContent = '👁️';
        toggleIcon.setAttribute('aria-label', 'Show password');
    }

    // Focus back on the input for better UX
    passwordInput.focus();
}

// Form validation
function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function validatePassword(password) {
    return password.length >= 6;
}

// ID Number validation
function validateIdNumber(idNumber) {
    const idRegex = /^[0-9]{8}$/;
    return idRegex.test(idNumber);
}

// Show message function
function showMessage(message, type = 'success') {
    const existingMessages = document.querySelectorAll('.success-message, .error-message');
    existingMessages.forEach(msg => msg.remove());

    const messageDiv = document.createElement('div');
    messageDiv.className = type === 'success' ? 'success-message' : 'error-message';
    messageDiv.textContent = message;
    messageDiv.style.display = 'block';

    const activeForm = document.querySelector('.form-wrapper.active .auth-form');
    activeForm.parentNode.insertBefore(messageDiv, activeForm);

    setTimeout(() => {
        messageDiv.style.opacity = '0';
        setTimeout(() => {
            messageDiv.remove();
        }, 300);
    }, 5000);
}

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

// Simulate API call
function simulateAPICall(endpoint, data) {
    return new Promise((resolve, reject) => {
        setTimeout(() => {
            if (Math.random() > 0.2) {
                resolve({
                    success: true,
                    message: 'Operation completed successfully',
                    user: {
                        id: 1,
                        email: data.email,
                        name: data.name || 'User'
                    }
                });
            } else {
                reject({
                    success: false,
                    message: 'Something went wrong. Please try again.'
                });
            }
        }, 2000);
    });
}

// Login form handler
document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');

    // ID Number input validation
    const idNumberInputs = document.querySelectorAll('input[name="idNumber"]');
    idNumberInputs.forEach(input => {
        input.addEventListener('input', function (e) {
            // Only allow numbers
            this.value = this.value.replace(/[^0-9]/g, '');

            // Limit to 8 characters
            if (this.value.length > 8) {
                this.value = this.value.slice(0, 8);
            }

            // Visual feedback
            if (this.value.length === 8) {
                this.style.borderColor = '#2ecc71';
                this.style.boxShadow = '0 0 0 3px rgba(46, 204, 113, 0.1)';
            } else if (this.value.length > 0) {
                this.style.borderColor = '#e74c3c';
                this.style.boxShadow = '0 0 0 3px rgba(231, 76, 60, 0.1)';
            } else {
                this.style.borderColor = '#e1e5e9';
                this.style.boxShadow = 'none';
            }
        });
    });

    // Login form submission
    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const submitBtn = this.querySelector('.submit-btn');
            const idNumber = document.getElementById('loginIdNumber').value.trim();
            const password = document.getElementById('loginPassword').value;

            // Validation
            if (!validateIdNumber(idNumber)) {
                showMessage('Please enter a valid 8-digit ID number', 'error');
                return;
            }

            if (!validatePassword(password)) {
                showMessage('Password must be at least 6 characters long', 'error');
                return;
            }

            setLoadingState(submitBtn, true);

            try {
                const response = await simulateAPICall('/api/login', { idNumber, password });
                showMessage('Login successful! Redirecting...', 'success');

                // Simulate redirect
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 1500);

            } catch (error) {
                showMessage(error.message, 'error');
            } finally {
                setLoadingState(submitBtn, false);
            }
        });
    }

    // Registration form submission
    if (registerForm) {
        registerForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const submitBtn = this.querySelector('.submit-btn');
            const name = document.getElementById('name').value.trim();
            const email = document.getElementById('email').value.trim();
            const idNumber = document.getElementById('idNumber').value.trim();
            const phone = document.getElementById('phone').value.trim();
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const dateOfBirth = document.getElementById('dateOfBirth').value;
            const gender = document.getElementById('gender').value;
            const terms = document.getElementById('terms').checked;

            // Validation
            if (name.length < 2) {
                showMessage('Please enter your full name', 'error');
                return;
            }

            if (!validateEmail(email)) {
                showMessage('Please enter a valid email address', 'error');
                return;
            }

            if (!validateIdNumber(idNumber)) {
                showMessage('Please enter a valid 8-digit ID number', 'error');
                return;
            }

            if (phone.length < 10) {
                showMessage('Please enter a valid phone number', 'error');
                return;
            }

            if (!validatePassword(password)) {
                showMessage('Password must be at least 6 characters long', 'error');
                return;
            }

            if (password !== confirmPassword) {
                showMessage('Passwords do not match', 'error');
                return;
            }

            if (!dateOfBirth) {
                showMessage('Please select your date of birth', 'error');
                return;
            }

            if (!gender) {
                showMessage('Please select your gender', 'error');
                return;
            }

            if (!terms) {
                showMessage('Please accept the Terms of Service', 'error');
                return;
            }

            setLoadingState(submitBtn, true);

            try {
                const response = await simulateAPICall('/api/register', {
                    name,
                    email,
                    idNumber,
                    phone,
                    password,
                    dateOfBirth,
                    gender,
                    newsletter: document.getElementById('newsletter').checked
                });

                showMessage('Account created successfully! Welcome to QuickClinic.', 'success');

                // Auto-switch to login form after successful registration
                setTimeout(() => {
                    showLogin();
                    loginForm.reset();
                }, 2000);

            } catch (error) {
                showMessage(error.message, 'error');
            } finally {
                setLoadingState(submitBtn, false);
            }
        });
    }

    // Terms and Privacy links
    const termsLinks = document.querySelectorAll('.terms-link, .privacy-link');
    termsLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            showMessage('Opening terms and conditions...', 'success');
        });
    });

    // Enhanced input validation with real-time feedback
    const emailInputs = document.querySelectorAll('input[type="email"]');
    emailInputs.forEach(input => {
        input.addEventListener('blur', function () {
            if (this.value && !validateEmail(this.value)) {
                this.style.borderColor = '#e74c3c';
                this.style.boxShadow = '0 0 0 3px rgba(231, 76, 60, 0.1)';
            } else if (this.value) {
                this.style.borderColor = '#2ecc71';
                this.style.boxShadow = '0 0 0 3px rgba(46, 204, 113, 0.1)';
            }
        });

        input.addEventListener('input', function () {
            this.style.borderColor = '#e1e5e9';
            this.style.boxShadow = 'none';
        });
    });

    // Password strength indicator
    const passwordInputs = document.querySelectorAll('input[type="password"]');
    passwordInputs.forEach(input => {
        input.addEventListener('input', function () {
            if (this.value.length > 0) {
                const strength = getPasswordStrength(this.value);
                const color = strength === 'strong' ? '#2ecc71' :
                    strength === 'medium' ? '#f39c12' : '#e74c3c';

                this.style.borderColor = color;
                this.style.boxShadow = `0 0 0 3px ${color}20`;
            } else {
                this.style.borderColor = '#e1e5e9';
                this.style.boxShadow = 'none';
            }
        });
    });

    // Phone number formatting
    const phoneInput = document.getElementById('phone');
    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');

            if (value.length > 3 && value.length <= 6) {
                value = value.replace(/(\d{3})(\d+)/, '$1-$2');
            } else if (value.length > 6) {
                value = value.replace(/(\d{3})(\d{3})(\d+)/, '$1-$2-$3');
            }

            e.target.value = value;
        });
    }
});

// Password strength checker
function getPasswordStrength(password) {
    if (password.length >= 8 && /[A-Z]/.test(password) && /[0-9]/.test(password) && /[^A-Za-z0-9]/.test(password)) {
        return 'strong';
    } else if (password.length >= 6) {
        return 'medium';
    } else {
        return 'weak';
    }
}

// Enhanced form validation functions
function validatePhone(phone) {
    const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
    return phoneRegex.test(phone.replace(/\D/g, ''));
}

function validateName(name) {
    return name.length >= 2 && /^[a-zA-Z\s]+$/.test(name);
}

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
        const activeForm = document.querySelector('.form-wrapper.active form');
        if (activeForm) {
            activeForm.reset();
            const firstInput = activeForm.querySelector('input');
            if (firstInput) firstInput.focus();
        }
    }
});