/**
 * Profile Manager - Profile Management JavaScript
 * Handles profile editing, image uploads, and friendship operations
 * Requirements: 3.1, 3.7, 4.1, 4.4, 4.5, 5.2, 5.4, 5.5, 5.6, 7.2, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10, 7.11, 8.7, 8.10, 8.11
 */

(function() {
    'use strict';

    // ============================================
    // 14.1 Open Edit Profile Modal
    // Requirements: 7.2
    // ============================================

    /**
     * Open the edit profile modal and load current values
     */
    window.openEditProfileModal = function() {
        const modal = document.getElementById('editProfileModal');
        if (!modal) {
            console.error('Edit profile modal not found');
            return;
        }

        // Open modal using Bootstrap 5 Modal API
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Clear any previous errors when opening
        if (window.ProfileValidation) {
            window.ProfileValidation.clearErrors();
        }

        // Load current values into form fields (already populated by Razor)
        // But we can refresh character counts
        updateCharacterCounts();
    };

    /**
     * Close the edit profile modal
     */
    function closeEditProfileModal() {
        const modal = document.getElementById('editProfileModal');
        if (!modal) return;

        const bsModal = bootstrap.Modal.getInstance(modal);
        if (bsModal) {
            bsModal.hide();
        }
    }

    /**
     * Update character counts for text fields
     */
    function updateCharacterCounts() {
        const bioInput = document.getElementById('bioInput');
        const bioCharCount = document.getElementById('bioCharCount');
        if (bioInput && bioCharCount) {
            bioCharCount.textContent = bioInput.value.length;
        }

        const interestsInput = document.getElementById('interestsInput');
        const interestsCharCount = document.getElementById('interestsCharCount');
        if (interestsInput && interestsCharCount) {
            interestsCharCount.textContent = interestsInput.value.length;
        }
    }

    // ============================================
    // 14.2 Save Profile Changes
    // Requirements: 8.7, 8.10, 8.11
    // ============================================

    /**
     * Save profile changes (called by validation module)
     */
    window.saveProfileChanges = async function() {
        const form = document.getElementById('editProfileForm');
        if (!form) {
            console.error('Edit profile form not found');
            return;
        }

        // Validate before submitting
        if (window.ProfileValidation && !window.ProfileValidation.validate()) {
            window.ProfileValidation.showError('Vui lòng kiểm tra lại các thông tin đã nhập');
            return;
        }

        // Get date of birth value
        const dobValue = document.getElementById('dateOfBirthInput')?.value;
        
        // Collect form data as JSON
        const profileData = {
            firstName: document.getElementById('firstNameInput')?.value || '',
            lastName: document.getElementById('lastNameInput')?.value || '',
            bio: document.getElementById('bioInput')?.value || '',
            location: document.getElementById('locationInput')?.value || '',
            gender: document.querySelector('input[name="gender"]:checked')?.value || '',
            dateOfBirth: dobValue && dobValue.trim() !== '' ? dobValue : null,
            faculty: document.getElementById('facultySelect')?.value || '',
            academicYear: document.getElementById('academicYearSelect')?.value || '',
            interests: document.getElementById('interestsInput')?.value || ''
        };

        // Show loading state
        const saveBtn = document.getElementById('saveProfileBtn');
        const originalBtnText = saveBtn?.innerHTML;
        if (saveBtn) {
            saveBtn.disabled = true;
            saveBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang lưu...';
        }

        try {
            // Send Ajax POST request to /Profile/UpdateProfile
            const response = await fetch('/Profile/UpdateProfile', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(profileData)
            });

            const result = await response.json();

            // Handle success: close modal, refresh page
            if (result.success) {
                closeEditProfileModal();
                showToast('Cập nhật thông tin thành công', 'success');
                
                // Reload page to show updated data
                setTimeout(() => location.reload(), 1000);
            } else {
                // Handle error: display error message
                if (window.ProfileValidation) {
                    window.ProfileValidation.showError(result.errorMessage || 'Có lỗi xảy ra khi cập nhật thông tin');
                } else {
                    alert(result.errorMessage || 'Có lỗi xảy ra khi cập nhật thông tin');
                }
            }
        } catch (error) {
            console.error('Error updating profile:', error);
            if (window.ProfileValidation) {
                window.ProfileValidation.showError('Có lỗi xảy ra. Vui lòng thử lại.');
            } else {
                alert('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            // Restore button state
            if (saveBtn && originalBtnText) {
                saveBtn.disabled = false;
                saveBtn.innerHTML = originalBtnText;
            }
        }
    };

    // ============================================
    // 14.3 Upload Avatar
    // Requirements: 7.4, 7.5, 7.6, 7.7, 7.11
    // ============================================

    /**
     * Upload avatar image
     */
    window.uploadAvatar = async function() {
        // Try both input IDs (modal and profile page)
        const fileInput = document.getElementById('avatarInput') || document.getElementById('avatarFileInput');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            showToast('Vui lòng chọn một file ảnh', 'error');
            return;
        }

        const file = fileInput.files[0];

        // Validate file client-side
        if (!validateImageFile(file)) {
            return;
        }

        // Prepare FormData
        const formData = new FormData();
        formData.append('file', file);

        // Add anti-forgery token (try to get from form or create one)
        const form = document.getElementById('editProfileForm');
        const token = form?.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }

        try {
            // Send Ajax POST request to /Profile/UpdateAvatar
            const response = await fetch('/Profile/UpdateAvatar', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            // Handle success: update avatar display
            if (result.success) {
                // Update avatar in modal preview (if exists)
                const modalPreview = document.getElementById('avatarPreviewModal');
                if (modalPreview && result.avatarPath) {
                    modalPreview.src = result.avatarPath;
                }

                // Update avatar in profile page
                const profileAvatar = document.getElementById('avatarPreview');
                if (profileAvatar && result.avatarPath) {
                    profileAvatar.src = result.avatarPath;
                }

                showToast('Cập nhật ảnh đại diện thành công', 'success');
                
                // Clear file input
                fileInput.value = '';
                
                // Reload page after 1 second to update all avatars
                setTimeout(() => location.reload(), 1000);
            } else {
                // Handle error: display error message
                showToast(result.errorMessage || 'Không thể tải ảnh lên. Vui lòng thử lại.', 'error');
            }
        } catch (error) {
            console.error('Error uploading avatar:', error);
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        }
    };

    // ============================================
    // 14.4 Upload Cover Image
    // Requirements: 7.8, 7.9, 7.10, 7.11
    // ============================================

    /**
     * Upload cover image
     */
    window.uploadCoverImage = async function() {
        // Try both input IDs (modal and profile page)
        const fileInput = document.getElementById('coverImageInput') || document.getElementById('coverFileInput');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            showToast('Vui lòng chọn một file ảnh', 'error');
            return;
        }

        const file = fileInput.files[0];

        // Validate file client-side
        if (!validateImageFile(file)) {
            return;
        }

        // Prepare FormData
        const formData = new FormData();
        formData.append('file', file);

        // Add anti-forgery token (try to get from form or create one)
        const form = document.getElementById('editProfileForm');
        const token = form?.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }

        try {
            // Send Ajax POST request to /Profile/UpdateCoverImage
            const response = await fetch('/Profile/UpdateCoverImage', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            // Handle success: update cover display
            if (result.success) {
                // Update cover in modal preview (if exists)
                const modalPreview = document.getElementById('coverPreviewModal');
                if (modalPreview && result.coverPath) {
                    modalPreview.src = result.coverPath;
                }

                // Update cover in profile page
                const profileCover = document.getElementById('coverImagePreview');
                if (profileCover && result.coverPath) {
                    profileCover.src = result.coverPath;
                }

                showToast('Cập nhật ảnh bìa thành công', 'success');
                
                // Clear file input
                fileInput.value = '';
                
                // Reload page after 1 second to update all images
                setTimeout(() => location.reload(), 1000);
            } else {
                // Handle error: display error message
                showToast(result.errorMessage || 'Không thể tải ảnh lên. Vui lòng thử lại.', 'error');
            }
        } catch (error) {
            console.error('Error uploading cover image:', error);
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        }
    };

    // ============================================
    // 14.5 Send Friend Request
    // Requirements: 4.1, 4.4, 4.5
    // ============================================

    /**
     * Send friend request
     * @param {number} friendId - The ID of the user to send friend request to
     */
    window.sendFriendRequest = async function(friendId) {
        if (!friendId) {
            console.error('Friend ID is required');
            return;
        }

        try {
            // Send Ajax POST request to /Profile/SendFriendRequest
            const response = await fetch('/Profile/SendFriendRequest', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(friendId)
            });

            const result = await response.json();

            // Handle success: update button to "Đã gửi lời mời", disable button
            if (result.success) {
                updateFriendButton(friendId, 'pending');
                showToast('Đã gửi lời mời kết bạn', 'success');
            } else {
                // Handle error: display error message
                showToast(result.errorMessage || 'Không thể gửi lời mời. Vui lòng thử lại.', 'error');
            }
        } catch (error) {
            console.error('Error sending friend request:', error);
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        }
    };

    // ============================================
    // 14.6 Accept Friend Request
    // Requirements: 5.2, 5.5, 5.6
    // ============================================

    /**
     * Accept friend request
     * @param {number} friendId - The ID of the user who sent the friend request
     */
    window.acceptFriendRequest = async function(friendId) {
        if (!friendId) {
            console.error('Friend ID is required');
            return;
        }

        try {
            // Send Ajax POST request to /Profile/AcceptFriendRequest
            const response = await fetch('/Profile/AcceptFriendRequest', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(friendId)
            });

            const result = await response.json();

            // Handle success: remove request from list, update friend count
            if (result.success) {
                // Remove request from list
                const requestElement = document.querySelector(`[data-friend-request-id="${friendId}"]`);
                if (requestElement) {
                    requestElement.remove();
                }

                // Update friend count
                const friendCountElement = document.querySelector('.profile-tab[data-tab="friends"] span');
                if (friendCountElement && result.friendCount !== undefined) {
                    friendCountElement.textContent = `Bạn bè (${result.friendCount})`;
                }

                showToast('Đã chấp nhận lời mời kết bạn', 'success');
                
                // Optionally reload page to show updated friend list
                setTimeout(() => location.reload(), 1500);
            } else {
                // Handle error: display error message
                showToast(result.errorMessage || 'Không thể chấp nhận lời mời. Vui lòng thử lại.', 'error');
            }
        } catch (error) {
            console.error('Error accepting friend request:', error);
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        }
    };

    // ============================================
    // 14.7 Decline Friend Request
    // Requirements: 5.4, 5.6
    // ============================================

    /**
     * Decline friend request
     * @param {number} friendId - The ID of the user who sent the friend request
     */
    window.declineFriendRequest = async function(friendId) {
        if (!friendId) {
            console.error('Friend ID is required');
            return;
        }

        try {
            // Send Ajax POST request to /Profile/DeclineFriendRequest
            const response = await fetch('/Profile/DeclineFriendRequest', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(friendId)
            });

            const result = await response.json();

            // Handle success: remove request from list
            if (result.success) {
                // Remove request from list
                const requestElement = document.querySelector(`[data-friend-request-id="${friendId}"]`);
                if (requestElement) {
                    requestElement.remove();
                }

                showToast('Đã từ chối lời mời kết bạn', 'success');
            } else {
                // Handle error: display error message
                showToast(result.errorMessage || 'Không thể từ chối lời mời. Vui lòng thử lại.', 'error');
            }
        } catch (error) {
            console.error('Error declining friend request:', error);
            showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
        }
    };

    // ============================================
    // 14.8 Load Friend Suggestions
    // Requirements: 3.1, 3.7
    // ============================================

    /**
     * Load friend suggestions
     */
    window.loadFriendSuggestions = async function() {
        const suggestionsContainer = document.getElementById('friendSuggestionsContainer');
        if (!suggestionsContainer) {
            console.error('Friend suggestions container not found');
            return;
        }

        try {
            // Send Ajax GET request to /Profile/GetFriendSuggestions
            const response = await fetch('/Profile/GetFriendSuggestions', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const suggestions = await response.json();

            // Render suggestions dynamically
            if (suggestions && suggestions.length > 0) {
                suggestionsContainer.innerHTML = '';
                
                suggestions.forEach(friend => {
                    const friendCard = createFriendSuggestionCard(friend);
                    suggestionsContainer.insertAdjacentHTML('beforeend', friendCard);
                });

                // Attach event handlers to "Add Friend" buttons
                const addFriendButtons = suggestionsContainer.querySelectorAll('.btn-add-friend');
                addFriendButtons.forEach(button => {
                    button.addEventListener('click', function() {
                        const friendId = parseInt(this.getAttribute('data-friend-id'));
                        if (friendId) {
                            sendFriendRequest(friendId);
                        }
                    });
                });
            } else {
                suggestionsContainer.innerHTML = '<p class="text-muted text-center">Không có gợi ý kết bạn</p>';
            }
        } catch (error) {
            console.error('Error loading friend suggestions:', error);
            suggestionsContainer.innerHTML = '<p class="text-danger text-center">Không thể tải gợi ý kết bạn</p>';
        }
    };

    /**
     * Create friend suggestion card HTML
     * @param {object} friend - Friend data object
     * @returns {string} HTML string
     */
    function createFriendSuggestionCard(friend) {
        const avatar = friend.avatar || '/assets/user.png';
        const bio = friend.bio ? `<p>${escapeHtml(friend.bio)}</p>` : '';
        
        return `
            <div class="friend-card suggested">
                <img src="${avatar}" alt="${escapeHtml(friend.fullName)}">
                <div class="friend-info">
                    <h4>${escapeHtml(friend.fullName)}</h4>
                    ${bio}
                </div>
                <button class="btn-add-friend" data-friend-id="${friend.userId}">
                    <i class="fa-solid fa-user-plus"></i>
                    Thêm bạn bè
                </button>
            </div>
        `;
    }

    // ============================================
    // 14.9 Helper Functions
    // Requirements: 4.5, 5.6, 7.4, 7.5
    // ============================================

    /**
     * Show toast notification
     * @param {string} message - Message to display
     * @param {string} type - Type of toast (success, error, info, warning)
     */
    function showToast(message, type = 'info') {
        // Check if Bootstrap toast is available
        const toastContainer = document.getElementById('toastContainer');
        
        if (!toastContainer) {
            // Create toast container if it doesn't exist
            const container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }

        // Create toast element
        const toastId = 'toast-' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : type === 'warning' ? 'warning' : 'info'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        ${escapeHtml(message)}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        const container = document.getElementById('toastContainer');
        container.insertAdjacentHTML('beforeend', toastHtml);

        // Show toast
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: 3000
        });
        toast.show();

        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function() {
            toastElement.remove();
        });
    }

    /**
     * Update friend button state
     * @param {number} friendId - Friend user ID
     * @param {string} status - Status (pending, accepted, declined)
     */
    function updateFriendButton(friendId, status) {
        const button = document.querySelector(`[data-friend-id="${friendId}"]`);
        if (!button) return;

        if (status === 'pending') {
            button.textContent = 'Đã gửi lời mời';
            button.disabled = true;
            button.classList.add('btn-secondary');
            button.classList.remove('btn-primary', 'btn-add-friend');
            
            // Update icon
            const icon = button.querySelector('i');
            if (icon) {
                icon.className = 'fa-solid fa-clock';
            }
        } else if (status === 'accepted') {
            button.textContent = 'Bạn bè';
            button.disabled = true;
            button.classList.add('btn-secondary');
            button.classList.remove('btn-primary', 'btn-add-friend');
            
            // Update icon
            const icon = button.querySelector('i');
            if (icon) {
                icon.className = 'fa-solid fa-user-check';
            }
        }
    }

    /**
     * Validate image file (client-side)
     * @param {File} file - File to validate
     * @returns {boolean} True if valid, false otherwise
     */
    function validateImageFile(file) {
        if (!file) {
            showToast('Vui lòng chọn một file ảnh', 'error');
            return false;
        }

        // Check file size (max 5MB)
        const maxSize = 5 * 1024 * 1024; // 5MB in bytes
        if (file.size > maxSize) {
            showToast('Kích thước file không được vượt quá 5MB', 'error');
            return false;
        }

        // Check file type
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        const allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif'];
        
        const isValidType = allowedTypes.includes(file.type) || allowedExtensions.includes(fileExtension);
        
        if (!isValidType) {
            showToast('Chỉ chấp nhận file ảnh định dạng JPG, PNG, hoặc GIF', 'error');
            return false;
        }

        return true;
    }

    /**
     * Escape HTML to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ============================================
    // Initialization
    // ============================================

    /**
     * Initialize profile manager
     */
    function init() {
        console.log('Profile Manager initialized');

        // Auto-upload avatar when file is selected
        const avatarFileInput = document.getElementById('avatarFileInput');
        if (avatarFileInput) {
            avatarFileInput.addEventListener('change', function() {
                if (this.files && this.files.length > 0) {
                    uploadAvatar();
                }
            });
        }

        // Auto-upload cover when file is selected
        const coverFileInput = document.getElementById('coverFileInput');
        if (coverFileInput) {
            coverFileInput.addEventListener('change', function() {
                if (this.files && this.files.length > 0) {
                    uploadCoverImage();
                }
            });
        }

        // Load friend suggestions if container exists
        const suggestionsContainer = document.getElementById('friendSuggestionsContainer');
        if (suggestionsContainer) {
            loadFriendSuggestions();
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Export public API
    window.ProfileManager = {
        openEditProfileModal: window.openEditProfileModal,
        saveProfileChanges: window.saveProfileChanges,
        uploadAvatar: window.uploadAvatar,
        uploadCoverImage: window.uploadCoverImage,
        sendFriendRequest: window.sendFriendRequest,
        acceptFriendRequest: window.acceptFriendRequest,
        declineFriendRequest: window.declineFriendRequest,
        loadFriendSuggestions: window.loadFriendSuggestions,
        showToast: showToast,
        updateFriendButton: updateFriendButton,
        validateImageFile: validateImageFile
    };

})();
