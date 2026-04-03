// Enhanced Page Load Timing JavaScript with comprehensive metrics
window.pageLoadSpeed = {
    // Store timing data
    timingData: {
        navigationStart: 0,
        domContentLoaded: 0,
        loadComplete: 0,
        blazorReady: 0,
        scriptLoadTime: 0
    },

    // Store LCP value from PerformanceObserver
    lcpValue: 0,

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

        // Set up PerformanceObserver for LCP
        this.observeLCP();
    },

    // Observe LCP using PerformanceObserver API
    observeLCP: function() {
        const self = this;
        if ('PerformanceObserver' in window) {
            try {
                const observer = new PerformanceObserver((list) => {
                    const entries = list.getEntries();
                    const lastEntry = entries[entries.length - 1];
                    self.lcpValue = lastEntry.startTime;
                });
                observer.observe({ entryTypes: ['largest-contentful-paint'] });
            } catch (e) {
                // LCP observation not supported
            }
        }
    },

    // Get comprehensive performance metrics
    getComprehensiveMetrics: function() {
        const now = performance.now();
        let metrics = {
            // Timing metrics
            timeToFirstByte: 0,
            domContentLoaded: 0,
            loadComplete: 0,
            firstContentfulPaint: 0,
            largestContentfulPaint: 0,

            // Size metrics
            transferSize: 0,
            encodedSize: 0,
            decodedSize: 0,

            // Calculated metrics
            serverResponseTime: 0,
            domProcessingTime: 0,
            resourceLoadTime: 0
        };

        // Use Performance API timing if available
        if (window.performance && window.performance.timing) {
            const timing = window.performance.timing;
            const navStart = timing.navigationStart;

            // Core timing metrics
            metrics.timeToFirstByte = timing.responseStart > 0 ? timing.responseStart - navStart : 0;
            metrics.domContentLoaded = timing.domContentLoadedEventEnd > 0 ? timing.domContentLoadedEventEnd - navStart : now;
            metrics.loadComplete = timing.loadEventEnd > 0 ? timing.loadEventEnd - navStart : now;

            // Calculated metrics
            metrics.serverResponseTime = timing.responseEnd > 0 && timing.requestStart > 0
                ? timing.responseEnd - timing.requestStart : 0;
            metrics.domProcessingTime = timing.domContentLoadedEventEnd > 0 && timing.responseEnd > 0
                ? timing.domContentLoadedEventEnd - timing.responseEnd : 0;
            metrics.resourceLoadTime = timing.loadEventEnd > 0 && timing.domContentLoadedEventEnd > 0
                ? timing.loadEventEnd - timing.domContentLoadedEventEnd : 0;
        }

        // Get Paint Timing API metrics if available
        if (window.performance && window.performance.getEntriesByType) {
            try {
                const paintEntries = window.performance.getEntriesByType('paint');
                paintEntries.forEach(entry => {
                    if (entry.name === 'first-contentful-paint') {
                        metrics.firstContentfulPaint = Math.round(entry.startTime);
                    }
                });

                // Use stored LCP value from PerformanceObserver
                if (this.lcpValue > 0) {
                    metrics.largestContentfulPaint = Math.round(this.lcpValue);
                }
            } catch (e) {
                // Paint timing not supported
            }
        }

        // Get Navigation Timing API v2 for transfer sizes
        if (window.performance && window.performance.getEntriesByType) {
            try {
                const navEntries = window.performance.getEntriesByType('navigation');
                if (navEntries.length > 0) {
                    const navEntry = navEntries[0];
                    metrics.transferSize = navEntry.transferSize || 0;
                    metrics.encodedSize = navEntry.encodedBodySize || 0;
                    metrics.decodedSize = navEntry.decodedBodySize || 0;
                }
            } catch (e) {
                // Navigation timing v2 not supported
            }
        }

        // Get additional resource transfer sizes
        if (window.performance && window.performance.getEntriesByType) {
            try {
                const resourceEntries = window.performance.getEntriesByType('resource');
                let totalTransferSize = metrics.transferSize;
                let totalEncodedSize = metrics.encodedSize;
                let totalDecodedSize = metrics.decodedSize;

                resourceEntries.forEach(entry => {
                    totalTransferSize += entry.transferSize || 0;
                    totalEncodedSize += entry.encodedBodySize || 0;
                    totalDecodedSize += entry.decodedBodySize || 0;
                });

                metrics.transferSize = totalTransferSize;
                metrics.encodedSize = totalEncodedSize;
                metrics.decodedSize = totalDecodedSize;
            } catch (e) {
                // Resource timing not supported
            }
        }

        return metrics;
    },

    // Get current timing data (legacy method for backward compatibility)
    getTiming: function() {
        const metrics = this.getComprehensiveMetrics();
        return {
            NavigationStartToRender: metrics.loadComplete,
            LoadStartToDomReady: metrics.domContentLoaded
        };
    },

    // Format bytes to human readable format
    formatBytes: function(bytes) {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    },

    // Get Core Web Vitals
    getCoreWebVitals: function() {
        const vitals = {
            lcp: 0,
            fid: 0,
            cls: 0,
            fcp: 0,
            ttfb: 0
        };

        // Use stored LCP value from PerformanceObserver
        if (this.lcpValue > 0) {
            vitals.lcp = this.lcpValue;
        }

        // Try to get FCP
        try {
            const fcpEntries = performance.getEntriesByType('paint');
            fcpEntries.forEach(entry => {
                if (entry.name === 'first-contentful-paint') {
                    vitals.fcp = entry.startTime;
                }
            });
        } catch (e) {}

        // Try to get TTFB
        try {
            const navEntries = performance.getEntriesByType('navigation');
            if (navEntries.length > 0) {
                vitals.ttfb = navEntries[0].responseStart;
            }
        } catch (e) {}

        return vitals;
    },

    // Get detailed resource timing
    getResourceTiming: function() {
        try {
            const resources = performance.getEntriesByType('resource');
            let totalResources = 0;
            let totalSize = 0;
            let slowestResource = { name: '', duration: 0 };

            resources.forEach(resource => {
                totalResources++;
                totalSize += resource.transferSize || 0;

                if (resource.duration > slowestResource.duration) {
                    slowestResource = {
                        name: resource.name.split('/').pop() || resource.name,
                        duration: resource.duration
                    };
                }
            });

            return {
                totalResources,
                totalSize,
                slowestResource,
                averageResponseTime: totalResources > 0 ?
                    resources.reduce((sum, r) => sum + r.duration, 0) / totalResources : 0
            };
        } catch (e) {
            return {
                totalResources: 0,
                totalSize: 0,
                slowestResource: { name: 'N/A', duration: 0 },
                averageResponseTime: 0
            };
        }
    }
};

