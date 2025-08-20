// ========================================
// COMPLETE SITE.JS FOR CAMP MAP SYSTEM
// Compatible with .NET 9 Blazor
// Supports both Mapa.razor and CampDetail.razor
// ========================================

// Global variables
let map = null;
let markersGroup = null;
let campMarkers = {};
let detailMaps = {};

// DOM ready event
document.addEventListener('DOMContentLoaded', function() {
    console.log('✅ DOM loaded');
    console.log('📍 Leaflet available:', typeof L !== 'undefined');
    console.log('🅱️ Bootstrap available:', typeof bootstrap !== 'undefined');

    // Add custom map styles
    addMapStyles();
});

// ========================================
// UTILITY FUNCTIONS
// ========================================

// Wait for Leaflet to be loaded
function waitForLeaflet(callback) {
    if (typeof L !== 'undefined') {
        callback();
    } else {
        console.log('⏳ Waiting for Leaflet to load...');
        setTimeout(() => waitForLeaflet(callback), 100);
    }
}

// Validate coordinates
function validateCoordinates(lat, lng) {
    const latitude = parseFloat(lat);
    const longitude = parseFloat(lng);

    if (isNaN(latitude) || isNaN(longitude)) {
        return { valid: false, error: 'Invalid number format', lat: 0, lng: 0 };
    }

    if (latitude === 0 && longitude === 0) {
        return { valid: false, error: 'Zero coordinates', lat: latitude, lng: longitude };
    }

    if (latitude < -90 || latitude > 90) {
        return { valid: false, error: 'Invalid latitude range', lat: latitude, lng: longitude };
    }

    if (longitude < -180 || longitude > 180) {
        return { valid: false, error: 'Invalid longitude range', lat: latitude, lng: longitude };
    }

    return { valid: true, lat: latitude, lng: longitude };
}

// ========================================
// OVERVIEW MAP FUNCTIONS (Mapa.razor)
// ========================================

// Initialize main overview map
window.initializeMap = function(camps) {
    console.log('🗺️ Initializing overview map with', camps?.length || 0, 'camps');

    waitForLeaflet(() => {
        try {
            // Clean up existing map
            if (map) {
                console.log('🧹 Cleaning up existing overview map');
                map.remove();
                map = null;
            }

            // Check if map container exists
            const mapContainer = document.getElementById('map');
            if (!mapContainer) {
                console.error('❌ Overview map container #map not found');
                return;
            }

            console.log('📐 Map container found, dimensions:', mapContainer.offsetWidth, 'x', mapContainer.offsetHeight);

            // Clear container
            mapContainer.innerHTML = '';

            // Initialize map centered on Plzen (Czech Republic)
            map = L.map('map', {
                center: [49.7384, 13.3736], // Plzen coordinates
                zoom: 11,
                scrollWheelZoom: true,
                zoomControl: true
            });

            // Add OpenStreetMap tile layer
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 18,
                minZoom: 5
            }).addTo(map);

            // Create markers group
            markersGroup = L.layerGroup().addTo(map);

            // Add markers for camps if provided
            if (camps && Array.isArray(camps) && camps.length > 0) {
                addCampMarkers(camps);

                // Fit map to show all markers
                if (camps.length === 1) {
                    // Single camp - center on it
                    const camp = camps[0];
                    const coordCheck = validateCoordinates(camp.latitude, camp.longitude);
                    if (coordCheck.valid) {
                        map.setView([coordCheck.lat, coordCheck.lng], 14);
                    }
                } else if (camps.length > 1) {
                    // Multiple camps - fit bounds
                    const validCamps = camps.filter(c => {
                        const check = validateCoordinates(c.latitude, c.longitude);
                        return check.valid;
                    });

                    if (validCamps.length > 0) {
                        const bounds = L.latLngBounds(validCamps.map(c => [c.latitude, c.longitude]));
                        map.fitBounds(bounds, { padding: [50, 50] });
                    }
                }
            } else {
                console.log('📍 No valid camps to display');
            }

            // Force resize after initialization
            setTimeout(() => {
                map.invalidateSize();
                console.log('✅ Overview map initialized successfully with', camps?.length || 0, 'camps');
            }, 100);

        } catch (error) {
            console.error('❌ Error initializing overview map:', error);
            const container = document.getElementById('map');
            if (container) {
                container.innerHTML = '<div class="alert alert-danger p-3">❌ Chyba při načítání mapy: ' + error.message + '</div>';
            }
        }
    });
};

