// Initialize Map
const map = L.map('map').setView([56.2084, 10.0359], 13);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '© OpenStreetMap contributors'
}).addTo(map);

// Keep track of active map markers & polyline paths
const trackerMarkers = {};
const trackerPaths = {};
let selectedCoordinates = null;
let currentPreviewInstructions = [];
let activePickerMode = null; // 'init' or 'route' or null

let mainPreviewPolyline = null;
let mainPreviewRouteLine = null;

let gpsPickerMode = 'node'; // 'node' or 'initial-location'
let activePickerStartLat = 56.2084;
let activePickerStartLng = 10.0359;

// Find the final coordinate in the current preview queue (or fallback to init)
function getLastPreviewPoint() {
    let lastLat = parseFloat(document.getElementById('init-lat').value);
    let lastLng = parseFloat(document.getElementById('init-lng').value);
    
    for (let i = currentPreviewInstructions.length - 1; i >= 0; i--) {
        const inst = currentPreviewInstructions[i];
        if (inst && (inst.type == 2 || inst.type === 'drive')) {
            const instLat = parseFloat(inst.latitude !== undefined ? inst.latitude : inst.lat);
            const instLng = parseFloat(inst.longitude !== undefined ? inst.longitude : inst.lng);
            if (!isNaN(instLat) && !isNaN(instLng)) {
                lastLat = instLat;
                lastLng = instLng;
                break;
            }
        }
    }
    return { lat: lastLat, lng: lastLng };
}

// Redraw the solid route line built from queued instructions
function drawMainPreviewRoute() {
    if (mainPreviewRouteLine) {
        map.removeLayer(mainPreviewRouteLine);
        mainPreviewRouteLine = null;
    }
    
    let lastLat = parseFloat(document.getElementById('init-lat').value);
    let lastLng = parseFloat(document.getElementById('init-lng').value);
    if (isNaN(lastLat) || isNaN(lastLng)) return;
    
    const points = [[lastLat, lastLng]];
    currentPreviewInstructions.forEach(inst => {
        if (inst.type === 2 && inst.latitude && inst.longitude) {
            points.push([inst.latitude, inst.longitude]);
        }
    });
    
    if (points.length > 1) {
        mainPreviewRouteLine = L.polyline(points, {
            color: '#ffaa00',
            weight: 3,
            opacity: 0.7
        }).addTo(map);
    }
}

// Bind initial coordinates manual change listeners
document.getElementById('init-lat').addEventListener('change', drawMainPreviewRoute);
document.getElementById('init-lng').addEventListener('change', drawMainPreviewRoute);

// FAB Add click - opens Wizard Step 1
document.getElementById('btn-fab-add').addEventListener('click', () => {
    // Clear state
    document.getElementById('new-imei').value = '';
    document.getElementById('init-lat').value = '56.2084';
    document.getElementById('init-lng').value = '10.0359';
    currentPreviewInstructions = [];
    if (mainPreviewPolyline) {
        map.removeLayer(mainPreviewPolyline);
        mainPreviewPolyline = null;
    }
    drawMainPreviewRoute();

    // Seed random IMEI
    const randomDigits = Math.floor(100000000 + Math.random() * 900000000);
    document.getElementById('new-imei').value = `353344${randomDigits}`;

    // Show Step 1 modal
    document.getElementById('wizard-step1-modal').classList.add('active');
});

// Close Wizard
document.getElementById('btn-close-wizard').addEventListener('click', () => {
    document.getElementById('wizard-step1-modal').classList.remove('active');
});

// Random IMEI button in Wizard
document.getElementById('btn-rand-imei').addEventListener('click', () => {
    const randomDigits = Math.floor(100000000 + Math.random() * 900000000);
    document.getElementById('new-imei').value = `353344${randomDigits}`;
});

// Pick Initial Location on sub-map
document.getElementById('btn-pick-init').addEventListener('click', () => {
    gpsPickerMode = 'initial-location';
    openGpsPickerForNode(null);
});