// Enhanced global function for Blazor to call with comprehensive metrics
window.getPageLoadMetrics = function() {
    try {
        const metrics = window.pageLoadSpeed.getComprehensiveMetrics();
        const coreVitals = window.pageLoadSpeed.getCoreWebVitals();
        const resourceTiming = window.pageLoadSpeed.getResourceTiming();

        return {
            // Timing metrics (in milliseconds)
            timeToFirstByte: Math.max(0, Math.round(metrics.timeToFirstByte || coreVitals.ttfb || 0)),
            domContentLoaded: Math.max(0, Math.round(metrics.domContentLoaded || performance.now())),
            loadComplete: Math.max(0, Math.round(metrics.loadComplete || performance.now())),
            firstContentfulPaint: Math.max(0, Math.round(metrics.firstContentfulPaint || coreVitals.fcp || 0)),
            largestContentfulPaint: Math.max(0, Math.round(metrics.largestContentfulPaint || coreVitals.lcp || 0)),

            // Size metrics (in bytes)
            transferSize: Math.max(0, metrics.transferSize || resourceTiming.totalSize || 0),
            encodedSize: Math.max(0, metrics.encodedSize || 0),
            decodedSize: Math.max(0, metrics.decodedSize || 0),

            // Calculated metrics (in milliseconds)
            serverResponseTime: Math.max(0, Math.round(metrics.serverResponseTime || 0)),
            domProcessingTime: Math.max(0, Math.round(metrics.domProcessingTime || 0)),
            resourceLoadTime: Math.max(0, Math.round(metrics.resourceLoadTime || 0)),

            // Formatted sizes for display
            transferSizeFormatted: window.pageLoadSpeed.formatBytes(metrics.transferSize || resourceTiming.totalSize || 0),
            encodedSizeFormatted: window.pageLoadSpeed.formatBytes(metrics.encodedSize || 0),
            decodedSizeFormatted: window.pageLoadSpeed.formatBytes(metrics.decodedSize || 0),

            // Additional metrics
            resourceCount: resourceTiming.totalResources,
            slowestResourceName: resourceTiming.slowestResource.name,
            slowestResourceTime: Math.round(resourceTiming.slowestResource.duration),
            averageResourceTime: Math.round(resourceTiming.averageResponseTime)
        };
    } catch (error) {
        console.warn('Page load metrics error:', error);
        // Return fallback values
        const now = performance.now();
        return {
            timeToFirstByte: Math.round(now * 0.1),
            domContentLoaded: Math.round(now * 0.8),
            loadComplete: Math.round(now),
            firstContentfulPaint: Math.round(now * 0.6),
            largestContentfulPaint: Math.round(now * 0.9),
            transferSize: 0,
            encodedSize: 0,
            decodedSize: 0,
            serverResponseTime: Math.round(now * 0.2),
            domProcessingTime: Math.round(now * 0.3),
            resourceLoadTime: Math.round(now * 0.1),
            transferSizeFormatted: '0 B',
            encodedSizeFormatted: '0 B',
            decodedSizeFormatted: '0 B',
            resourceCount: 0,
            slowestResourceName: 'N/A',
            slowestResourceTime: 0,
            averageResourceTime: 0
        };
    }
};