// Update markers on the overview map
window.updateMapMarkers = function(camps) {
    console.log('🔄 Updating overview map markers with', camps?.length || 0, 'camps');

    if (!map) {
        console.warn('⚠️ Overview map not initialized, initializing now...');
        window.initializeMap(camps);
        return;
    }

    try {
        // Clear existing markers
        if (markersGroup) {
            markersGroup.clearLayers();
        }
        campMarkers = {};

        // Add new markers
        if (camps && Array.isArray(camps) && camps.length > 0) {
            addCampMarkers(camps);

            // Adjust map view to show markers
            const validCamps = camps.filter(c => {
                const check = validateCoordinates(c.latitude, c.longitude);
                return check.valid;
            });

            if (validCamps.length === 1) {
                const camp = validCamps[0];
                map.setView([camp.latitude, camp.longitude], 14);
            } else if (validCamps.length > 1) {
                const bounds = L.latLngBounds(validCamps.map(c => [c.latitude, c.longitude]));
                map.fitBounds(bounds, { padding: [50, 50] });
            }
        }

        console.log('✅ Overview map markers updated');

    } catch (error) {
        console.error('❌ Error updating overview map markers:', error);
    }
};

// Add camp markers to the overview map
function addCampMarkers(camps) {
    let validMarkerCount = 0;

    camps.forEach(camp => {
        // Validate camp data
        if (!camp) {
            console.warn('⚠️ Null camp object');
            return;
        }

        const coordCheck = validateCoordinates(camp.latitude, camp.longitude);
        if (!coordCheck.valid) {
            console.warn('⚠️ Camp with invalid coordinates:', camp.name, coordCheck.error);
            return;
        }

        validMarkerCount++;

        const icon = getOverviewMarkerIcon(camp.type);
        const marker = L.marker([coordCheck.lat, coordCheck.lng], { icon })
            .bindPopup(createPopupContent(camp));

        if (markersGroup) {
            markersGroup.addLayer(marker);
            campMarkers[camp.id] = marker;
        }
    });

    console.log('📍 Added', validMarkerCount, 'valid markers to overview map');
}

// Focus map on specific camp (used by "Focus on Map" buttons)
window.focusMapOnCamp = function(lat, lng) {
    try {
        if (map) {
            const coordCheck = validateCoordinates(lat, lng);
            if (coordCheck.valid) {
                map.setView([coordCheck.lat, coordCheck.lng], 15, {
                    animate: true,
                    duration: 1
                });
                console.log('🎯 Map focused on coordinates:', coordCheck.lat, coordCheck.lng);

                // Highlight the marker if it exists
                Object.values(campMarkers).forEach(marker => {
                    const markerLatLng = marker.getLatLng();
                    if (Math.abs(markerLatLng.lat - coordCheck.lat) < 0.001 &&
                        Math.abs(markerLatLng.lng - coordCheck.lng) < 0.001) {
                        marker.openPopup();
                    }
                });
            } else {
                console.warn('⚠️ Invalid coordinates for focus:', coordCheck.error);
            }
        } else {
            console.warn('⚠️ Map not initialized for focus operation');
        }
    } catch (error) {
        console.error('❌ Error focusing map:', error);
    }
};

// ========================================
// DETAIL MAP FUNCTIONS (CampDetail.razor)
// ========================================

