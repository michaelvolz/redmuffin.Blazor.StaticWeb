// Page Load Timing JavaScript
window.pageLoadSpeed = {
    // Store timing data
    timingData: {
        navigationStart: 0,
        domContentLoaded: 0,
        loadComplete: 0,
        blazorReady: 0,
        scriptLoadTime: 0
    },

    // Initialize timing measurements
    init: function() {
        this.timingData.scriptLoadTime = performance.now();

        // Use Performance API if available
        if (window.performance && window.performance.timing) {
            const timing = window.performance.timing;
            this.timingData.navigationStart = timing.navigationStart;
        } else {
            this.timingData.navigationStart = Date.now();
        }

        // Set up event listeners for future events
        document.addEventListener('DOMContentLoaded', () => {
            this.timingData.domContentLoaded = performance.now();
        });

        window.addEventListener('load', () => {
            this.timingData.loadComplete = performance.now();
        });

        // Check if DOM is already loaded
        if (document.readyState === 'loading') {
            // DOM is still loading
        } else if (document.readyState === 'interactive') {
            // DOM loading finished, but resources may still be loading
            this.timingData.domContentLoaded = performance.now();
        } else if (document.readyState === 'complete') {
            // Page is fully loaded
            this.timingData.domContentLoaded = performance.now();
            this.timingData.loadComplete = performance.now();
        }
    },

    // Get current timing data
    getTiming: function() {
        const now = performance.now();

        // Use Performance API timing if available
        if (window.performance && window.performance.timing) {
            const timing = window.performance.timing;
            const navStart = timing.navigationStart;

            const domReady = timing.domContentLoadedEventEnd > 0
                ? timing.domContentLoadedEventEnd - navStart
                : this.timingData.domContentLoaded || now;

            const loadComplete = timing.loadEventEnd > 0
                ? timing.loadEventEnd - navStart
                : this.timingData.loadComplete || now;

            return {
                NavigationStartToRender: loadComplete,
                LoadStartToDomReady: domReady
            };
        } else {
            // Fallback to manual timing
            return {
                NavigationStartToRender: this.timingData.loadComplete || now,
                LoadStartToDomReady: this.timingData.domContentLoaded || now
            };
        }
    }
};

// Global function for Blazor to call
window.getPageLoadTimes = function() {
    try {
        const timing = window.pageLoadSpeed.getTiming();

        // Ensure we return valid numbers
        const navToRender = typeof timing.NavigationStartToRender === 'number' && !isNaN(timing.NavigationStartToRender)
            ? timing.NavigationStartToRender
            : performance.now();

        const loadToDom = typeof timing.LoadStartToDomReady === 'number' && !isNaN(timing.LoadStartToDomReady)
            ? timing.LoadStartToDomReady
            : performance.now() * 0.8;

        return [Math.max(0, navToRender), Math.max(0, loadToDom)];
    } catch (error) {
        console.warn('Page load timing error:', error);
        // Return fallback values
        const now = performance.now();
        return [now, now * 0.8];
    }
};

// Initialize when script loads
window.pageLoadSpeed.init();