// Legacy function for backward compatibility
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

// Real-time performance monitoring
window.startPerformanceMonitoring = function(callback) {
    let observer;

    try {
        // Monitor LCP changes
        observer = new PerformanceObserver((list) => {
            for (const entry of list.getEntries()) {
                if (entry.entryType === 'largest-contentful-paint') {
                    callback({
                        type: 'lcp',
                        value: entry.startTime,
                        element: (entry.element && entry.element.tagName) || 'unknown'
                    });
                }
            }
        });

        observer.observe({ entryTypes: ['largest-contentful-paint'] });

        // Monitor layout shifts
        const clsObserver = new PerformanceObserver((list) => {
            let clsValue = 0;
            for (const entry of list.getEntries()) {
                if (!entry.hadRecentInput) {
                    clsValue += entry.value;
                }
            }
            if (clsValue > 0) {
                callback({
                    type: 'cls',
                    value: clsValue
                });
            }
        });

        clsObserver.observe({ entryTypes: ['layout-shift'] });

        return () => {
            observer.disconnect();
            clsObserver.disconnect();
        };
    } catch (e) {
        console.warn('Performance monitoring not supported:', e);
        return () => {};
    }
};

// WASM Metric Collection
window.pageLoadSpeed.wasmMetrics = {
    // Timing marks for WASM lifecycle
    wasmStartTime: 0,
    blazorStartTime: 0,
    wasmEndTime: 0,

    // Set WASM start time (called from beforeStart)
    markStart: function() {
        this.wasmStartTime = performance.now();
        try {
            performance.mark('wasm-start');
        } catch (e) {}
    },

    // Mark when WASM runtime is ready and Blazor starts
    markBlazorStart: function() {
        this.blazorStartTime = performance.now();
        try {
            performance.mark('blazor-start');
        } catch (e) {}
    },

    // Set WASM end time (called from afterStarted)
    markEnd: function() {
        if (this.wasmEndTime > 0) {
            return;
        }

        this.wasmEndTime = performance.now();
        try {
            performance.mark('wasm-end');
            performance.measure('wasm-runtime', 'wasm-start', 'wasm-end');
        } catch (e) {}
    },

    // Get WASM metrics
    getWasmMetrics: function() {
        const metrics = {
            // WASM Download
            wasmDownloadTime: 0,
            wasmDownloadSize: 0,
            wasmDownloadSizeFormatted: 'N/A',

            // Assemblies
            assemblyCount: 0,
            assemblyTotalSize: 0,
            assemblyTotalSizeFormatted: 'N/A',

            // Runtime Startup
            runtimeStartupTime: 0,

            // Memory Heap
            memoryUsed: 0,
            memoryTotal: 0,
            memoryFormatted: 'N/A',

            // Blazor Init
            blazorInitTime: 0
        };

        try {
            // Get WASM download info
            const wasmEntry = this.findWasmEntry();
            if (wasmEntry) {
                metrics.wasmDownloadTime = Math.round(wasmEntry.duration);
                metrics.wasmDownloadSize = wasmEntry.transferSize || 0;
                metrics.wasmDownloadSizeFormatted = window.pageLoadSpeed.formatBytes(metrics.wasmDownloadSize);
                
                // Use WASM entry timing to set blazorStartTime if not already set
                if (this.blazorStartTime === 0 && wasmEntry.responseEnd > 0) {
                    this.blazorStartTime = wasmEntry.responseEnd;
                }
            }

            // Get assemblies info
            const assemblyInfo = this.getAssemblyInfo();
            metrics.assemblyCount = assemblyInfo.count;
            metrics.assemblyTotalSize = assemblyInfo.totalSize;
            metrics.assemblyTotalSizeFormatted = window.pageLoadSpeed.formatBytes(metrics.assemblyTotalSize);

            // Runtime startup time (WASM runtime load to ready)
            if (this.wasmStartTime > 0 && this.blazorStartTime > 0) {
                metrics.runtimeStartupTime = Math.round(this.blazorStartTime - this.wasmStartTime);
            }

            // Memory heap (Chrome only)
            const memoryInfo = this.getMemoryInfo();
            if (memoryInfo) {
                metrics.memoryUsed = Math.round(memoryInfo.usedJSHeapSize / 1024 / 1024);
                metrics.memoryTotal = Math.round(memoryInfo.totalJSHeapSize / 1024 / 1024);
                metrics.memoryFormatted = metrics.memoryUsed + ' MB / ' + metrics.memoryTotal + ' MB';
            } else {
                metrics.memoryFormatted = 'N/A';
            }

            // Blazor init time (Blazor framework initialization)
            if (this.blazorStartTime > 0 && this.wasmEndTime > 0) {
                metrics.blazorInitTime = Math.round(this.wasmEndTime - this.blazorStartTime);
            }

        } catch (e) {
            console.warn('Error collecting WASM metrics:', e);
        }

        return metrics;
    },

    // Find the primary WASM resource entry in resource timing.
    // Returns the largest .wasm file under _framework/ (the main app assembly).
    // Falls back to the dotnet.native.*.js loader if no .wasm entries are found.
    findWasmEntry: function() {
        try {
            const resources = performance.getEntriesByType('resource');

            // Prefer actual .wasm files — pick the largest one by transferSize
            const wasmFiles = resources.filter(r =>
                r.name && r.name.includes('_framework') && r.name.endsWith('.wasm')
            );

            if (wasmFiles.length > 0) {
                return wasmFiles.reduce((largest, current) =>
                    (current.transferSize || 0) > (largest.transferSize || 0) ? current : largest
                );
            }

            // Fallback: .NET 9+ JS loader (often cached, may report transferSize: 0)
            return resources.find(r => r.name && r.name.includes('dotnet.native') && r.name.endsWith('.js'));
        } catch (e) {
            return null;
        }
    },

    // Get assembly information — count only actual .wasm files under _framework/.
    getAssemblyInfo: function() {
        const info = { count: 0, totalSize: 0 };
        try {
            const resources = performance.getEntriesByType('resource');
            resources.forEach(r => {
                if (r.name && r.name.includes('_framework') && r.name.endsWith('.wasm')) {
                    info.count++;
                    info.totalSize += r.transferSize || 0;
                }
            });
        } catch (e) {
            // Fall through with zeros
        }
        return info;
    },

    // Get memory info (Chrome only)
    getMemoryInfo: function() {
        try {
            if (performance.memory) {
                return performance.memory;
            }
        } catch (e) {}
        return null;
    },

    // Get navigation start time
    getNavigationStart: function() {
        try {
            if (window.performance && window.performance.timing) {
                return window.performance.timing.navigationStart;
            }
        } catch (e) {}
        return 0;
    }
};

