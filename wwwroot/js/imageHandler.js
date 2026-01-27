/**
 * Image Handler Module
 * Handles image upload, resize, and compression for the feed composer
 */

(function() {
    'use strict';

    // Configuration constants
    const MAX_FILE_SIZE = 2 * 1024 * 1024; // 2MB in bytes
    const MAX_IMAGE_WIDTH = 1920; // Maximum width in pixels
    const COMPRESSION_QUALITY = 0.85; // JPEG compression quality (0-1)
    
    // Flag to prevent double initialization
    let isInitialized = false;

    /**
     * Initialize image upload functionality
     * Sets up event listeners for file input
     */
    function initImageUpload() {
        // Prevent double initialization
        if (isInitialized) {
            console.log('Image upload already initialized');
            return;
        }
        
        // Check if Index.cshtml already handles image upload
        const existingInput = document.getElementById('post-image-input');
        if (existingInput) {
            console.log('Image upload handled by Index.cshtml, skipping imageHandler.js init');
            isInitialized = true;
            return;
        }
        
        // Get the image button in feed composer
        const imageButton = document.querySelector('.feed-composer-actions .feed-btn:first-child');
        
        if (!imageButton) {
            console.warn('Image upload button not found');
            return;
        }

        // Check if file input already exists
        let fileInput = document.getElementById('image-upload-input');
        if (!fileInput) {
            // Create hidden file input
            fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.accept = 'image/*';
            fileInput.style.display = 'none';
            fileInput.id = 'image-upload-input';
            document.body.appendChild(fileInput);
            
            // Add event listener for file selection
            fileInput.addEventListener('change', handleFileSelection);
        }

        // Remove existing click handlers to prevent duplicates
        const newButton = imageButton.cloneNode(true);
        imageButton.parentNode.replaceChild(newButton, imageButton);
        
        // Attach click handler to image button
        newButton.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            fileInput.click();
        });

        isInitialized = true;
        console.log('Image upload initialized');
    }

    /**
     * Handle file selection event
     * Validates and processes the selected image file
     * 
     * @param {Event} event - File input change event
     * 
     * TODO: Add validation for file type (only images)
     * TODO: Handle multiple file selection
     */
    function handleFileSelection(event) {
        const file = event.target.files[0];
        
        if (!file) {
            return;
        }

        console.log('File selected:', file.name, 'Size:', formatFileSize(file.size));

        // TODO: Validate file type
        if (!file.type.startsWith('image/')) {
            alert('Please select an image file');
            return;
        }

        // Check if image needs compression
        checkAndProcessImage(file);
    }

    /**
     * Check if image needs compression and process accordingly
     * 
     * @param {File} file - The image file to check
     * 
     * TODO: Add more sophisticated size checking (dimensions + file size)
     * TODO: Handle different image formats (PNG, WebP, etc.)
     * TODO: Add user preference for compression threshold
     */
    function checkAndProcessImage(file) {
        const needsCompression = checkImageSize(file);

        if (needsCompression) {
            console.log('Image needs compression');
            // TODO: Show loading indicator
            compressImage(file);
        } else {
            // Check image dimensions
            checkImageDimensions(file);
        }
    }

    /**
     * Check if image size exceeds threshold
     * 
     * @param {File} file - The image file to check
     * @returns {boolean} True if image needs compression
     * 
     * TODO: Add configurable size threshold
     * TODO: Consider both file size and dimensions
     */
    function checkImageSize(file) {
        const exceedsSize = file.size > MAX_FILE_SIZE;
        
        if (exceedsSize) {
            console.log('File size exceeds maximum:', formatFileSize(file.size), '>', formatFileSize(MAX_FILE_SIZE));
        }
        
        return exceedsSize;
    }

    /**
     * Check image dimensions and resize if necessary
     * 
     * @param {File} file - The image file to check
     * 
     * TODO: Implement dimension checking logic
     * TODO: Resize image if width > MAX_IMAGE_WIDTH
     */
    function checkImageDimensions(file) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            const img = new Image();
            
            img.onload = function() {
                console.log('Image dimensions:', img.width, 'x', img.height);
                
                if (img.width > MAX_IMAGE_WIDTH) {
                    console.log('Image width exceeds maximum, resizing...');
                    // TODO: Call resize function
                    resizeAndCompressImage(file, img.width, img.height);
                } else {
                    // Image is fine, show preview
                    showImagePreview(file, file.size, file.size, false);
                }
            };
            
            img.src = e.target.result;
        };
        
        reader.readAsDataURL(file);
    }

    /**
     * Compress image using HTML5 Canvas
     * Reduces file size while maintaining dimensions
     * 
     * @param {File} file - The image file to compress
     * 
     * TODO: Add support for PNG format with transparency
     * TODO: Allow user to adjust compression quality
     * TODO: Add error handling for canvas operations
     */
    function compressImage(file) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            const img = new Image();
            
            img.onload = function() {
                // Create canvas with original dimensions
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                
                canvas.width = img.width;
                canvas.height = img.height;
                
                // TODO: Implement smart compression algorithm
                // For now, use standard quality reduction
                
                // Draw image on canvas
                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                
                // Convert canvas to blob with compression
                // TODO: Detect original format and use appropriate MIME type
                canvas.toBlob(function(blob) {
                    if (blob) {
                        console.log('Compression complete');
                        console.log('Original size:', formatFileSize(file.size));
                        console.log('Compressed size:', formatFileSize(blob.size));
                        
                        // Show preview with compression info
                        showImagePreview(blob, file.size, blob.size, true);
                    } else {
                        console.error('Failed to compress image');
                        // Fallback to original
                        showImagePreview(file, file.size, file.size, false);
                    }
                }, 'image/jpeg', COMPRESSION_QUALITY);
            };
            
            img.onerror = function() {
                console.error('Failed to load image for compression');
                showImagePreview(file, file.size, file.size, false);
            };
            
            img.src = e.target.result;
        };
        
        reader.onerror = function() {
            console.error('Failed to read file');
            showImagePreview(file, file.size, file.size, false);
        };
        
        reader.readAsDataURL(file);
    }

    /**
     * Resize and compress image using HTML5 Canvas
     * Reduces both dimensions and file size
     * 
     * @param {File} file - The image file
     * @param {number} originalWidth - Original image width
     * @param {number} originalHeight - Original image height
     * 
     * TODO: Add option to preserve aspect ratio with different target dimensions
     * TODO: Implement bicubic interpolation for better quality
     * TODO: Add support for different output formats
     */
    function resizeAndCompressImage(file, originalWidth, originalHeight) {
        const reader = new FileReader();
        
        reader.onload = function(e) {
            const img = new Image();
            
            img.onload = function() {
                // Calculate new dimensions maintaining aspect ratio
                // TODO: Make this configurable based on user preferences
                const aspectRatio = originalHeight / originalWidth;
                const newWidth = MAX_IMAGE_WIDTH;
                const newHeight = Math.round(newWidth * aspectRatio);
                
                console.log('Resizing from', originalWidth, 'x', originalHeight, 'to', newWidth, 'x', newHeight);
                
                // Create canvas with new dimensions
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                
                canvas.width = newWidth;
                canvas.height = newHeight;
                
                // TODO: Implement multi-step downscaling for better quality
                // For large reductions, step-down approach produces better results
                // Example: 4000px -> 2000px -> 1920px instead of 4000px -> 1920px
                
                // Enable image smoothing for better quality
                ctx.imageSmoothingEnabled = true;
                ctx.imageSmoothingQuality = 'high';
                
                // Draw resized image on canvas
                ctx.drawImage(img, 0, 0, newWidth, newHeight);
                
                // Convert canvas to blob with compression
                // TODO: Adjust compression quality based on resize ratio
                canvas.toBlob(function(blob) {
                    if (blob) {
                        console.log('Resize and compression complete');
                        console.log('Original size:', formatFileSize(file.size));
                        console.log('Compressed size:', formatFileSize(blob.size));
                        console.log('Size reduction:', Math.round((1 - blob.size / file.size) * 100) + '%');
                        
                        // Show preview with compression info
                        showImagePreview(blob, file.size, blob.size, true);
                    } else {
                        console.error('Failed to resize and compress image');
                        // Fallback to original
                        showImagePreview(file, file.size, file.size, false);
                    }
                }, 'image/jpeg', COMPRESSION_QUALITY);
            };
            
            img.onerror = function() {
                console.error('Failed to load image for resizing');
                showImagePreview(file, file.size, file.size, false);
            };
            
            img.src = e.target.result;
        };
        
        reader.onerror = function() {
            console.error('Failed to read file');
            showImagePreview(file, file.size, file.size, false);
        };
        
        reader.readAsDataURL(file);
    }

    /**
     * Show image preview in the feed composer
     * Displays the selected/compressed image with size information
     * 
     * @param {File|Blob} imageData - The image data to preview
     * @param {number} originalSize - Original file size in bytes
     * @param {number} compressedSize - Compressed file size in bytes
     * @param {boolean} wasCompressed - Whether the image was compressed
     * 
     * TODO: Add loading state while image is being processed
     * TODO: Add support for multiple image previews
     * TODO: Store image data for actual upload
     */
    function showImagePreview(imageData, originalSize, compressedSize, wasCompressed) {
        console.log('Showing image preview');
        console.log('Original size:', formatFileSize(originalSize));
        console.log('Compressed size:', formatFileSize(compressedSize));
        console.log('Was compressed:', wasCompressed);
        
        // Get preview elements
        const previewContainer = document.getElementById('image-preview-container');
        const previewImg = document.getElementById('image-preview-img');
        const compressionBadge = document.getElementById('compression-badge');
        const compressionBadgeText = document.getElementById('compression-badge-text');
        const previewInfoText = document.getElementById('image-preview-info-text');
        const removeButton = document.getElementById('image-preview-remove');
        
        if (!previewContainer || !previewImg) {
            console.error('Preview elements not found');
            return;
        }
        
        // Create object URL for preview
        const imageUrl = URL.createObjectURL(imageData);
        
        // Set preview image
        previewImg.src = imageUrl;
        
        // Show/hide compression badge
        if (wasCompressed && compressionBadge && compressionBadgeText) {
            const savingsPercent = Math.round((1 - compressedSize / originalSize) * 100);
            compressionBadgeText.textContent = `Compressed ${savingsPercent}%`;
            compressionBadge.style.display = 'block';
        } else if (compressionBadge) {
            compressionBadge.style.display = 'none';
        }
        
        // Set info text
        if (previewInfoText) {
            if (wasCompressed) {
                previewInfoText.innerHTML = `
                    <span class="image-preview-info-highlight">Compressed from ${formatFileSize(originalSize)} to ${formatFileSize(compressedSize)}</span>
                    <br>Image optimized for faster upload and better performance.
                `;
            } else {
                previewInfoText.innerHTML = `
                    Image size: <span class="image-preview-info-highlight">${formatFileSize(originalSize)}</span>
                    <br>Ready to upload.
                `;
            }
        }
        
        // Show preview container
        previewContainer.classList.add('visible');
        
        // TODO: Store image data for actual upload
        // Store in a global variable or data attribute for later use
        previewContainer.dataset.imageData = imageUrl;
        
        // Attach remove button handler
        if (removeButton) {
            removeButton.onclick = function() {
                removeImagePreview();
            };
        }
        
        console.log('Preview displayed successfully');
    }
    
    /**
     * Remove image preview and reset state
     * 
     * TODO: Clean up object URLs to prevent memory leaks
     * TODO: Reset file input
     */
    function removeImagePreview() {
        const previewContainer = document.getElementById('image-preview-container');
        const previewImg = document.getElementById('image-preview-img');
        const fileInput = document.getElementById('image-upload-input');
        
        if (previewContainer) {
            // Revoke object URL to free memory
            if (previewContainer.dataset.imageData) {
                URL.revokeObjectURL(previewContainer.dataset.imageData);
                delete previewContainer.dataset.imageData;
            }
            
            // Hide preview
            previewContainer.classList.remove('visible');
        }
        
        if (previewImg) {
            previewImg.src = '';
        }
        
        // Reset file input
        if (fileInput) {
            fileInput.value = '';
        }
        
        console.log('Preview removed');
    }

    /**
     * Format file size for display
     * 
     * @param {number} bytes - File size in bytes
     * @returns {string} Formatted file size (e.g., "2.5 MB")
     */
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initImageUpload);
    } else {
        initImageUpload();
    }

    // TODO: Export functions for testing if needed
    // window.ImageHandler = {
    //     formatFileSize: formatFileSize
    // };

})();
