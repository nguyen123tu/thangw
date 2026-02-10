console.log('AI Chat Script Loaded');

document.addEventListener('DOMContentLoaded', function () {
    console.log('AI Chat: DOMContentLoaded');
    const aiToggleBtn = document.getElementById('aiToggleBtn');
    const aiPopup = document.getElementById('aiPopup');
    const aiCloseBtn = document.getElementById('aiCloseBtn');
    const aiInput = document.getElementById('aiInput');
    const aiSendBtn = document.getElementById('aiSendBtn');
    const aiBody = document.getElementById('aiBody');

    if (!aiToggleBtn) {
        console.error('AI Chat: Toggle button not found!');
        return;
    } else {
        console.log('AI Chat: Toggle button found');
    }

    // Toggle Popup
    aiToggleBtn.addEventListener('click', () => {
        console.log('AI Chat: Toggle clicked');
        aiPopup.classList.toggle('active');
        if (aiPopup.classList.contains('active')) {
            console.log('AI Chat: Popup opened');
            setTimeout(() => aiInput.focus(), 300);
        } else {
            console.log('AI Chat: Popup closed');
        }
    });

    aiCloseBtn.addEventListener('click', () => {
        console.log('AI Chat: Close clicked');
        aiPopup.classList.remove('active');
    });

    // Input handling
    aiInput.addEventListener('input', () => {
        aiSendBtn.disabled = !aiInput.value.trim();
    });

    aiInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    aiSendBtn.addEventListener('click', sendMessage);

    async function sendMessage() {
        const text = aiInput.value.trim();
        if (!text) return;

        // Add user message
        appendMessage(text, 'user');
        aiInput.value = '';
        aiSendBtn.disabled = true;

        // Show typing indicator
        const typingId = showTypingIndicator();

        try {
            const response = await fetch('/Chatbot/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ message: text })
            });

            const data = await response.json();

            // Remove typing indicator
            removeTypingIndicator(typingId);

            if (data.success) {
                appendMessage(data.response, 'bot');
            } else {
                appendMessage('Có lỗi xảy ra. Vui lòng thử lại.', 'bot');
            }
        } catch (error) {
            removeTypingIndicator(typingId);
            appendMessage('Không thể kết nối với server.', 'bot');
            console.error('Chat error:', error);
        }
    }

    function appendMessage(text, sender) {
        const div = document.createElement('div');
        div.className = `ai-message ${sender}`;

        const avatar = document.createElement('div');
        avatar.className = 'ai-avatar';
        avatar.innerHTML = sender === 'bot' ? '<i class="fa-solid fa-robot"></i>' : '<i class="fa-regular fa-user"></i>';

        const content = document.createElement('div');
        content.className = 'ai-content';
        // Simple text for user, HTML for bot (careful with XSS if not trusted, but here it's from our backend/Gemini)
        if (sender === 'user') {
            content.textContent = text;
        } else {
            // Basic formatting for bot response - normally needs a markdown parser
            // For now we just replace newlines with <br> and safeguard basic stuff
            content.innerHTML = formatBotResponse(text);
        }

        div.appendChild(avatar);
        div.appendChild(content);
        aiBody.appendChild(div);
        scrollToBottom();
    }

    function formatBotResponse(text) {
        // Basic bold/code formatting helper
        let formatted = text
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/\n/g, '<br>')
            .replace(/\*\*(.*?)\*\*/g, '<b>$1</b>')
            .replace(/`(.*?)`/g, '<code>$1</code>');
        return formatted;
    }

    function showTypingIndicator() {
        const id = 'typing-' + Date.now();
        const div = document.createElement('div');
        div.className = 'ai-message bot';
        div.id = id;
        div.innerHTML = `
            <div class="ai-avatar"><i class="fa-solid fa-robot"></i></div>
            <div class="ai-content">
                <div class="typing-indicator">
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                </div>
            </div>
        `;
        aiBody.appendChild(div);
        scrollToBottom();
        return id;
    }

    function removeTypingIndicator(id) {
        const el = document.getElementById(id);
        if (el) el.remove();
    }

    function scrollToBottom() {
        aiBody.scrollTop = aiBody.scrollHeight;
    }
});