// Initialize single camp detail map
window.initializeCampDetailMap = function(mapId, latitude, longitude, campName, location) {
    console.log('🏕️ === CAMP DETAIL MAP DEBUG ===');
    console.log('📍 MapId:', mapId);
    console.log('🌍 Coordinates:', latitude, longitude);
    console.log('🏕️ Camp:', campName);
    console.log('📍 Location:', location);

    waitForLeaflet(() => {
        try {
            // Check if container exists
            const container = document.getElementById(mapId);
            if (!container) {
                console.error('❌ Detail map container not found:', mapId);
                return;
            }

            console.log('📐 Container found, dimensions:', container.offsetWidth, 'x', container.offsetHeight);
            console.log('👁️ Container visible:', container.offsetWidth > 0 && container.offsetHeight > 0);

            // Clean up existing map instance
            if (detailMaps[mapId]) {
                console.log('🧹 Cleaning up existing detail map:', mapId);
                detailMaps[mapId].remove();
                delete detailMaps[mapId];
            }

            // Clear container
            container.innerHTML = '';

            // Validate coordinates
            const coordCheck = validateCoordinates(latitude, longitude);
            if (!coordCheck.valid) {
                console.warn('⚠️ Invalid coordinates for detail map:', coordCheck.error);
                container.innerHTML = `
                    <div class="alert alert-warning p-3 text-center">
                        <i class="fas fa-exclamation-triangle"></i><br>
                        <strong>Souřadnice pro tábor nejsou k dispozici</strong><br>
                        <small class="text-muted">Chyba: ${coordCheck.error}</small>
                    </div>
                `;
                return;
            }

            console.log('✅ Creating detail map with valid coordinates:', coordCheck.lat, coordCheck.lng);

            // Create map instance
            const detailMap = L.map(mapId, {
                center: [coordCheck.lat, coordCheck.lng],
                zoom: 15,
                scrollWheelZoom: true,
                zoomControl: true,
                attributionControl: true
            });

            // Store map instance
            detailMaps[mapId] = detailMap;

            console.log('🗺️ Map instance created, adding tile layer...');

            // Add tile layer
            const tileLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors',
                maxZoom: 18,
                minZoom: 5
            });

            tileLayer.addTo(detailMap);

            console.log('🎯 Adding marker...');

            // Create and add marker
            const marker = L.marker([coordCheck.lat, coordCheck.lng], {
                icon: getDetailMarkerIcon()
            })
                .addTo(detailMap)
                .bindPopup(`
                <div class="popup-content">
                    <h6 class="mb-2">${campName}</h6>
                    <p class="mb-0"><i class="fas fa-map-marker-alt"></i> ${location}</p>
                </div>
            `, {
                    closeButton: true,
                    autoClose: false
                });

            console.log('🚀 Finalizing map setup...');

            // Final setup with delays to ensure proper rendering
            setTimeout(() => {
                detailMap.invalidateSize();
                console.log('🔄 Map size invalidated');
            }, 100);

            setTimeout(() => {
                marker.openPopup();
                console.log('💬 Popup opened');
            }, 300);

            setTimeout(() => {
                console.log('✅ CAMP DETAIL MAP SUCCESSFULLY INITIALIZED! 🎉');
            }, 500);

        } catch (error) {
            console.error('❌ Critical error initializing detail map:', error);
            const container = document.getElementById(mapId);
            if (container) {
                container.innerHTML = `
                    <div class="alert alert-danger p-3 text-center">
                        <i class="fas fa-exclamation-circle"></i><br>
                        <strong>Chyba při načítání mapy</strong><br>
                        <small class="text-muted">${error.message}</small>
                    </div>
                `;
            }
        }
    });
};

// ========================================
// HELPER FUNCTIONS
// ========================================

