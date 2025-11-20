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

// ID Number input validation
document.addEventListener('DOMContentLoaded', function () {
    const idNumberInput = document.getElementById('Idnumber');

    if (idNumberInput) {
        idNumberInput.addEventListener('input', function (e) {
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
    }

    // Phone number input - only allow digits and enforce 11-digit format
    const phoneInput = document.getElementById('PhoneNumber');
    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            // Remove any non-digit characters
            let value = e.target.value.replace(/\D/g, '');

            // Limit to 11 digits
            if (value.length > 11) {
                value = value.slice(0, 11);
            }

            e.target.value = value;
        });

        // Also add paste event to handle pasted content
        phoneInput.addEventListener('paste', function (e) {
            setTimeout(() => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 11) {
                    value = value.slice(0, 11);
                }
                e.target.value = value;
            }, 0);
        });
    }

    // Form submission loading state
    const registerForm = document.querySelector('.auth-form');
    if (registerForm) {
        registerForm.addEventListener('submit', function (e) {
            const submitBtn = this.querySelector('.submit-btn');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.classList.add('loading');
                submitBtn.disabled = true;
            }
        });
    }

    // File input preview functionality
    // Student ID images
    setupFilePreview('StudentIdImageFront', 'preview-front');
    setupFilePreview('StudentIdImageBack', 'preview-back');
    
    // Staff ID images
    setupFilePreview('StaffIdImageFront', 'preview-front');
    setupFilePreview('StaffIdImageBack', 'preview-back');
});

// Function to open camera
function openCamera(inputId) {
    const fileInput = document.getElementById(inputId);
    if (!fileInput) return;

    // Check if device has camera support
    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        // Set capture attribute to use camera
        fileInput.setAttribute('capture', 'environment');
        // Trigger file input click
        fileInput.click();
    } else {
        // Fallback: just open file picker
        fileInput.removeAttribute('capture');
        fileInput.click();
    }
}

// Function to setup file preview
function setupFilePreview(inputId, previewId) {
    const fileInput = document.getElementById(inputId);
    const preview = document.getElementById(previewId);

    if (!fileInput || !preview) return;

    fileInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            // Validate file type
            if (!file.type.startsWith('image/')) {
                alert('Please select an image file.');
                e.target.value = '';
                preview.classList.remove('has-image');
                preview.innerHTML = '';
                return;
            }

            // Validate file size (5MB)
            const maxSize = 5 * 1024 * 1024; // 5MB
            if (file.size > maxSize) {
                alert('File size exceeds 5MB. Please choose a smaller file.');
                e.target.value = '';
                preview.classList.remove('has-image');
                preview.innerHTML = '';
                return;
            }

            // Create preview
            const reader = new FileReader();
            reader.onload = function (e) {
                preview.innerHTML = `<img src="${e.target.result}" alt="Preview">`;
                preview.classList.add('has-image');
            };
            reader.readAsDataURL(file);
        } else {
            preview.classList.remove('has-image');
            preview.innerHTML = '';
        }
    });
}