// Global map variables
let map = null;
let markersGroup = null;
let campMarkers = {};

// Wait for Leaflet to be loaded
function waitForLeaflet(callback) {
    if (typeof L !== 'undefined') {
        callback();
    } else {
        setTimeout(() => waitForLeaflet(callback), 100);
    }
}

// Initialize main map for camps overview
window.initializeMap = function(camps) {
    console.log('Initializing map with camps:', camps);

    waitForLeaflet(() => {
        try {
            // Destroy existing map if it exists
            if (map) {
                map.remove();
                map = null;
            }

            // Check if map container exists
            const mapContainer = document.getElementById('map');
            if (!mapContainer) {
                console.error('Map container not found');
                return;
            }

            // Initialize map centered on Plzen
            map = L.map('map', {
                center: [49.7384, 13.3736],
                zoom: 11,
                scrollWheelZoom: true
            });

            // Add tile layer with Czech map
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 18,
                minZoom: 5
            }).addTo(map);

            // Create markers group
            markersGroup = L.layerGroup().addTo(map);

            // Add markers for camps
            if (camps && camps.length > 0) {
                updateMapMarkers(camps);

                // Fit map to show all markers
                if (camps.length > 1) {
                    const bounds = L.latLngBounds(camps.map(c => [c.latitude, c.longitude]));
                    map.fitBounds(bounds, { padding: [50, 50] });
                }
            }

            console.log('Map initialized successfully with', camps.length, 'camps');
        } catch (error) {
            console.error('Error initializing map:', error);
        }
    });
};

// Update map markers based on filtered camps
window.updateMapMarkers = function(camps) {
    console.log('Updating map markers for camps:', camps);

    if (!map) {
        console.warn('Map not initialized yet, initializing now...');
        initializeMap(camps);
        return;
    }

    try {
        // Clear existing markers
        if (markersGroup) {
            markersGroup.clearLayers();
        }
        campMarkers = {};

        // Add new markers
        if (camps && Array.isArray(camps)) {
            camps.forEach(camp => {
                // Validate camp data
                if (!camp.latitude || !camp.longitude) {
                    console.warn('Camp missing coordinates:', camp.name);
                    return;
                }

                const icon = getMarkerIcon(camp.type);
                const marker = L.marker([camp.latitude, camp.longitude], { icon })
                    .bindPopup(createPopupContent(camp));

                if (markersGroup) {
                    markersGroup.addLayer(marker);
                    campMarkers[camp.id] = marker;
                }
            });

            // Adjust map view if there are markers
            if (camps.length > 0) {
                const bounds = L.latLngBounds(camps.map(c => [c.latitude, c.longitude]));
                map.fitBounds(bounds, { padding: [50, 50] });
            }
        }

        console.log('Map markers updated:', camps.length, 'camps');
    } catch (error) {
        console.error('Error updating map markers:', error);
    }
};

// Create popup content for camp
function createPopupContent(camp) {
    const availabilityClass = getAvailabilityBadge(camp.availableSpots);
    const availabilityText = getAvailabilityText(camp.availableSpots);

    return `
        <div class="popup-content" style="min-width: 250px;">
            <h6 class="mb-2" style="color: #2c5aa0;">${camp.name}</h6>
            <p class="small text-muted mb-1">
                <i class="fas fa-map-marker-alt"></i> ${camp.location}
            </p>
            <p class="small mb-2">${camp.shortDescription}</p>
            <p class="fw-bold text-primary mb-2">${camp.price.toLocaleString('cs-CZ')} Kč / týden</p>
            <div class="d-flex gap-2 align-items-center">
                <a href="/tabor/${camp.id}" class="btn btn-primary btn-sm">
                    <i class="fas fa-info-circle"></i> Detail
                </a>
                <span class="badge ${availabilityClass}">${availabilityText}</span>
            </div>
        </div>
    `;
}

// Focus map on specific camp
window.focusMapOnCamp = function(lat, lng) {
    try {
        if (map) {
            map.setView([lat, lng], 15, {
                animate: true,
                duration: 1
            });
            console.log('Map focused on coordinates:', lat, lng);
        } else {
            console.warn('Map not initialized');
        }
    } catch (error) {
        console.error('Error focusing map:', error);
    }
};

// Initialize single camp detail map
window.initializeCampMap = function(lat, lng, name) {
    console.log('Initializing camp detail map for:', name, 'at', lat, lng);

    waitForLeaflet(() => {
        try {
            // Check if container exists
            const mapContainer = document.getElementById('campMap');
            if (!mapContainer) {
                console.error('Camp map container not found');
                return;
            }

            // Initialize map
            const campMap = L.map('campMap', {
                center: [lat, lng],
                zoom: 13,
                scrollWheelZoom: false
            });

            // Add tile layer
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 18
            }).addTo(campMap);

            // Create custom icon for the camp
            const icon = L.divIcon({
                html: `<div style="background: #2c5aa0; color: white; border-radius: 50%; width: 50px; height: 50px; display: flex; align-items: center; justify-content: center; font-size: 24px; border: 3px solid white; box-shadow: 0 3px 10px rgba(0,0,0,0.4);">
                    <i class="fas fa-campground"></i>
                </div>`,
                className: 'custom-camp-icon',
                iconSize: [50, 50],
                iconAnchor: [25, 25]
            });

            // Add marker for the camp
            L.marker([lat, lng], { icon })
                .bindPopup(`<strong>${name}</strong>`)
                .addTo(campMap)
                .openPopup();

            console.log('Camp detail map initialized successfully');
        } catch (error) {
            console.error('Error initializing camp detail map:', error);
        }
    });
};