// Create popup content for overview map
function createPopupContent(camp) {
    if (!camp) return '<div class="text-danger">Chyba: Žádná data tábora</div>';

    try {
        const availabilityClass = getAvailabilityBadge(camp.availableSpots);
        const availabilityText = getAvailabilityText(camp.availableSpots);

        return `
            <div class="popup-content" style="min-width: 250px;">
                <h6 class="mb-2 fw-bold text-primary">${camp.name || 'Neznámý tábor'}</h6>
                <p class="small text-muted mb-1">
                    <i class="fas fa-map-marker-alt"></i> ${camp.location || 'Neznámá lokace'}
                </p>
                <p class="small mb-2">${camp.shortDescription || ''}</p>
                <p class="fw-bold text-success mb-2">${(camp.price || 0).toLocaleString('cs-CZ')} Kč / týden</p>
                <div class="d-flex gap-2 align-items-center">
                    <a href="/tabor/${camp.id}" class="btn btn-primary btn-sm">
                        <i class="fas fa-info-circle"></i> Detail
                    </a>
                    <span class="badge ${availabilityClass}">${availabilityText}</span>
                </div>
            </div>
        `;
    } catch (error) {
        console.error('Error creating popup content:', error);
        return '<div class="alert alert-warning">Chyba při načítání informací o táboře</div>';
    }
}

// Get marker icon for overview map based on camp type
function getOverviewMarkerIcon(campType) {
    const iconConfig = getCampTypeConfig(campType);

    return L.divIcon({
        html: `
            <div class="overview-marker-icon" style="
                background: ${iconConfig.color};
                color: white;
                border: 3px solid white;
                border-radius: 50%;
                width: 36px;
                height: 36px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 16px;
                box-shadow: 0 3px 8px rgba(0,0,0,0.3);
                transition: transform 0.2s;
            ">
                <i class="fas ${iconConfig.icon}"></i>
            </div>
        `,
        iconSize: [36, 36],
        iconAnchor: [18, 18],
        popupAnchor: [0, -18],
        className: 'camp-marker'
    });
}

// Get special marker icon for detail maps
function getDetailMarkerIcon() {
    return L.divIcon({
        html: `
            <div class="detail-marker-icon" style="
                background: linear-gradient(135deg, #007bff 0%, #0056b3 100%);
                border: 4px solid white;
                border-radius: 50%;
                width: 50px;
                height: 50px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 20px;
                color: white;
                box-shadow: 0 4px 12px rgba(0,0,0,0.4);
            ">
                🏕️
            </div>
        `,
        iconSize: [50, 50],
        iconAnchor: [25, 25],
        popupAnchor: [0, -25],
        className: 'camp-detail-marker'
    });
}

// Get camp type configuration
function getCampTypeConfig(campType) {
    const configs = {
        'Sportovní': { icon: 'fa-futbol', color: '#FF6B35' },
        'Přírodní': { icon: 'fa-tree', color: '#28A745' },
        'Vzdělávací': { icon: 'fa-graduation-cap', color: '#6F42C1' },
        'Dobrodružný': { icon: 'fa-mountain', color: '#17A2B8' },
        'Kreativní': { icon: 'fa-palette', color: '#E91E63' },
        'Vědecký': { icon: 'fa-microscope', color: '#3F51B5' },
        'Vodní': { icon: 'fa-swimmer', color: '#00BCD4' },
        'Technický': { icon: 'fa-cog', color: '#6C757D' },
        'Jazykový': { icon: 'fa-comments', color: '#FD7E14' },
        'Hudební': { icon: 'fa-music', color: '#D63384' }
    };

    return configs[campType] || { icon: 'fa-campground', color: '#007BFF' };
}

// Get availability badge class
function getAvailabilityBadge(availableSpots) {
    if (typeof availableSpots !== 'number') return 'bg-secondary';

    if (availableSpots === 0) return 'bg-danger';
    if (availableSpots <= 5) return 'bg-warning text-dark';
    return 'bg-success';
}