// Next Step -> Opens Visual Designer
document.getElementById('btn-wizard-next').addEventListener('click', () => {
    const imei = document.getElementById('new-imei').value.trim();
    if (!imei || imei.length < 5) {
        alert('Please specify a valid IMEI number.');
        return;
    }

    const initLat = parseFloat(document.getElementById('init-lat').value);
    const initLng = parseFloat(document.getElementById('init-lng').value);
    if (isNaN(initLat) || isNaN(initLng)) {
        alert('Please specify valid initial coordinates.');
        return;
    }

    // Hide Step 1 modal
    document.getElementById('wizard-step1-modal').classList.remove('active');

    // Setup and open visual designer (Step 2)
    syncPreviewToVisualNodes();
    const wfModal = document.getElementById('workflow-modal');
    if (wfModal) {
        wfModal.classList.add('active');
    }
    renderDesignerBoard();
});

// Workflow Designer Node State Data
let visualNodes = [];
let pickerMap = null;
let pickerMarker = null;
let activePickingNodeId = null;
let tempPickedCoords = null;

document.getElementById('btn-close-modal').addEventListener('click', () => {
    const wfModal = document.getElementById('workflow-modal');
    if (wfModal) {
        wfModal.classList.remove('active');
    }
});

let pickerStartMarker = null;
let pickerPathLine = null;

// GPS Picker Modal Event Listeners
document.getElementById('btn-close-gps-modal').addEventListener('click', () => {
    document.getElementById('gps-picker-modal').classList.remove('active');
    activePickingNodeId = null;
    tempPickedCoords = null;
});

document.getElementById('btn-confirm-gps').addEventListener('click', () => {
    if (tempPickedCoords) {
        if (gpsPickerMode === 'initial-location') {
            document.getElementById('init-lat').value = tempPickedCoords.lat.toFixed(6);
            document.getElementById('init-lng').value = tempPickedCoords.lng.toFixed(6);
            drawMainPreviewRoute();
        } else if (gpsPickerMode === 'node' && activePickingNodeId) {
            const node = visualNodes.find(n => n.id === activePickingNodeId);
            if (node) {
                node.latitude = tempPickedCoords.lat;
                node.longitude = tempPickedCoords.lng;
            }
            renderDesignerBoard();
        }
    }
    document.getElementById('gps-picker-modal').classList.remove('active');
    activePickingNodeId = null;
    tempPickedCoords = null;
});

