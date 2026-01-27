/**
 * Sound Effects Manager - Pixel/Retro Style Sounds
 * Provides audio feedback for user interactions
 */

(function() {
    'use strict';

    const SoundEffects = {
        // Audio context
        audioContext: null,

        // Initialize audio context
        init: function() {
            if (!this.audioContext) {
                this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            }
        },

        // Play click sound
        click: function() {
            this.init();
            const oscillator = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(this.audioContext.destination);
            
            oscillator.frequency.value = 800;
            oscillator.type = 'square';
            
            gainNode.gain.setValueAtTime(0.1, this.audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.1);
            
            oscillator.start(this.audioContext.currentTime);
            oscillator.stop(this.audioContext.currentTime + 0.1);
        },

        // Play success sound (2 notes up)
        success: function() {
            this.init();
            const playNote = (freq, delay) => {
                const oscillator = this.audioContext.createOscillator();
                const gainNode = this.audioContext.createGain();
                
                oscillator.connect(gainNode);
                gainNode.connect(this.audioContext.destination);
                
                oscillator.frequency.value = freq;
                oscillator.type = 'square';
                
                gainNode.gain.setValueAtTime(0.2, this.audioContext.currentTime + delay);
                gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + delay + 0.15);
                
                oscillator.start(this.audioContext.currentTime + delay);
                oscillator.stop(this.audioContext.currentTime + delay + 0.15);
            };
            
            playNote(600, 0);
            playNote(800, 0.1);
        },

        // Play notification sound
        notification: function() {
            this.init();
            const oscillator = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(this.audioContext.destination);
            
            oscillator.frequency.value = 1000;
            oscillator.type = 'sine';
            
            gainNode.gain.setValueAtTime(0.15, this.audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.2);
            
            oscillator.start(this.audioContext.currentTime);
            oscillator.stop(this.audioContext.currentTime + 0.2);
        },

        // Play send message sound
        send: function() {
            this.init();
            const playNote = (freq, delay) => {
                const oscillator = this.audioContext.createOscillator();
                const gainNode = this.audioContext.createGain();
                
                oscillator.connect(gainNode);
                gainNode.connect(this.audioContext.destination);
                
                oscillator.frequency.value = freq;
                oscillator.type = 'triangle';
                
                gainNode.gain.setValueAtTime(0.15, this.audioContext.currentTime + delay);
                gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + delay + 0.1);
                
                oscillator.start(this.audioContext.currentTime + delay);
                oscillator.stop(this.audioContext.currentTime + delay + 0.1);
            };
            
            playNote(700, 0);
            playNote(900, 0.05);
        },

        // Play error sound
        error: function() {
            this.init();
            const oscillator = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(this.audioContext.destination);
            
            oscillator.frequency.value = 200;
            oscillator.type = 'sawtooth';
            
            gainNode.gain.setValueAtTime(0.2, this.audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.3);
            
            oscillator.start(this.audioContext.currentTime);
            oscillator.stop(this.audioContext.currentTime + 0.3);
        },

        // Play hover sound (subtle)
        hover: function() {
            this.init();
            const oscillator = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(this.audioContext.destination);
            
            oscillator.frequency.value = 600;
            oscillator.type = 'sine';
            
            gainNode.gain.setValueAtTime(0.05, this.audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.05);
            
            oscillator.start(this.audioContext.currentTime);
            oscillator.stop(this.audioContext.currentTime + 0.05);
        }
    };

    // Auto-attach to common elements
    document.addEventListener('DOMContentLoaded', function() {
        // Add click sound to buttons
        document.addEventListener('click', function(e) {
            if (e.target.matches('button, .btn, .nav-item, a[href]')) {
                SoundEffects.click();
            }
        });

        // Add hover sound to interactive elements (throttled)
        let hoverTimeout;
        document.addEventListener('mouseover', function(e) {
            if (e.target.matches('button, .btn, .nav-item')) {
                clearTimeout(hoverTimeout);
                hoverTimeout = setTimeout(() => {
                    SoundEffects.hover();
                }, 50);
            }
        });
    });

    // Export to window
    window.SoundEffects = SoundEffects;
})();