// Get availability text
function getAvailabilityText(availableSpots) {
    if (typeof availableSpots !== 'number') return 'Neznámá dostupnost';

    if (availableSpots === 0) return 'Obsazeno';
    if (availableSpots === 1) return 'Poslední místo!';
    if (availableSpots <= 5) return `Posledních ${availableSpots} míst`;
    return `${availableSpots} volných míst`;
}

// ========================================
// CLEANUP FUNCTIONS
// ========================================

// Cleanup all maps (call when navigating away)
window.cleanupAllMaps = function() {
    console.log('🧹 Cleaning up all maps...');

    try {
        // Cleanup overview map
        if (map) {
            map.remove();
            map = null;
            console.log('✅ Overview map cleaned up');
        }

        // Cleanup all detail maps
        Object.keys(detailMaps).forEach(mapId => {
            if (detailMaps[mapId]) {
                detailMaps[mapId].remove();
                delete detailMaps[mapId];
                console.log('✅ Detail map cleaned up:', mapId);
            }
        });

        // Reset variables
        markersGroup = null;
        campMarkers = {};

        console.log('✅ All maps cleaned up successfully');

    } catch (error) {
        console.error('❌ Error during map cleanup:', error);
    }
};

// Cleanup specific detail map
window.cleanupDetailMap = function(mapId) {
    if (detailMaps[mapId]) {
        detailMaps[mapId].remove();
        delete detailMaps[mapId];
        console.log('✅ Detail map cleaned up:', mapId);
    }
};

// ========================================
// ADDITIONAL UTILITY FUNCTIONS
// ========================================

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

// ========================================
// CSS STYLES
// ========================================

// Add custom CSS styles for maps
function addMapStyles() {
    if (!document.getElementById('custom-map-styles')) {
        const style = document.createElement('style');
        style.id = 'custom-map-styles';
        style.textContent = `
            /* Map container styles */
            #campDetailMap, #map {
                border-radius: 8px;
                box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            }
            
            /* Leaflet popup styles */
            .leaflet-popup-content {
                margin: 12px 15px;
                line-height: 1.5;
                font-family: inherit;
            }
            
            .popup-content h6 {
                margin-bottom: 8px;
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

            /* Marker styles */
            .camp-marker, .camp-detail-marker {
                background: transparent !important;
                border: none !important;
            }
            
            .overview-marker-icon:hover {
                transform: scale(1.1);
            }
            
            /* Leaflet container styles */
            .leaflet-container {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            }

            .leaflet-popup-content-wrapper {
                border-radius: 8px;
                box-shadow: 0 3px 15px rgba(0,0,0,0.2);
            }

            .leaflet-popup-tip {
                background: white;
            }
            
            /* Leaflet controls */
            .leaflet-control-zoom {
                border: none !important;
                border-radius: 6px !important;
                box-shadow: 0 2px 10px rgba(0,0,0,0.15) !important;
            }
            
            .leaflet-control-zoom-in,
            .leaflet-control-zoom-out {
                font-size: 18px !important;
                line-height: 28px !important;
                width: 30px !important;
                height: 30px !important;
            }
            
            .leaflet-control-attribution {
                background: rgba(255, 255, 255, 0.8) !important;
                border-radius: 4px !important;
                font-size: 11px !important;
            }
        `;
        document.head.appendChild(style);
    }
}

// ========================================
// EVENT LISTENERS
// ========================================

// Cleanup maps when page unloads
window.addEventListener('beforeunload', function() {
    window.cleanupAllMaps();
});

// Handle Blazor navigation if available
if (typeof Blazor !== 'undefined') {
    Blazor.addEventListener('enhancedload', () => {
        console.log('🔄 Blazor enhanced navigation detected');
        // Maps will be reinitialized by individual pages
    });
}

console.log('✅ Complete Site.js loaded successfully - Camp Map System Ready! 🗺️🏕️');