function openGpsPickerForNode(nodeId) {
    activePickingNodeId = nodeId;
    let lat, lng, startLat, startLng;

    if (nodeId === null) {
        // Initial Location mode
        lat = parseFloat(document.getElementById('init-lat').value) || 56.2084;
        lng = parseFloat(document.getElementById('init-lng').value) || 10.0359;
        startLat = lat;
        startLng = lng;
    } else {
        const node = visualNodes.find(n => n.id === nodeId);
        if (!node) return;
        lat = parseFloat(node.latitude !== undefined ? node.latitude : node.lat) || 56.2084;
        lng = parseFloat(node.longitude !== undefined ? node.longitude : node.lng) || 10.0359;

        // Find starting coordinates by scanning preceding nodes
        const nodeIndex = visualNodes.findIndex(n => n.id === nodeId);
        startLat = 56.2084;
        startLng = 10.0359;
        let foundPreceding = false;
        for (let i = nodeIndex - 1; i >= 0; i--) {
            const prevNode = visualNodes[i];
            if (prevNode && (prevNode.type === 'drive' || prevNode.type == 2)) {
                const prevLat = parseFloat(prevNode.latitude !== undefined ? prevNode.latitude : prevNode.lat);
                const prevLng = parseFloat(prevNode.longitude !== undefined ? prevNode.longitude : prevNode.lng);
                if (!isNaN(prevLat) && !isNaN(prevLng)) {
                    startLat = prevLat;
                    startLng = prevLng;
                    foundPreceding = true;
                    break;
                }
            }
        }
        if (!foundPreceding) {
            const initLat = parseFloat(document.getElementById('init-lat').value);
            const initLng = parseFloat(document.getElementById('init-lng').value);
            if (!isNaN(initLat) && !isNaN(initLng)) {
                startLat = initLat;
                startLng = initLng;
            }
        }
    }

    // Default target coords to startLat/startLng if they are still at the default Sabro values
    let currentTargetLat = lat;
    let currentTargetLng = lng;
    if (nodeId !== null && Math.abs(lat - 56.2084) < 0.00001 && Math.abs(lng - 10.0359) < 0.00001) {
        currentTargetLat = startLat;
        currentTargetLng = startLng;
    }
    tempPickedCoords = { lat: currentTargetLat, lng: currentTargetLng };

    activePickerStartLat = startLat;
    activePickerStartLng = startLng;

    if (!pickerMap) {
        pickerMap = L.map('gps-picker-map').setView([startLat, startLng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(pickerMap);

        pickerMap.on('click', (e) => {
            const clickLat = e.latlng.lat;
            const clickLng = e.latlng.lng;
            tempPickedCoords = { lat: clickLat, lng: clickLng };

            if (pickerMarker) {
                pickerMarker.setLatLng([clickLat, clickLng]);
            } else {
                pickerMarker = L.marker([clickLat, clickLng]).addTo(pickerMap);
            }

            if (pickerPathLine) {
                pickerPathLine.setLatLngs([[activePickerStartLat, activePickerStartLng], [clickLat, clickLng]]);
            }

            document.getElementById('gps-picker-coords').textContent = `SELECTED: ${clickLat.toFixed(5)}, ${clickLng.toFixed(5)}`;
            document.getElementById('btn-confirm-gps').removeAttribute('disabled');
        });
    }

    // Set starting position marker (green circle)
    const startPos = [startLat, startLng];
    if (pickerStartMarker) {
        pickerStartMarker.setLatLng(startPos);
    } else {
        pickerStartMarker = L.circleMarker(startPos, {
            radius: 8,
            fillColor: '#00ff66',
            color: '#ffffff',
            weight: 2,
            opacity: 1,
            fillOpacity: 0.8
        }).addTo(pickerMap).bindPopup('Device Starting Position');
    }

    // Set destination marker
    if (pickerMarker) {
        pickerMarker.setLatLng([currentTargetLat, currentTargetLng]);
    } else {
        pickerMarker = L.marker([currentTargetLat, currentTargetLng]).addTo(pickerMap);
    }

    // Set dashed path connecting starting position to destination
    if (pickerPathLine) {
        pickerPathLine.setLatLngs([[startLat, startLng], [currentTargetLat, currentTargetLng]]);
    } else {
        pickerPathLine = L.polyline([[startLat, startLng], [currentTargetLat, currentTargetLng]], {
            color: '#007acc',
            weight: 2,
            dashArray: '5, 5'
        }).addTo(pickerMap);
    }

    document.getElementById('gps-picker-coords').textContent = `SELECTED: ${currentTargetLat.toFixed(5)}, ${currentTargetLng.toFixed(5)}`;
    document.getElementById('btn-confirm-gps').removeAttribute('disabled');

    document.getElementById('gps-picker-modal').classList.add('active');

    // Force Leaflet recalculation on container size changes and focus camera on starting point
    setTimeout(() => {
        if (pickerMap) {
            pickerMap.invalidateSize();
            pickerMap.setView([startLat, startLng], 15);
        }
    }, 200);
}

// Sync current instruction list preview into Visual Nodes array on open
function syncPreviewToVisualNodes() {
    visualNodes = currentPreviewInstructions.map((inst, index) => {
        return {
            id: 'node_' + Date.now() + '_' + index + '_' + Math.random().toString(36).substr(2, 5),
            type: inst.type === 0 ? (inst.ignitionValue ? 'ignition-on' : 'ignition-off') : (inst.type === 1 ? 'delay' : 'drive'),
            durationSeconds: inst.durationSeconds || 10,
            latitude: inst.latitude || 56.2084,
            longitude: inst.longitude || 10.0359,
            speed: inst.speed || 60
        };
    });
}

// Render node items and connection arrows sequentially in the visual workspace
function renderDesignerBoard() {
    const board = document.getElementById('designer-board');
    board.innerHTML = '';

    if (visualNodes.length === 0) {
        board.innerHTML = '<div class="empty-state" style="color: var(--text-muted); margin-top: 100px;">Drag or add commands from the toolbar to start building your workflow.</div>';
        return;
    }

    visualNodes.forEach((node, index) => {
        // Draw connector arrow before rendering subsequent nodes
        if (index > 0) {
            const connector = document.createElement('div');
            connector.className = 'wf-connector';
            board.appendChild(connector);
        }

        const nodeEl = document.createElement('div');
        nodeEl.className = 'wf-node';
        nodeEl.setAttribute('data-id', node.id);

        let bodyContent = '';
        let headerTitle = '';

        if (node.type === 'ignition-on') {
            headerTitle = 'IGNITION CONTROLLER';
            bodyContent = `<label>IGNITION STATE</label>
                           <div style="font-weight: bold; color: var(--led-green);">FORCE ON (TRUE)</div>`;
        } else if (node.type === 'ignition-off') {
            headerTitle = 'IGNITION CONTROLLER';
            bodyContent = `<label>IGNITION STATE</label>
                           <div style="font-weight: bold; color: var(--led-gray);">FORCE OFF (FALSE)</div>`;
        } else if (node.type === 'delay') {
            headerTitle = 'WAIT DELAY';
            bodyContent = `<label>WAIT DURATION (SEC)</label>
                           <input type="number" value="${node.durationSeconds}" class="node-input" data-prop="durationSeconds" style="padding: 4px; font-size: 11px;">`;
        } else if (node.type === 'drive') {
            headerTitle = 'DRIVE MOVEMENT';
            bodyContent = `<label>TARGET COORDINATES</label>
                           <div style="display:flex; gap:4px; align-items: center;">
                             <input type="number" step="0.0001" value="${node.latitude.toFixed(5)}" class="node-input" data-prop="latitude" style="padding: 4px; font-size: 11px; flex:1;" id="lat-${node.id}">
                             <input type="number" step="0.0001" value="${node.longitude.toFixed(5)}" class="node-input" data-prop="longitude" style="padding: 4px; font-size: 11px; flex:1;" id="lng-${node.id}">
                             <button class="btn btn-outline btn-picker btn-node-gps" data-node-id="${node.id}" title="Pick from Map" style="padding: 4px 6px; display: flex; align-items: center; justify-content: center; height: 25px;">
                               <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m12 2v4M12 18v4M4 12h4M16 12h4"/></svg>
                             </button>
                           </div>
                           <label class="mt-1">SPEED REGULATOR (KM/H)</label>
                           <input type="number" value="${node.speed}" class="node-input" data-prop="speed" style="padding: 4px; font-size: 11px;">`;
        }

        nodeEl.innerHTML = `
            <div class="wf-node-header">
                <span class="node-title">${headerTitle}</span>
                <span class="btn-close-node" onclick="removeVisualNode('${node.id}')">&times;</span>
            </div>
            <div class="wf-node-body">
                ${bodyContent}
            </div>
        `;

        // Bind input listeners to modify underlying state values dynamically
        nodeEl.querySelectorAll('.node-input').forEach(input => {
            input.addEventListener('input', (e) => {
                const prop = e.target.getAttribute('data-prop');
                const val = parseFloat(e.target.value);
                if (!isNaN(val)) {
                    node[prop] = val;
                }
            });
        });

        const gpsBtn = nodeEl.querySelector('.btn-node-gps');
        if (gpsBtn) {
            gpsBtn.addEventListener('click', () => {
                gpsPickerMode = 'node';
                openGpsPickerForNode(node.id);
            });
        }

        board.appendChild(nodeEl);
    });
}

// Add new nodes from visual designer toolbar buttons
document.querySelectorAll('.tool-node').forEach(btn => {
    btn.addEventListener('click', (e) => {
        const type = e.target.getAttribute('data-type');
        
        let newLat = 56.2084;
        let newLng = 10.0359;
        
        // Seed coordinate from the preceding drive node or initial location if available
        let precedingLat = null;
        let precedingLng = null;
        for (let i = visualNodes.length - 1; i >= 0; i--) {
            if (visualNodes[i].type === 'drive') {
                precedingLat = visualNodes[i].latitude;
                precedingLng = visualNodes[i].longitude;
                break;
            }
        }
        
        if (precedingLat !== null && precedingLng !== null) {
            newLat = precedingLat;
            newLng = precedingLng;
        } else {
            const initLat = parseFloat(document.getElementById('init-lat').value);
            const initLng = parseFloat(document.getElementById('init-lng').value);
            if (!isNaN(initLat) && !isNaN(initLng)) {
                newLat = initLat;
                newLng = initLng;
            }
        }

        visualNodes.push({
            id: 'node_' + Date.now() + '_' + Math.random().toString(36).substr(2, 5),
            type: type,
            durationSeconds: 10,
            latitude: newLat,
            longitude: newLng,
            speed: 60
        });

        renderDesignerBoard();
    });
});

// Remove individual visual workflow nodes
window.removeVisualNode = function(id) {
    visualNodes = visualNodes.filter(n => n.id !== id);
    renderDesignerBoard();
};

// Map Visual Nodes back to preview queue array on Apply Click & create tracker immediately
document.getElementById('btn-apply-workflow').addEventListener('click', async () => {
    currentPreviewInstructions = visualNodes.map(node => {
        let type = 0;
        let ignitionValue = null;
        
        if (node.type === 'ignition-on') {
            type = 0;
            ignitionValue = true;
        } else if (node.type === 'ignition-off') {
            type = 0;
            ignitionValue = false;
        } else if (node.type === 'delay') {
            type = 1;
        } else if (node.type === 'drive') {
            type = 2;
        }

        return {
            type: type,
            ignitionValue: ignitionValue,
            durationSeconds: node.durationSeconds,
            latitude: node.latitude,
            longitude: node.longitude,
            speed: node.speed
        };
    });

    const imei = document.getElementById('new-imei').value.trim();
    if (!imei || imei.length < 5) {
        alert('Please specify a valid IMEI number.');
        return;
    }

    const initLat = parseFloat(document.getElementById('init-lat').value);
    const initLng = parseFloat(document.getElementById('init-lng').value);

    const payload = {
        imei: imei,
        latitude: isNaN(initLat) ? null : initLat,
        longitude: isNaN(initLng) ? null : initLng,
        instructions: currentPreviewInstructions
    };

    const res = await fetch('/api/trackers', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        document.getElementById('new-imei').value = '';
        currentPreviewInstructions = [];
        updatePreviewList();
        if (mainPreviewPolyline) {
            map.removeLayer(mainPreviewPolyline);
            mainPreviewPolyline = null;
        }
        drawMainPreviewRoute();
        loadTrackers();

        const wfModal = document.getElementById('workflow-modal');
        if (wfModal) {
            wfModal.classList.remove('active');
        }
    } else {
        const text = await res.text();
        alert('Error creating tracker: ' + text);
    }
});

function updatePreviewList() {
    const list = document.getElementById('instruction-list-preview');
    if (!list) return;
    list.innerHTML = '';
    
    if (currentPreviewInstructions.length === 0) {
        list.innerHTML = '<li class="empty-state">No commands queued.</li>';
        return;
    }

    currentPreviewInstructions.forEach((inst, index) => {
        const li = document.createElement('li');
        if (inst.type === 0) {
            li.textContent = `[${index + 1}] IGNITION = ${inst.ignitionValue ? 'ON' : 'OFF'}`;
        } else if (inst.type === 1) {
            li.textContent = `[${index + 1}] DELAY: ${inst.durationSeconds}s`;
        } else if (inst.type === 2) {
            li.textContent = `[${index + 1}] DRIVE: (${inst.latitude.toFixed(4)}, ${inst.longitude.toFixed(4)}) @ ${inst.speed} km/h`;
        }
        list.appendChild(li);
    });

    list.scrollTop = list.scrollHeight;
}

// Fetch trackers and update UI loop
async function loadTrackers() {
    const res = await fetch('/api/trackers');
    if (!res.ok) return;

    const allTrackers = await res.json();
    document.getElementById('active-count').textContent = allTrackers.filter(t => t.isRunning).length;

    // Apply Search Filter (by IMEI)
    const searchVal = document.getElementById('search-filter').value.trim().toLowerCase();
    let trackers = allTrackers.filter(t => t.imei.toLowerCase().includes(searchVal));

    // Apply Status Filter
    if (activeStatusFilter === 'running') {
        trackers = trackers.filter(t => t.isRunning);
    } else if (activeStatusFilter === 'stopped') {
        trackers = trackers.filter(t => !t.isRunning);
    }

    const grid = document.getElementById('tracker-cards-grid');

    // 1. Remove cards that are not in the filtered list
    const filteredImeis = trackers.map(t => t.imei);
    const existingCards = grid.querySelectorAll('.tracker-card');
    existingCards.forEach(card => {
        const imei = card.getAttribute('data-imei');
        if (!filteredImeis.includes(imei)) {
            card.remove();
        }
    });

    // 2. Add or update cards in place
    for (const tracker of trackers) {
        let card = grid.querySelector(`.tracker-card[data-imei="${tracker.imei}"]`);
        const isNew = !card;

        if (isNew) {
            card = document.createElement('div');
            card.className = 'tracker-card';
            card.setAttribute('data-imei', tracker.imei);
            grid.appendChild(card);
        }

        // Apply running class toggle in place
        card.className = `tracker-card ${tracker.isRunning ? 'running' : ''}`;

        // Build status led class
        let ledClass = 'stopped-led';
        if (tracker.isRunning) {
            ledClass = tracker.speed > 0 ? 'online-led' : 'idle-led';
        }



        // Build instructions list html
        let taskListHtml = '';
        if (tracker.instructions && tracker.instructions.length > 0) {
            tracker.instructions.forEach((inst, index) => {
                // Determine if this task is within the sliding window: 
                // 1 previous task, the current active task, and 1 next task
                const cur = tracker.currentInstructionIndex;
                const isRunning = tracker.isRunning;
                
                const showInRunning = isRunning && (index === cur || index === cur - 1 || index === cur + 1);
                const showInStopped = !isRunning && index < 3; // Show first 3 tasks if stopped
                
                if (!showInRunning && !showInStopped) {
                    return; // Skip rendering
                }

                let statusClass = '';
                if (isRunning) {
                    if (index === cur) {
                        statusClass = 'active';
                    } else if (index < cur) {
                        statusClass = 'completed';
                    }
                }
                
                let text = '';
                if (inst.type === 0) {
                    text = `IGNITION = ${inst.ignitionValue ? 'ON' : 'OFF'}`;
                } else if (inst.type === 1) {
                    text = `DELAY: ${inst.durationSeconds}s`;
                } else if (inst.type === 2) {
                    text = `DRIVE: (${inst.latitude.toFixed(4)}, ${inst.longitude.toFixed(4)})`;
                }
                
                taskListHtml += `<div class="task-item ${statusClass}">
                    <span>${index + 1}. ${text}</span>
                    <span>${index === cur && isRunning ? 'ACTIVE' : ''}</span>
                </div>`;
            });
        } else {
            taskListHtml = '<div class="task-item">No tasks queued</div>';
        }

        // Set interior HTML. Doing this in-place preserves selection/focus elsewhere and prevents grid-layout flashing/reflows
        card.innerHTML = `
            <div class="card-header">
                <div class="card-header-left">
                    <span class="indicator-led ${ledClass}"></span>
                    <span class="card-imei">IMEI: ${tracker.imei}</span>
                </div>
            </div>
            <div class="card-body">
                <div class="status-row">
                    <span class="status-value">${tracker.instructionStatus}</span>
                </div>
                
                <div class="progress-container">
                    <div class="progress-bar" style="width: ${tracker.isRunning ? tracker.progressPercentage : 0}%"></div>
                    <span class="progress-text">${tracker.isRunning ? tracker.progressPercentage.toFixed(0) : 0}% COMPLETE</span>
                </div>

                <div class="task-history">
                    <div class="task-history-title">SEQUENTIAL TASK HISTORY</div>
                    ${taskListHtml}
                </div>

                <div class="telemetry-grid">
                    <div class="telemetry-item">
                        <span class="tel-label">LATITUDE</span>
                        <span class="tel-val">${tracker.latitude.toFixed(5)}</span>
                    </div>
                    <div class="telemetry-item">
                        <span class="tel-label">LONGITUDE</span>
                        <span class="tel-val">${tracker.longitude.toFixed(5)}</span>
                    </div>
                    <div class="telemetry-item">
                        <span class="tel-label">SPEED</span>
                        <span class="tel-val">${tracker.speed.toFixed(1)} km/h</span>
                    </div>
                    <div class="telemetry-item">
                        <span class="tel-label">IGNITION</span>
                        <span class="tel-val ${tracker.ignition ? 'text-green' : ''}">${tracker.ignition ? 'ON' : 'OFF'}</span>
                    </div>
                </div>
            </div>
            <div class="card-footer">
                ${tracker.isRunning ? 
                    `<button onclick="stopTracker('${tracker.imei}')" class="btn btn-danger">HALT</button>` :
                    `<button onclick="startTracker('${tracker.imei}')" class="btn btn-primary">RUN</button>`
                }
                <button onclick="removeTracker('${tracker.imei}')" class="btn btn-outline">REMOVE</button>
            </div>
        `;

        // Update Map Marker
        updateMapMarker(tracker);
    }

    // Clean up markers for removed trackers
    Object.keys(trackerMarkers).forEach(imei => {
        if (!trackers.some(t => t.imei === imei)) {
            map.removeLayer(trackerMarkers[imei]);
            delete trackerMarkers[imei];
            if (trackerPaths[imei]) {
                map.removeLayer(trackerPaths[imei]);
                delete trackerPaths[imei];
            }
        }
    });
}

function updateMapMarker(tracker) {
    const pos = [tracker.latitude, tracker.longitude];
    
    if (trackerMarkers[tracker.imei]) {
        trackerMarkers[tracker.imei].setLatLng(pos);
    } else {
        const marker = L.marker(pos).addTo(map)
            .bindPopup(`<b>IMEI: ${tracker.imei}</b><br>Status: ${tracker.instructionStatus}`);
        trackerMarkers[tracker.imei] = marker;
    }

    // Draw route path if instructions contain MoveTo points
    const points = [[tracker.latitude, tracker.longitude]];
    tracker.instructions.forEach(inst => {
        if (inst.type === 2 && inst.latitude && inst.longitude) {
            points.push([inst.latitude, inst.longitude]);
        }
    });

    if (points.length > 1) {
        if (trackerPaths[tracker.imei]) {
            trackerPaths[tracker.imei].setLatLngs(points);
        } else {
            const polyline = L.polyline(points, { color: '#007acc', weight: 3, dashArray: '5, 5' }).addTo(map);
            trackerPaths[tracker.imei] = polyline;
        }
    }
}

async function startTracker(imei) {
    await fetch(`/api/trackers/${imei}/start`, { method: 'POST' });
    loadTrackers();
}

async function stopTracker(imei) {
    await fetch(`/api/trackers/${imei}/stop`, { method: 'POST' });
    loadTrackers();
}

async function removeTracker(imei) {
    if (confirm(`Are you sure you want to remove tracker ${imei}?`)) {
        await fetch(`/api/trackers/${imei}`, { method: 'DELETE' });
        loadTrackers();
    }
}

// Global Filter states
let activeStatusFilter = 'all';
let viewLayoutMode = 'list';

// Search & Filter event listeners
document.getElementById('search-filter').addEventListener('input', () => {
    loadTrackers();
});

document.querySelectorAll('.filter-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
        document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');
        activeStatusFilter = e.target.getAttribute('data-filter');
        loadTrackers();
    });
});

// Layout Switcher listeners
const btnLayoutGrid = document.getElementById('btn-layout-grid');
const btnLayoutList = document.getElementById('btn-layout-list');

btnLayoutGrid.addEventListener('click', () => {
    viewLayoutMode = 'grid';
    btnLayoutGrid.classList.add('active');
    btnLayoutList.classList.remove('active');
    document.getElementById('tracker-cards-grid').classList.remove('list-view');
    loadTrackers();
});

btnLayoutList.addEventListener('click', () => {
    viewLayoutMode = 'list';
    btnLayoutList.classList.add('active');
    btnLayoutGrid.classList.remove('active');
    document.getElementById('tracker-cards-grid').classList.add('list-view');
    loadTrackers();
});

// Initial Load & polling interval
loadTrackers();
setInterval(loadTrackers, 1000);
