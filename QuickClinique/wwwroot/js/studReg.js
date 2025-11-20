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
    setupFilePreview('StudentIdImageFront', 'preview-front');
    setupFilePreview('StudentIdImageBack', 'preview-back');
});

// Helper function to copy file from one input to another (cross-browser compatible)
function copyFileToInput(sourceInput, targetInput) {
    if (!sourceInput.files || sourceInput.files.length === 0) return;
    
    const file = sourceInput.files[0];
    
    // Modern browsers (Chrome, Firefox, Edge, Safari 14+)
    if (typeof DataTransfer !== 'undefined') {
        try {
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(file);
            targetInput.files = dataTransfer.files;
            return true;
        } catch (e) {
            console.warn('DataTransfer not fully supported, using fallback');
        }
    }
    
    // Fallback for older browsers: directly assign files if possible
    try {
        // This works in some browsers
        Object.defineProperty(targetInput, 'files', {
            value: sourceInput.files,
            writable: false
        });
        return true;
    } catch (e) {
        // If that doesn't work, we'll need to use a different approach
        console.warn('Direct file assignment not supported');
    }
    
    return false;
}

// Function to open file upload (file explorer)
function openFileUpload(inputId) {
    const uploadInput = document.getElementById(inputId + 'Upload');
    const mainInput = document.getElementById(inputId);
    
    if (!uploadInput || !mainInput) return;
    
    // Remove capture attribute to open file explorer
    uploadInput.removeAttribute('capture');
    
    // Set up one-time change handler
    const handleFileSelect = function(e) {
        const file = e.target.files[0];
        if (file) {
            // Copy file to main input
            if (copyFileToInput(uploadInput, mainInput)) {
                // Trigger change event on main input to update preview
                const changeEvent = new Event('change', { bubbles: true });
                mainInput.dispatchEvent(changeEvent);
            } else {
                // Fallback: use the upload input directly for preview
                const previewId = inputId === 'StudentIdImageFront' ? 'preview-front' : 'preview-back';
                const preview = document.getElementById(previewId);
                if (preview && file) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        preview.innerHTML = `<img src="${e.target.result}" alt="Preview">`;
                        preview.classList.add('has-image');
                    };
                    reader.readAsDataURL(file);
                }
                // Note: Form submission will need to handle this case
                alert('Please note: Some browsers may require you to use the same input for file selection and submission.');
            }
        }
        // Remove the handler after use
        uploadInput.removeEventListener('change', handleFileSelect);
    };
    
    uploadInput.addEventListener('change', handleFileSelect, { once: true });
    uploadInput.click();
}

// Function to open camera
function openCamera(inputId) {
    const cameraInput = document.getElementById(inputId + 'Camera');
    const mainInput = document.getElementById(inputId);
    
    if (!cameraInput || !mainInput) return;
    
    // Check browser support for camera
    const hasCameraSupport = 
        (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) ||
        (navigator.getUserMedia) ||
        (navigator.webkitGetUserMedia) ||
        (navigator.mozGetUserMedia) ||
        (navigator.msGetUserMedia);
    
    // Set capture attribute for mobile devices
    // 'environment' = back camera, 'user' = front camera
    // Try environment first (back camera), fallback to user (front camera)
    if (hasCameraSupport) {
        // For mobile devices, use capture attribute
        if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
            cameraInput.setAttribute('capture', 'environment');
        } else {
            // For desktop, try to use camera if available
            // Some browsers support camera on desktop
            cameraInput.setAttribute('capture', 'user');
        }
    } else {
        // Fallback: remove capture and just open file picker
        cameraInput.removeAttribute('capture');
    }
    
    // Set up one-time change handler
    const handleFileSelect = function(e) {
        const file = e.target.files[0];
        if (file) {
            // Copy file to main input
            if (copyFileToInput(cameraInput, mainInput)) {
                // Trigger change event on main input to update preview
                const changeEvent = new Event('change', { bubbles: true });
                mainInput.dispatchEvent(changeEvent);
            } else {
                // Fallback: use the camera input directly for preview
                const previewId = inputId === 'StudentIdImageFront' ? 'preview-front' : 'preview-back';
                const preview = document.getElementById(previewId);
                if (preview && file) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        preview.innerHTML = `<img src="${e.target.result}" alt="Preview">`;
                        preview.classList.add('has-image');
                    };
                    reader.readAsDataURL(file);
                }
                // Note: Form submission will need to handle this case
                alert('Please note: Some browsers may require you to use the same input for file selection and submission.');
            }
        }
        // Remove the handler after use
        cameraInput.removeEventListener('change', handleFileSelect);
    };
    
    cameraInput.addEventListener('change', handleFileSelect, { once: true });
    cameraInput.click();
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