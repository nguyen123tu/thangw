(function() {
    'use strict';

    let activeChatWindows = [];
    const maxChatWindows = 3;
    let pollingIntervals = {};

    function init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', attachEventListeners);
        } else {
            attachEventListeners();
        }
    }

    function attachEventListeners() {
        // Dùng event delegation vì contact-item được tạo động
        document.addEventListener('click', function(event) {
            const contactItem = event.target.closest('.contact-item');
            if (contactItem && !event.target.closest('.mini-chat-window')) {
                handleContactClick({ currentTarget: contactItem });
            }
        });
    }

    function handleContactClick(event) {
        const contactItem = event.currentTarget;
        const userId = contactItem.getAttribute('data-user-id');
        const userName = contactItem.querySelector('.contact-name')?.textContent || 'User';
        const userAvatar = contactItem.querySelector('.contact-avatar img')?.src || '/assets/user.png';

        console.log('Chat clicked:', userId, userName);
        
        if (userId) {
            openChatWindow(userId, userName, userAvatar);
        }
    }

    function openChatWindow(userId, userName, userAvatar) {
        const existingWindow = document.querySelector('.chat-box[data-chat-id="' + userId + '"]');
        if (existingWindow) {
            existingWindow.classList.add('chat-focus');
            setTimeout(function() {
                existingWindow.classList.remove('chat-focus');
            }, 300);
            return;
        }

        if (activeChatWindows.length >= maxChatWindows) {
            var oldestWindow = activeChatWindows.shift();
            var oldId = oldestWindow.getAttribute('data-chat-id');
            if (pollingIntervals[oldId]) {
                clearInterval(pollingIntervals[oldId]);
                delete pollingIntervals[oldId];
            }
            oldestWindow.remove();
        }

        var chatWindow = createChatWindow(userId, userName, userAvatar);
        document.body.appendChild(chatWindow);
        activeChatWindows.push(chatWindow);

        var rightOffset = 90 + (activeChatWindows.length - 1) * 320;
        chatWindow.style.right = rightOffset + 'px';

        attachChatWindowListeners(chatWindow, userId, userName);

        setTimeout(function() {
            chatWindow.classList.add('chat-show');
        }, 10);

        loadMessages(userId, chatWindow);
        startPolling(userId, chatWindow);
    }

    function createChatWindow(userId, userName, userAvatar) {
        var chatWindow = document.createElement('div');
        chatWindow.className = 'chat-box';
        chatWindow.setAttribute('data-chat-id', userId);
        chatWindow.innerHTML = 
            '<div class="chat-header">' +
                '<img src="' + escapeHtml(userAvatar) + '" alt="' + escapeHtml(userName) + '" class="chat-user-avatar">' +
                '<div class="chat-user-info">' +
                    '<span class="chat-user-name">' + escapeHtml(userName) + '</span>' +
                    '<span class="chat-user-status">Đang hoạt động</span>' +
                '</div>' +
                '<button class="chat-close-btn" type="button" aria-label="Đóng">&times;</button>' +
            '</div>' +
            '<div class="chat-messages"></div>' +
            '<div class="chat-input-area">' +
                '<input type="text" class="chat-input" placeholder="Nhập tin nhắn..." />' +
                '<button class="chat-send-btn" type="button" aria-label="Gửi">' +
                    '<i class="fa-solid fa-paper-plane"></i>' +
                '</button>' +
            '</div>';
        return chatWindow;
    }

    function attachChatWindowListeners(chatWindow, userId, userName) {
        var closeBtn = chatWindow.querySelector('.chat-close-btn');
        if (closeBtn) {
            closeBtn.onclick = function() {
                closeChatWindow(chatWindow, userId);
            };
        }

        var sendBtn = chatWindow.querySelector('.chat-send-btn');
        var input = chatWindow.querySelector('.chat-input');

        if (sendBtn && input) {
            sendBtn.onclick = function() {
                sendMessage(chatWindow, input, userId);
            };

            input.onkeypress = function(e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    sendMessage(chatWindow, input, userId);
                }
            };
        }
    }

    async function loadMessages(userId, chatWindow) {
        var messagesContainer = chatWindow.querySelector('.chat-messages');
        if (!messagesContainer) return;

        messagesContainer.innerHTML = '<div class="chat-loading" style="text-align: center; padding: 20px; color: var(--lofi-muted);"><i class="fa-solid fa-spinner fa-spin"></i> Đang tải...</div>';

        try {
            var response = await fetch('/Message/GetMessages?partnerId=' + userId);
            var data = await response.json();

            messagesContainer.innerHTML = '';

            if (data.success && data.messages && data.messages.length > 0) {
                data.messages.forEach(function(msg) {
                    appendMessage(messagesContainer, msg.content, msg.isOwn, msg.time, msg.imageUrl);
                });
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            } else {
                messagesContainer.innerHTML = '<div class="chat-empty-state"><i class="fa-regular fa-comment-dots"></i><p>Chưa có tin nhắn</p></div>';
            }

            chatWindow.setAttribute('data-last-count', data.messages ? data.messages.length : 0);
        } catch (error) {
            console.error('Error loading messages:', error);
            messagesContainer.innerHTML = '<div class="chat-empty-state"><p>Không thể tải tin nhắn</p></div>';
        }
    }

    function appendMessage(container, content, isOwn, time, imageUrl) {
        var emptyState = container.querySelector('.chat-empty-state');
        if (emptyState) emptyState.remove();

        var messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message ' + (isOwn ? 'outgoing' : 'incoming');
        
        var bubbleContent = escapeHtml(content);
        if (imageUrl) {
            bubbleContent = '<img src="' + escapeHtml(imageUrl) + '" alt="Image" style="max-width: 200px; border-radius: 8px; margin-bottom: 4px;"><br>' + bubbleContent;
        }
        
        messageDiv.innerHTML = '<div class="message-bubble">' + bubbleContent + '<span class="message-time">' + escapeHtml(time || '') + '</span></div>';
        container.appendChild(messageDiv);
    }

    async function sendMessage(chatWindow, input, receiverId) {
        var content = input.value.trim();
        if (!content) return;

        var messagesContainer = chatWindow.querySelector('.chat-messages');
        if (!messagesContainer) return;

        input.disabled = true;
        var sendBtn = chatWindow.querySelector('.chat-send-btn');
        if (sendBtn) sendBtn.disabled = true;

        var now = new Date();
        var timeStr = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');
        appendMessage(messagesContainer, content, true, timeStr);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
        input.value = '';

        try {
            var formData = new FormData();
            formData.append('ReceiverId', receiverId);
            formData.append('Content', content);

            var response = await fetch('/Message/SendMessage', {
                method: 'POST',
                body: formData
            });

            var data = await response.json();

            if (!data.success) {
                console.error('Failed to send message:', data.message);
                alert('Không thể gửi tin nhắn: ' + (data.message || 'Lỗi không xác định'));
            }
        } catch (error) {
            console.error('Error sending message:', error);
            alert('Không thể gửi tin nhắn. Vui lòng thử lại.');
        } finally {
            input.disabled = false;
            if (sendBtn) sendBtn.disabled = false;
            input.focus();
        }
    }

    function startPolling(userId, chatWindow) {
        if (pollingIntervals[userId]) {
            clearInterval(pollingIntervals[userId]);
        }

        pollingIntervals[userId] = setInterval(async function() {
            try {
                var response = await fetch('/Message/GetMessages?partnerId=' + userId);
                var data = await response.json();

                if (data.success && data.messages) {
                    var lastCount = parseInt(chatWindow.getAttribute('data-last-count') || '0');
                    
                    if (data.messages.length > lastCount) {
                        var messagesContainer = chatWindow.querySelector('.chat-messages');
                        if (messagesContainer) {
                            var newMessages = data.messages.slice(lastCount);
                            newMessages.forEach(function(msg) {
                                if (!msg.isOwn) {
                                    appendMessage(messagesContainer, msg.content, msg.isOwn, msg.time, msg.imageUrl);
                                }
                            });
                            messagesContainer.scrollTop = messagesContainer.scrollHeight;
                        }
                        chatWindow.setAttribute('data-last-count', data.messages.length);
                    }
                }
            } catch (error) {
                console.error('Polling error:', error);
            }
        }, 3000);
    }

    function closeChatWindow(chatWindow, userId) {
        if (pollingIntervals[userId]) {
            clearInterval(pollingIntervals[userId]);
            delete pollingIntervals[userId];
        }

        chatWindow.classList.remove('chat-show');

        setTimeout(function() {
            chatWindow.remove();
            activeChatWindows = activeChatWindows.filter(function(w) { return w !== chatWindow; });
            repositionChatWindows();
        }, 300);
    }

    function repositionChatWindows() {
        activeChatWindows.forEach(function(window, index) {
            var rightOffset = 90 + (index * 320);
            window.style.right = rightOffset + 'px';
        });
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    window.ChatManager = {
        openChat: openChatWindow,
        init: init
    };

    init();

})();
