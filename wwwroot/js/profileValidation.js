/**
 * Profile Edit Modal - Client-side Validation
 * Validates field lengths, file types, and sizes
 * Shows inline error messages
 */

(function() {
    'use strict';

    // Validation constants
    const VALIDATION_RULES = {
        bio: {
            maxLength: 500,
            fieldName: 'Giới thiệu'
        },
        placeOfBirth: {
            maxLength: 100,
            fieldName: 'Nơi sinh sống'
        },
        interests: {
            maxLength: 200,
            fieldName: 'Sở thích'
        },
        image: {
            maxSize: 5 * 1024 * 1024, // 5MB in bytes
            allowedTypes: ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'],
            allowedExtensions: ['.jpg', '.jpeg', '.png', '.gif']
        }
    };

    // Validation state
    const validationState = {
        avatar: true,
        cover: true,
        bio: true,
        placeOfBirth: true,
        interests: true
    };

    /**
     * Initialize validation when DOM is ready
     */
    function initValidation() {
        // Character count for text fields
        initCharacterCount('bioInput', 'bioCharCount', VALIDATION_RULES.bio.maxLength);
        initCharacterCount('interestsInput', 'interestsCharCount', VALIDATION_RULES.interests.maxLength);

        // Real-time validation for text fields
        initTextFieldValidation('bioInput', 'bioError', VALIDATION_RULES.bio);
        initTextFieldValidation('placeOfBirthInput', 'placeOfBirthError', VALIDATION_RULES.placeOfBirth);
        initTextFieldValidation('interestsInput', 'interestsError', VALIDATION_RULES.interests);

        // File input validation
        initFileValidation('avatarFileInput', 'avatarError', 'avatarPreviewModal', 'avatar');
        initFileValidation('coverFileInput', 'coverError', 'coverPreviewModal', 'cover');

        // Form submission validation
        const saveBtn = document.getElementById('saveProfileBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', handleFormSubmit);
        }
    }

    /**
     * Initialize character counter for a text field
     */
    function initCharacterCount(inputId, counterId, maxLength) {
        const input = document.getElementById(inputId);
        const counter = document.getElementById(counterId);
        
        if (!input || !counter) return;

        input.addEventListener('input', function() {
            const currentLength = this.value.length;
            counter.textContent = currentLength;
            
            // Visual feedback when approaching limit
            if (currentLength > maxLength * 0.9) {
                counter.style.color = '#dc3545'; // Red
            } else if (currentLength > maxLength * 0.75) {
                counter.style.color = '#ffc107'; // Yellow
            } else {
                counter.style.color = ''; // Default
            }
        });
    }

    /**
     * Initialize real-time validation for text fields
     */
    function initTextFieldValidation(inputId, errorId, rules) {
        const input = document.getElementById(inputId);
        const errorDiv = document.getElementById(errorId);
        
        if (!input || !errorDiv) return;

        input.addEventListener('blur', function() {
            validateTextField(this, errorDiv, rules);
        });

        // Clear error on input
        input.addEventListener('input', function() {
            if (this.value.length <= rules.maxLength) {
                clearError(this, errorDiv);
            }
        });
    }

    /**
     * Validate a text field
     */
    function validateTextField(input, errorDiv, rules) {
        const value = input.value.trim();
        const fieldKey = input.id.replace('Input', '');

        // Check length
        if (value.length > rules.maxLength) {
            showError(input, errorDiv, `${rules.fieldName} không được vượt quá ${rules.maxLength} ký tự`);
            validationState[fieldKey] = false;
            return false;
        }

        clearError(input, errorDiv);
        validationState[fieldKey] = true;
        return true;
    }

    /**
     * Initialize file input validation
     */
    function initFileValidation(inputId, errorId, previewId, stateKey) {
        const input = document.getElementById(inputId);
        const errorDiv = document.getElementById(errorId);
        const preview = document.getElementById(previewId);
        
        if (!input || !errorDiv) return;

        input.addEventListener('change', function(e) {
            validateImageFile(this, errorDiv, preview, stateKey);
        });
    }

    /**
     * Validate image file
     */
    function validateImageFile(input, errorDiv, preview, stateKey) {
        const file = input.files[0];
        
        if (!file) {
            validationState[stateKey] = true;
            return true;
        }

        // Check file size
        if (file.size > VALIDATION_RULES.image.maxSize) {
            const sizeMB = (VALIDATION_RULES.image.maxSize / (1024 * 1024)).toFixed(0);
            showError(input, errorDiv, `Kích thước file không được vượt quá ${sizeMB}MB`);
            input.value = ''; // Clear the input
            validationState[stateKey] = false;
            return false;
        }

        // Check file type
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        const isValidType = VALIDATION_RULES.image.allowedTypes.includes(file.type) ||
                           VALIDATION_RULES.image.allowedExtensions.includes(fileExtension);

        if (!isValidType) {
            showError(input, errorDiv, 'Chỉ chấp nhận file ảnh định dạng JPG, PNG, hoặc GIF');
            input.value = ''; // Clear the input
            validationState[stateKey] = false;
            return false;
        }

        // If validation passes, show preview
        clearError(input, errorDiv);
        validationState[stateKey] = true;
        
        if (preview) {
            const reader = new FileReader();
            reader.onload = function(e) {
                preview.src = e.target.result;
            };
            reader.readAsDataURL(file);
        }

        return true;
    }

    /**
     * Show error message
     */
    function showError(input, errorDiv, message) {
        input.classList.add('is-invalid');
        errorDiv.textContent = message;
        errorDiv.style.display = 'block';
        errorDiv.classList.remove('d-none');
    }

    /**
     * Clear error message
     */
    function clearError(input, errorDiv) {
        input.classList.remove('is-invalid');
        errorDiv.textContent = '';
        errorDiv.style.display = 'none';
        errorDiv.classList.add('d-none');
    }

    /**
     * Clear all errors
     */
    function clearAllErrors() {
        const errorDivs = document.querySelectorAll('.invalid-feedback');
        const inputs = document.querySelectorAll('.is-invalid');
        
        errorDivs.forEach(div => {
            div.textContent = '';
            div.style.display = 'none';
            div.classList.add('d-none');
        });
        
        inputs.forEach(input => {
            input.classList.remove('is-invalid');
        });

        // Hide general error
        const generalError = document.getElementById('generalError');
        if (generalError) {
            generalError.classList.add('d-none');
            generalError.textContent = '';
        }
    }

    /**
     * Show general error message
     */
    function showGeneralError(message) {
        const generalError = document.getElementById('generalError');
        if (generalError) {
            generalError.textContent = message;
            generalError.classList.remove('d-none');
            generalError.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }

    /**
     * Validate all fields before form submission
     */
    function validateAllFields() {
        let isValid = true;

        // Validate bio
        const bioInput = document.getElementById('bioInput');
        const bioError = document.getElementById('bioError');
        if (bioInput && bioError) {
            if (!validateTextField(bioInput, bioError, VALIDATION_RULES.bio)) {
                isValid = false;
            }
        }

        // Validate place of birth
        const placeInput = document.getElementById('placeOfBirthInput');
        const placeError = document.getElementById('placeOfBirthError');
        if (placeInput && placeError) {
            if (!validateTextField(placeInput, placeError, VALIDATION_RULES.placeOfBirth)) {
                isValid = false;
            }
        }

        // Validate interests
        const interestsInput = document.getElementById('interestsInput');
        const interestsError = document.getElementById('interestsError');
        if (interestsInput && interestsError) {
            if (!validateTextField(interestsInput, interestsError, VALIDATION_RULES.interests)) {
                isValid = false;
            }
        }

        // Check validation state for files
        if (!validationState.avatar || !validationState.cover) {
            isValid = false;
        }

        return isValid;
    }

    /**
     * Handle form submission
     */
    function handleFormSubmit(e) {
        e.preventDefault();
        
        // Clear previous errors
        clearAllErrors();

        // Validate all fields
        if (!validateAllFields()) {
            showGeneralError('Vui lòng kiểm tra lại các thông tin đã nhập');
            return false;
        }

        // If validation passes, proceed with form submission
        // This will be handled by the actual form submission logic
        console.log('Validation passed, ready to submit');
        
        // Trigger the actual save logic (to be implemented in profileManager.js)
        if (typeof window.saveProfileChanges === 'function') {
            window.saveProfileChanges();
        }
    }

    /**
     * Public API
     */
    window.ProfileValidation = {
        validate: validateAllFields,
        clearErrors: clearAllErrors,
        showError: showGeneralError,
        isValid: function() {
            return Object.values(validationState).every(v => v === true);
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initValidation);
    } else {
        initValidation();
    }

})();
