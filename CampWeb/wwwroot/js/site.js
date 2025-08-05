// Global map variable
let map;
let markersGroup;

// Initialize main map for camps overview
window.initializeMap = (camps) => {
    try {
        // Check if Leaflet is loaded
        if (typeof L === 'undefined') {
            console.error('Leaflet library not loaded');
            return;
        }

        // Initialize map centered on Plzen
        map = L.map('map').setView([49.7384, 13.3736], 11);

        // Add tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 18,
        }).addTo(map);

        // Create markers group
        markersGroup = L.layerGroup().addTo(map);

        // Add markers for camps
        updateMapMarkers(camps);

        console.log('Map initialized successfully with', camps.length, 'camps');
    } catch (error) {
        console.error('Error initializing map:', error);
    }
};

// Update map markers based on filtered camps
window.updateMapMarkers = (camps) => {
    try {
        if (!markersGroup || !map) {
            console.warn('Map not initialized yet');
            return;
        }

        // Clear existing markers
        markersGroup.clearLayers();

        // Add new markers
        camps.forEach(camp => {
            const icon = getMarkerIcon(camp.type);
            const marker = L.marker([camp.latitude, camp.longitude], { icon })
                .bindPopup(`
                    <div class="popup-content">
                        <h6>${camp.name}</h6>
                        <p class="small text-muted">${camp.location}</p>
                        <p class="small">${camp.shortDescription}</p>
                        <p class="fw-bold text-primary">${camp.price} Kč / týden</p>
                        <div class="d-flex gap-1">
                            <a href="/tabor/${camp.id}" class="btn btn-primary btn-sm">Detail</a>
                            <span class="badge ${getAvailabilityBadge(camp.availableSpots)}">${getAvailabilityText(camp.availableSpots)}</span>
                        </div>
                    </div>
                `);

            markersGroup.addLayer(marker);
        });

        console.log('Map markers updated:', camps.length, 'camps');
    } catch (error) {
        console.error('Error updating map markers:', error);
    }
};

// Focus map on specific camp
window.focusMapOnCamp = (lat, lng) => {
    try {
        if (map) {
            map.setView([lat, lng], 15);
            console.log('Map focused on coordinates:', lat, lng);
        }
    } catch (error) {
        console.error('Error focusing map:', error);
    }
};

// Initialize single camp map
window.initializeCampMap = (lat, lng, name) => {
    try {
        if (typeof L === 'undefined') {
            console.error('Leaflet library not loaded for camp map');
            return;
        }

        const campMap = L.map('campMap').setView([lat, lng], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 18,
        }).addTo(campMap);

        // Add marker for the camp
        L.marker([lat, lng])
            .bindPopup(`<strong>${name}</strong>`)
            .addTo(campMap)
            .openPopup();

        console.log('Camp map initialized for:', name);
    } catch (error) {
        console.error('Error initializing camp map:', error);
    }
};

// Get marker icon based on camp type
function getMarkerIcon(type) {
    const iconMap = {
        'adventure': '🏔️',
        'sport': '⚽',
        'creative': '🎨',
        'science': '🔬',
        'water': '🏊'
    };

    const emoji = iconMap[type] || '🏕️';

    return L.divIcon({
        html: `<div style="background: #2c5aa0; color: white; border-radius: 50%; width: 40px; height: 40px; display: flex; align-items: center; justify-content: center; font-size: 20px; border: 3px solid white; box-shadow: 0 2px 5px rgba(0,0,0,0.3);">${emoji}</div>`,
        className: 'custom-div-icon',
        iconSize: [40, 40],
        iconAnchor: [20, 20]
    });
}

// Helper functions for availability badges
function getAvailabilityBadge(spots) {
    return spots === 0 ? 'bg-danger' : spots <= 5 ? 'bg-warning' : 'bg-success';
}

function getAvailabilityText(spots) {
    return spots === 0 ? 'Obsazeno' : spots <= 5 ? 'Poslední místa' : 'Volná místa';
}

// Modal functions
window.showModal = (modalId) => {
    try {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        } else {
            console.error('Modal element not found:', modalId);
        }
    } catch (error) {
        console.error('Error showing modal:', error);
    }
};

window.hideModal = (modalId) => {
    try {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
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
window.showAlert = (message) => {
    alert(message);
};

// Download function
window.downloadFile = (url) => {
    try {
        const link = document.createElement('a');
        link.href = url;
        link.download = url.split('/').pop();
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error('Error downloading file:', error);
    }
};

// Check if libraries are loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded');
    console.log('Leaflet available:', typeof L !== 'undefined');
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
});

// Add custom CSS for map popups
const style = document.createElement('style');
style.textContent = `
    .leaflet-popup-content {
        width: 250px !important;
    }
    
    .popup-content h6 {
        margin-bottom: 8px;
        color: #2c5aa0;
    }
    
    .popup-content .btn {
        font-size: 0.8rem;
        padding: 0.25rem 0.5rem;
    }
    
    .popup-content .badge {
        font-size: 0.7rem;
    }

    .custom-div-icon {
        background: transparent !important;
        border: none !important;
    }
    
    .leaflet-container {
        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    }

    .leaflet-popup-content-wrapper {
        border-radius: 10px;
    }

    .leaflet-popup-tip {
        background: white;
    }
`;
document.head.appendChild(style);