// Initialize when script loads
window.pageLoadSpeed.init();

// Mark WASM startup start time (this runs before Blazor loads)
window.pageLoadSpeed.wasmMetrics.markStart();

// Global WASM metrics function for Blazor interop
window.getWasmMetrics = function() {
    try {
        if (window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics) {
            return window.pageLoadSpeed.wasmMetrics.getWasmMetrics();
        }
    } catch (e) {
        console.warn('Error getting WASM metrics:', e);
    }

    // Return default values if metrics unavailable
    return {
        wasmDownloadTime: 0,
        wasmDownloadSize: 0,
        wasmDownloadSizeFormatted: 'N/A',
        assemblyCount: 0,
        assemblyTotalSize: 0,
        assemblyTotalSizeFormatted: 'N/A',
        runtimeStartupTime: 0,
        memoryUsed: 0,
        memoryTotal: 0,
        memoryFormatted: 'N/A',
        blazorInitTime: 0
    };
};

// Add a global performance summary function
window.getPerformanceSummary = function() {
    const metrics = window.getPageLoadMetrics();
    const coreVitals = window.pageLoadSpeed.getCoreWebVitals();

    // Calculate performance score (0-100)
    let score = 100;

    // LCP scoring (40% weight)
    if (metrics.largestContentfulPaint > 4000) score -= 40;
    else if (metrics.largestContentfulPaint > 2500) score -= 20;
    else if (metrics.largestContentfulPaint > 1200) score -= 10;

    // FCP scoring (30% weight)
    if (metrics.firstContentfulPaint > 3000) score -= 30;
    else if (metrics.firstContentfulPaint > 1800) score -= 15;
    else if (metrics.firstContentfulPaint > 1000) score -= 5;

    // Load time scoring (30% weight)
    if (metrics.loadComplete > 5000) score -= 30;
    else if (metrics.loadComplete > 3000) score -= 15;
    else if (metrics.loadComplete > 1500) score -= 5;

    return {
        score: Math.max(0, Math.round(score)),
        metrics: metrics,
        recommendations: generateRecommendations(metrics)
    };
};

function generateRecommendations(metrics) {
    const recommendations = [];

    if (metrics.largestContentfulPaint > 2500) {
        recommendations.push('Optimize LCP: Consider image optimization and critical resource prioritization');
    }

    if (metrics.firstContentfulPaint > 1800) {
        recommendations.push('Improve FCP: Reduce render-blocking resources and inline critical CSS');
    }

    if (metrics.transferSize > 1024 * 1024) { // > 1MB
        recommendations.push('Reduce bundle size: Consider code splitting and asset optimization');
    }

    if (metrics.serverResponseTime > 600) {
        recommendations.push('Optimize server response: Consider caching and server performance improvements');
    }

    return recommendations;
}
