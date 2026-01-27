/* ============================================
   SITE.JS - General Site Utilities
   MTU Social Network
   ============================================
   
   This file contains general-purpose utilities
   and initialization code for the site.
   
   NOTE: Chat functionality has been moved to chatManager.js
   ============================================ */

(function() {
    'use strict';

    /**
     * Initialize site-wide functionality
     */
    function init() {
        console.log('MTU Social Network initialized');
        
        // Initialize image preview handlers
        initImagePreviewHandlers();
        
        // Initialize post menu handlers
        initPostMenuHandlers();
        
        // Add any general site initialization here
    }

    /**
     * Initialize image preview handlers for avatar and cover image uploads
     */
    function initImagePreviewHandlers() {
        // Avatar file input handler
        const avatarFileInput = document.getElementById('avatarFileInput');
        if (avatarFileInput) {
            avatarFileInput.addEventListener('change', function(e) {
                handleImagePreview(e.target, 'avatarPreviewModal', 'avatarError');
            });
        }

        // Cover file input handler
        const coverFileInput = document.getElementById('coverFileInput');
        if (coverFileInput) {
            coverFileInput.addEventListener('change', function(e) {
                handleImagePreview(e.target, 'coverPreviewModal', 'coverError');
            });
        }
    }

    /**
     * Initialize post menu handlers - close menu when clicking outside
     */
    function initPostMenuHandlers() {
        document.addEventListener('click', function(e) {
            // Close all open menus if clicking outside
            if (!e.target.closest('.post-more') && !e.target.closest('.post-menu-dropdown')) {
                document.querySelectorAll('.post-menu-dropdown.show').forEach(menu => {
                    menu.classList.remove('show');
                });
            }
        });
    }

    /**
     * Handle image preview when file is selected
     * @param {HTMLInputElement} input - The file input element
     * @param {string} previewId - ID of the preview image element
     * @param {string} errorId - ID of the error message element
     */
    function handleImagePreview(input, previewId, errorId) {
        const file = input.files[0];
        const previewImg = document.getElementById(previewId);
        const errorDiv = document.getElementById(errorId);

        // Clear previous error
        if (errorDiv) {
            errorDiv.classList.add('d-none');
            errorDiv.textContent = '';
        }

        if (!file) {
            return;
        }

        // Validate file type
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        if (!allowedTypes.includes(file.type)) {
            showError(errorId, 'Định dạng file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF.');
            input.value = '';
            return;
        }

        // Validate file size (5MB max)
        const maxSize = 5 * 1024 * 1024; // 5MB in bytes
        if (file.size > maxSize) {
            showError(errorId, 'Kích thước file quá lớn. Tối đa 5MB.');
            input.value = '';
            return;
        }

        // Show preview
        const reader = new FileReader();
        reader.onload = function(e) {
            if (previewImg) {
                previewImg.src = e.target.result;
            }
        };
        reader.readAsDataURL(file);
    }

    /**
     * Show error message
     * @param {string} errorId - ID of the error element
     * @param {string} message - Error message to display
     */
    function showError(errorId, message) {
        const errorDiv = document.getElementById(errorId);
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.classList.remove('d-none');
            errorDiv.style.display = 'block';
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();

/**
 * Toggle post menu dropdown
 * @param {HTMLElement} btn - The button element
 */
function togglePostMenu(btn) {
    const dropdown = btn.nextElementSibling;
    
    // Close all other open menus
    document.querySelectorAll('.post-menu-dropdown.show').forEach(menu => {
        if (menu !== dropdown) {
            menu.classList.remove('show');
        }
    });
    
    // Toggle current menu
    dropdown.classList.toggle('show');
}

/**
 * Delete a post
 * @param {number} postId - The post ID to delete
 */
async function deletePost(postId) {
    if (!confirm('Bạn có chắc chắn muốn xóa bài viết này?')) {
        return;
    }
    
    try {
        const response = await fetch(`/Post/Delete/${postId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Remove post from DOM with animation
            const postCard = document.querySelector(`[data-post-id="${postId}"]`);
            if (postCard) {
                postCard.style.transition = 'all 0.3s ease';
                postCard.style.opacity = '0';
                postCard.style.transform = 'translateX(-100%)';
                setTimeout(() => {
                    postCard.remove();
                }, 300);
            }
            
            // Play success sound if available
            if (window.SoundEffects) {
                window.SoundEffects.play('success');
            }
        } else {
            alert(result.message || 'Không thể xóa bài viết');
        }
    } catch (error) {
        console.error('Error deleting post:', error);
        alert('Đã xảy ra lỗi khi xóa bài viết');
    }
}

/**
 * Edit a post (placeholder)
 * @param {number} postId - The post ID to edit
 */
function editPost(postId) {
    alert('Tính năng chỉnh sửa bài viết đang được phát triển');
}

