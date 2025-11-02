/**
 * Heartbeat ECG Loader - Custom Loading Animation
 * 
 * A medical-themed loading animation featuring a beating heart and ECG/life line
 * waveform similar to a heart monitor.
 * 
 * Usage:
 *   syringeLoader.show('Loading...');     // Show with custom text
 *   syringeLoader.show();                 // Show with default "Loading..." text
 *   syringeLoader.hide();                 // Hide the loader
 *   syringeLoader.setText('New text');    // Update text while loading
 * 
 * Example:
 *   syringeLoader.show('Saving appointment...');
 *   // Do your work here
 *   setTimeout(() => syringeLoader.hide(), 2000);
 */
class SyringeLoader {
    constructor() {
        this.overlay = null;
        this.init();
    }

    init() {
        // Create loader HTML if it doesn't exist
        if (!document.getElementById('syringe-loader-overlay')) {
            const loaderHTML = `
                <div id="syringe-loader-overlay" class="syringe-loader-overlay">
                    <div class="syringe-loader-container">
                        <div class="ecg-background">
                            <svg class="ecg-trace-bg" viewBox="0 0 330 150" preserveAspectRatio="none">
                                <path d="M 0 75 L 30 75 L 35 30 L 45 120 L 55 75 L 250 75 L 255 50 L 265 100 L 275 75 L 330 75" 
                                      stroke-dasharray="1000" 
                                      stroke-dashoffset="1000"
                                      fill="none"/>
                            </svg>
                            <div class="ecg-background-line"></div>
                        </div>
                        <div class="heart-beat"></div>
                        <div class="syringe-loader-text">Loading...</div>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', loaderHTML);
        }
        
        this.overlay = document.getElementById('syringe-loader-overlay');
    }

    show(text = 'Loading...') {
        if (!this.overlay) {
            this.init();
        }
        
        // Update text if provided
        const textElement = this.overlay.querySelector('.syringe-loader-text');
        if (textElement) {
            textElement.textContent = text;
        }
        
        // Show overlay
        this.overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    hide() {
        if (this.overlay) {
            this.overlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    setText(text) {
        if (this.overlay) {
            const textElement = this.overlay.querySelector('.syringe-loader-text');
            if (textElement) {
                textElement.textContent = text;
            }
        }
    }
}

// Create global instance
const syringeLoader = new SyringeLoader();

// Track when loader started showing
let loaderStartTime = null;
const LOADER_DURATION = 2000; // 2 seconds

// Function to show loader on page load
function showLoaderOnPageLoad() {
    syringeLoader.show('Loading...');
    loaderStartTime = Date.now();
}

// Function to hide loader - ensures minimum 2.5 seconds display
function hideLoaderWhenReady() {
    if (!loaderStartTime) {
        // If loader wasn't shown yet, wait for it
        setTimeout(hideLoaderWhenReady, 100);
        return;
    }
    
    const elapsed = Date.now() - loaderStartTime;
    const remaining = Math.max(0, LOADER_DURATION - elapsed);
    
    setTimeout(() => {
        syringeLoader.hide();
    }, remaining);
}

// Auto-show on every page load - works if DOM is already loaded or not
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', showLoaderOnPageLoad);
} else {
    // DOM is already loaded, show immediately
    showLoaderOnPageLoad();
}

// Hide loader when page is fully loaded (but respect 2.5s minimum)
if (document.readyState === 'complete') {
    hideLoaderWhenReady();
} else {
    window.addEventListener('load', hideLoaderWhenReady);
}

// Show loader before page unloads (when navigating)
window.addEventListener('beforeunload', function() {
    syringeLoader.show('Loading...');
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SyringeLoader;
}

