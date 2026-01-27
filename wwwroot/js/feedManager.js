(function() {
    'use strict';

    let currentPostIndex = 0;
    const postsPerLoad = 3;
    let isLoading = false;
    let hasMorePosts = false;
    let observer = null;

    function initPostComposer() {
        const postButton = document.querySelector('.feed-composer .feed-btn-primary');
        const postInput = document.querySelector('.feed-composer-input');
        
        if (!postButton || !postInput) {
            return;
        }

        postButton.addEventListener('click', handlePostSubmit);
        
        postInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handlePostSubmit();
            }
        });
    }

    function handlePostSubmit() {
        const postInput = document.querySelector('.feed-composer-input');
        const imagePreview = document.querySelector('#image-preview-img');
        
        if (!postInput) return;
        
        const content = postInput.value.trim();
        const imageUrl = imagePreview && imagePreview.src && !imagePreview.src.includes('data:') ? imagePreview.src : null;
        
        if (!content && !imageUrl) {
            showToast('Vui lòng nhập nội dung hoặc chọn ảnh', 'warning');
            return;
        }
        
        const newPost = {
            id: Date.now(),
            author: 'MTU User',
            authorAvatar: '/assets/avt.jpg',
            timestamp: 'Vừa xong',
            content: content,
            imageUrl: imageUrl,
            likes: 0,
            comments: 0,
            shares: 0
        };
        
        insertNewPost(newPost);
        
        postInput.value = '';
        
        if (imagePreview) {
            const previewContainer = document.querySelector('#image-preview-container');
            if (previewContainer) {
                previewContainer.style.display = 'none';
                imagePreview.src = '';
            }
        }
        
        showToast('Đăng bài thành công! 🎉', 'success');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function insertNewPost(post) {
        const feedList = document.querySelector('.feed-list');
        if (!feedList) return;
        
        const postElement = createPostElement(post);
        postElement.classList.add('new-post-animation');
        feedList.insertBefore(postElement, feedList.firstChild);
        
        setTimeout(() => {
            postElement.classList.remove('new-post-animation');
        }, 600);
        
        if (window.InteractionsManager) {
            window.InteractionsManager.initializeLikeButtons();
            window.InteractionsManager.initializeCommentButtons();
        }
    }

    function showToast(message, type = 'info') {
        const existingToast = document.querySelector('.toast-notification');
        if (existingToast) {
            existingToast.remove();
        }
        
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="toast-icon fas ${getToastIcon(type)}"></i>
                <span class="toast-message">${message}</span>
            </div>
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('show');
        }, 10);
        
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                toast.remove();
            }, 300);
        }, 3000);
    }

    function getToastIcon(type) {
        const icons = {
            success: 'fa-check-circle',
            error: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };
        return icons[type] || icons.info;
    }

    function createPostElement(post) {
        const article = document.createElement('article');
        article.className = 'post-card feed-card';

        const header = document.createElement('header');
        header.className = 'post-header';
        header.innerHTML = `
            <div class="post-author">
                <div class="post-avatar" aria-hidden="true">
                    <img src="${post.authorAvatar || '/assets/avt.jpg'}" alt="avatar" onerror="this.src='/assets/avt.jpg'">
                </div>
                <div class="post-authorBody">
                    <div class="post-authorName">${escapeHtml(post.author)}</div>
                    <div class="post-meta">${escapeHtml(post.timestamp)}</div>
                </div>
            </div>
            <button class="post-more" type="button" aria-label="Tùy chọn">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
        `;

        const body = document.createElement('div');
        body.className = 'post-body';
        
        let bodyHTML = `<div class="post-content">${escapeHtml(post.content)}</div>`;
        
        if (post.imageUrl) {
            bodyHTML += `
                <div class="post-media">
                    <img class="post-mediaImage" src="${post.imageUrl}" alt="Post image" />
                </div>
            `;
        }
        
        body.innerHTML = bodyHTML;

        const stats = document.createElement('div');
        stats.className = 'post-stats';
        stats.innerHTML = `
            <div class="post-likes">
                <i class="post-likeIcon fa-solid fa-thumbs-up"></i>
                <span class="post-likeCount">${post.likes}</span>
            </div>
            <div class="post-counts">
                <span class="post-countItem">${post.comments} bình luận</span>
                <span class="post-countItem">${post.shares} chia sẻ</span>
            </div>
        `;

        const actions = document.createElement('div');
        actions.className = 'post-actions';
        actions.innerHTML = `
            <button class="post-actionBtn post-action-like" type="button">
                <i class="post-actionIcon fa-regular fa-thumbs-up"></i>
                <span class="post-actionText">Thích</span>
            </button>
            <button class="post-actionBtn post-action-comment" type="button">
                <i class="post-actionIcon fa-regular fa-comment"></i>
                <span class="post-actionText">Bình luận</span>
            </button>
            <button class="post-actionBtn post-action-share" type="button">
                <i class="post-actionIcon fa-solid fa-share"></i>
                <span class="post-actionText">Chia sẻ</span>
            </button>
        `;

        const commentsExpandable = document.createElement('details');
        commentsExpandable.className = 'post-comments-expandable';
        commentsExpandable.innerHTML = `
            <summary class="post-comments-toggle" style="display: none;">Toggle Comments</summary>
            <div class="post-commentsPanel">
                <div class="post-commentRow">
                    <div class="post-commentAvatar" aria-hidden="true"><i class="fa-solid fa-user"></i></div>
                    <div class="post-commentBox">
                        <input class="post-commentInput" type="text" placeholder="Viết bình luận..." />
                        <button class="post-commentSend" type="button" aria-label="Gửi"><i class="fa-solid fa-paper-plane"></i></button>
                    </div>
                </div>
                <div class="post-commentList"></div>
            </div>
        `;

        article.appendChild(header);
        article.appendChild(body);
        article.appendChild(stats);
        article.appendChild(actions);
        article.appendChild(commentsExpandable);

        return article;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', attachEventListeners);
        } else {
            attachEventListeners();
        }
    }

    function attachEventListeners() {
        initPostComposer();
    }

    window.FeedManager = {
        init: init,
        showToast: showToast
    };

    init();

})();
