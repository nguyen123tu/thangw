(function () {
    'use strict';

    console.log('Interactions.js loaded');

    function initializeLikeButtons() {
        const likeButtons = document.querySelectorAll('.post-action-like');
        console.log('Found like buttons:', likeButtons.length);

        likeButtons.forEach(button => {
            button.onclick = handleLikeClick;
        });
    }

    async function handleLikeClick(event) {
        event.preventDefault();
        event.stopPropagation();

        const button = event.currentTarget;
        const postCard = button.closest('.post-card');

        if (!postCard) return;

        const postId = postCard.getAttribute('data-post-id');
        if (!postId) {
            console.error('Post ID not found');
            return;
        }

        if (button.disabled) return;
        button.disabled = true;

        try {
            const response = await fetch('/Post/Like?postId=' + postId, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to toggle like');
            }

            const result = await response.json();

            if (!result.success) {
                throw new Error(result.message || 'Failed to toggle like');
            }

            const icon = button.querySelector('.post-actionIcon');
            const likeCountElement = postCard.querySelector('.post-likeCount');

            if (icon) {
                if (result.isLiked) {
                    icon.classList.remove('fa-regular');
                    icon.classList.add('fa-solid');
                } else {
                    icon.classList.remove('fa-solid');
                    icon.classList.add('fa-regular');
                }
            }

            if (likeCountElement) {
                likeCountElement.textContent = result.likeCount;
            }

            button.setAttribute('data-liked', result.isLiked.toString());

        } catch (error) {
            console.error('Error toggling like:', error);
        } finally {
            button.disabled = false;
        }
    }

    function initializeCommentButtons() {
        const commentButtons = document.querySelectorAll('.post-action-comment');
        console.log('Found comment buttons:', commentButtons.length);

        commentButtons.forEach(button => {
            button.onclick = function (event) {
                event.preventDefault();
                console.log('Comment button clicked');

                const postCard = this.closest('.post-card');
                if (!postCard) {
                    console.error('Post card not found');
                    return;
                }

                const commentsExpandable = postCard.querySelector('.post-comments-expandable');
                if (!commentsExpandable) {
                    console.error('Comments expandable not found');
                    return;
                }

                console.log('Current open state:', commentsExpandable.hasAttribute('open'));

                if (commentsExpandable.hasAttribute('open')) {
                    commentsExpandable.removeAttribute('open');
                } else {
                    commentsExpandable.setAttribute('open', '');

                    const postId = postCard.getAttribute('data-post-id');
                    if (postId) {
                        loadComments(postId, postCard);
                    }

                    setTimeout(() => {
                        const commentInput = commentsExpandable.querySelector('.post-commentInput');
                        if (commentInput) {
                            commentInput.focus();
                        }
                    }, 100);
                }
            };
        });

        const commentSendButtons = document.querySelectorAll('.post-commentSend');
        console.log('Found comment send buttons:', commentSendButtons.length);

        commentSendButtons.forEach(button => {
            button.onclick = handleCommentSend;
        });

        const commentInputs = document.querySelectorAll('.post-commentInput');
        commentInputs.forEach(input => {
            input.onkeypress = function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    const sendButton = this.parentElement.querySelector('.post-commentSend');
                    if (sendButton) {
                        sendButton.click();
                    }
                }
            };
        });
    }

    async function handleCommentSend(event) {
        event.preventDefault();
        event.stopPropagation();

        const button = event.currentTarget;
        const postCard = button.closest('.post-card');

        if (!postCard) return;

        const postId = postCard.getAttribute('data-post-id');
        const commentInput = postCard.querySelector('.post-commentInput');

        if (!postId || !commentInput) {
            console.error('Post ID or comment input not found');
            return;
        }

        const content = commentInput.value.trim();

        if (!content) {
            alert('Vui lòng nhập nội dung bình luận');
            return;
        }

        if (button.disabled) return;
        button.disabled = true;
        commentInput.disabled = true;

        try {
            console.log('Sending comment:', { postId, content });

            const response = await fetch('/Post/Comment', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    postId: parseInt(postId),
                    content: content
                })
            });

            console.log('Response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Response error:', errorText);
                throw new Error('Failed to add comment');
            }

            const result = await response.json();
            console.log('Comment result:', result);

            if (!result.success) {
                throw new Error(result.message || 'Failed to add comment');
            }

            const newComment = result.comment;

            const commentList = postCard.querySelector('.post-commentList');
            if (commentList && newComment) {
                const noCommentsMsg = commentList.querySelector('p');
                if (noCommentsMsg) {
                    noCommentsMsg.remove();
                }

                const commentHtml = createCommentElement(newComment);
                commentList.insertAdjacentHTML('beforeend', commentHtml);
            }

            const commentCountElement = postCard.querySelector('.post-commentCount');
            if (commentCountElement) {
                const currentCount = parseInt(commentCountElement.textContent) || 0;
                commentCountElement.textContent = currentCount + 1;
            }

            commentInput.value = '';

        } catch (error) {
            console.error('Error adding comment:', error);
            alert('Không thể thêm bình luận. Vui lòng thử lại.');
        } finally {
            button.disabled = false;
            commentInput.disabled = false;
            commentInput.focus();
        }
    }

    async function loadComments(postId, postCard) {
        const commentList = postCard.querySelector('.post-commentList');
        if (!commentList) return;

        if (commentList.getAttribute('data-loaded') === 'true') {
            return;
        }

        try {
            const response = await fetch('/Post/GetComments?postId=' + postId);

            if (!response.ok) {
                throw new Error('Failed to load comments');
            }

            const result = await response.json();
            const comments = result.comments || result;

            commentList.innerHTML = '';

            if (!comments || comments.length === 0) {
                commentList.innerHTML = '<p style="text-align: center; color: var(--lofi-muted); padding: 16px;">Chưa có bình luận nào</p>';
            } else {
                comments.forEach(comment => {
                    const commentHtml = createCommentElement(comment);
                    commentList.insertAdjacentHTML('beforeend', commentHtml);
                });
            }

            commentList.setAttribute('data-loaded', 'true');

        } catch (error) {
            console.error('Error loading comments:', error);
            commentList.innerHTML = '<p style="text-align: center; color: #e74c3c; padding: 16px;">Không thể tải bình luận</p>';
        }
    }

    function createCommentElement(comment) {
        return '<div class="post-commentItem">' +
            '<div class="post-commentAvatarXs" aria-hidden="true">' +
            '<img src="' + escapeHtml(comment.authorAvatar) + '" alt="' + escapeHtml(comment.authorName) + '" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;">' +
            '</div>' +
            '<div class="post-commentBubble">' +
            '<div class="post-commentName">' + escapeHtml(comment.authorName) + '</div>' +
            '<div class="post-commentText">' + escapeHtml(comment.content) + '</div>' +
            '<div class="post-commentMeta" style="font-size: 11px; color: var(--lofi-muted); margin-top: 4px;">' + escapeHtml(comment.timeAgo) + '</div>' +
            '</div>' +
            '</div>';
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Toggle Post Menu
    window.togglePostMenu = function (btn) {
        event.stopPropagation();
        const dropdown = btn.nextElementSibling;

        // Close all other dropdowns
        document.querySelectorAll('.post-menu-dropdown').forEach(d => {
            if (d !== dropdown) d.classList.remove('show');
        });

        dropdown.classList.toggle('show');
    };

    // Close menus when clicking outside
    document.addEventListener('click', function (event) {
        if (!event.target.closest('.post-menu-dropdown') && !event.target.closest('.post-more')) {
            document.querySelectorAll('.post-menu-dropdown').forEach(d => {
                d.classList.remove('show');
            });
        }
    });

    // Update Privacy
    window.updatePrivacy = function (postId, privacy) {
        const formData = new FormData();
        formData.append('postId', postId);
        formData.append('privacy', privacy);

        fetch('/Post/UpdatePrivacy', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    alert(data.message);
                    // Close menu
                    document.querySelectorAll('.post-menu-dropdown').forEach(d => d.classList.remove('show'));
                } else {
                    alert(data.message);
                }
            })
            .catch(error => console.error('Error:', error));
    };

    // Placeholder for other actions
    window.deletePost = function (postId) {
        if (confirm('Bạn có chắc chắn muốn xóa bài viết này không?')) {
            fetch(`/Post/Delete?id=${postId}`, {
                method: 'POST'
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        const postCard = document.querySelector(`.post-card[data-post-id="${postId}"]`);
                        if (postCard) {
                            postCard.remove();
                        }
                        alert(data.message);
                    } else {
                        alert(data.message);
                    }
                })
                .catch(error => console.error('Error:', error));
        }
    };

    window.editPost = function (postId) {
        alert('Tính năng chỉnh sửa đang phát triển');
    };

    function initialize() {
        console.log('Initializing interactions...');
        initializeLikeButtons();
        initializeCommentButtons();
        console.log('Interactions initialized');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    window.InteractionsManager = {
        reinitialize: initialize
    };

})();