// Get marker icon based on camp type
function getMarkerIcon(type) {
    const iconMap = {
        'adventure': { icon: 'fa-mountain', color: '#4CAF50' },
        'sport': { icon: 'fa-futbol', color: '#FF9800' },
        'creative': { icon: 'fa-palette', color: '#E91E63' },
        'science': { icon: 'fa-flask', color: '#3F51B5' },
        'water': { icon: 'fa-swimmer', color: '#00BCD4' }
    };

    const config = iconMap[type] || { icon: 'fa-campground', color: '#2c5aa0' };

    return L.divIcon({
        html: `<div style="background: ${config.color}; color: white; border-radius: 50%; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; font-size: 18px; border: 3px solid white; box-shadow: 0 2px 8px rgba(0,0,0,0.3);">
            <i class="fas ${config.icon}"></i>
        </div>`,
        className: 'custom-div-icon',
        iconSize: [40, 40],
        iconAnchor: [20, 20],
        popupAnchor: [0, -20]
    });
}

// Helper functions for availability badges
function getAvailabilityBadge(spots) {
    if (spots === 0) return 'bg-danger';
    if (spots <= 5) return 'bg-warning text-dark';
    return 'bg-success';
}

function getAvailabilityText(spots) {
    if (spots === 0) return 'Obsazeno';
    if (spots === 1) return 'Poslední místo!';
    if (spots <= 5) return `Zbývá ${spots} míst`;
    return `${spots} volných míst`;
}

// Modal functions
window.showModal = function(modalId) {
    try {
        const modalElement = document.getElementById(modalId);
        if (modalElement && typeof bootstrap !== 'undefined') {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        } else {
            console.error('Modal element not found or Bootstrap not loaded:', modalId);
        }
    } catch (error) {
        console.error('Error showing modal:', error);
    }
};

window.hideModal = function(modalId) {
    try {
        const modalElement = document.getElementById(modalId);
        if (modalElement && typeof bootstrap !== 'undefined') {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
    } catch (error) {
        console.error('Error hiding modal:', error);
    }
};

// Alert function
window.showAlert = function(message, type = 'info') {
    // Create Bootstrap alert if possible
    if (typeof bootstrap !== 'undefined') {
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3" style="z-index: 9999;">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        const alertElement = document.createElement('div');
        alertElement.innerHTML = alertHtml;
        document.body.appendChild(alertElement.firstElementChild);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            const alert = document.querySelector('.alert');
            if (alert) {
                alert.remove();
            }
        }, 5000);
    } else {
        // Fallback to standard alert
        alert(message);
    }
};

// Download function
window.downloadFile = function(url, filename) {
    try {
        const link = document.createElement('a');
        link.href = url;
        link.download = filename || url.split('/').pop();
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error('Error downloading file:', error);
    }
};

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded');
    console.log('Leaflet available:', typeof L !== 'undefined');
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');

    // Add custom CSS for map popups
    if (!document.getElementById('custom-map-styles')) {
        const style = document.createElement('style');
        style.id = 'custom-map-styles';
        style.textContent = `
            .leaflet-popup-content {
                margin: 10px;
                line-height: 1.4;
            }
            
            .popup-content h6 {
                margin-bottom: 8px;
                color: #2c5aa0;
                font-weight: 600;
            }
            
            .popup-content .btn {
                font-size: 0.875rem;
                padding: 0.25rem 0.75rem;
            }
            
            .popup-content .badge {
                font-size: 0.75rem;
                padding: 0.25rem 0.5rem;
            }

            .custom-div-icon {
                background: transparent !important;
                border: none !important;
            }
            
            .leaflet-container {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                border-radius: 15px;
            }

            .leaflet-popup-content-wrapper {
                border-radius: 10px;
                box-shadow: 0 3px 14px rgba(0,0,0,0.2);
            }

            .leaflet-popup-tip {
                background: white;
            }
            
            .custom-camp-icon {
                background: transparent !important;
                border: none !important;
            }
            
            /* Fix for Leaflet controls */
            .leaflet-control-zoom {
                border: 2px solid rgba(0,0,0,0.2) !important;
                border-radius: 5px !important;
            }
            
            .leaflet-control-zoom-in,
            .leaflet-control-zoom-out {
                font-size: 18px !important;
                line-height: 26px !important;
            }
        `;
        document.head.appendChild(style);
    }
});