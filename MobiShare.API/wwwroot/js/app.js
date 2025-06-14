// MobiShare.API/wwwroot/js/app.js

// Global state
let currentUser = null;
let authToken = null;
let activeRide = null;
let rideTimer = null;
let userLocation = null;

// API Base URL
const API_BASE = '/api';

// Initialize app
document.addEventListener('DOMContentLoaded', function() {
    initializeApp();
    setupEventListeners();
});

// App initialization
function initializeApp() {
    // Check for existing auth token
    authToken = localStorage.getItem('authToken');
    if (authToken) {
        loadUserProfile();
    } else {
        showAuthSection();
    }
}

// Event listeners setup
function setupEventListeners() {
    // Auth forms
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    document.getElementById('registerForm').addEventListener('submit', handleRegister);
    
    // Auto-refresh active ride
    setInterval(checkActiveRide, 30000); // Check every 30 seconds
}

// Authentication functions
function showLogin() {
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
    document.querySelector('.tab-btn:nth-child(1)').classList.add('active');
    document.querySelector('.tab-btn:nth-child(2)').classList.remove('active');
}

function showRegister() {
    document.getElementById('loginForm').style.display = 'none';
    document.getElementById('registerForm').style.display = 'block';
    document.querySelector('.tab-btn:nth-child(1)').classList.remove('active');
    document.querySelector('.tab-btn:nth-child(2)').classList.add('active');
}

async function handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('loginUsername').value;
    const password = document.getElementById('loginPassword').value;
    
    try {
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });
        
        if (response.ok) {
            const data = await response.json();
            authToken = data.token;
            currentUser = data.user;
            
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
            
            showNotification('Login effettuato con successo!', 'success');
            showMainSection();
        } else {
            const error = await response.text();
            showNotification('Credenziali non valide', 'error');
        }
    } catch (error) {
        showNotification('Errore di connessione', 'error');
        console.error('Login error:', error);
    }
}

async function handleRegister(e) {
    e.preventDefault();
    
    const username = document.getElementById('registerUsername').value;
    const email = document.getElementById('registerEmail').value;
    const password = document.getElementById('registerPassword').value;
    const tipo = parseInt(document.getElementById('userType').value);
    
    try {
        const response = await fetch(`${API_BASE}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, email, password, tipo })
        });
        
        if (response.ok) {
            showNotification('Registrazione completata! Effettua il login.', 'success');
            showLogin();
            // Clear form
            document.getElementById('registerForm').reset();
        } else {
            const error = await response.text();
            showNotification('Errore nella registrazione: ' + error, 'error');
        }
    } catch (error) {
        showNotification('Errore di connessione', 'error');
        console.error('Register error:', error);
    }
}

function logout() {
    authToken = null;
    currentUser = null;
    activeRide = null;
    
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    
    if (rideTimer) {
        clearInterval(rideTimer);
        rideTimer = null;
    }
    
    showAuthSection();
    showNotification('Logout effettuato', 'success');
}

// UI Navigation
function showAuthSection() {
    document.getElementById('authSection').style.display = 'block';
    document.getElementById('mainSection').style.display = 'none';
}

function showMainSection() {
    document.getElementById('authSection').style.display = 'none';
    document.getElementById('mainSection').style.display = 'block';
    
    updateUserInfo();
    showTab('mezzi');
    loadMezzi();
    checkActiveRide();
    
    // Show gestore features if applicable
    if (currentUser && currentUser.tipo === 1) { // Gestore
        document.querySelectorAll('.gestore-only').forEach(el => {
            el.style.display = 'block';
        });
    }
}

function updateUserInfo() {
    if (currentUser) {
        document.getElementById('username').textContent = currentUser.username;
        document.getElementById('userCredit').textContent = currentUser.credito.toFixed(2);
        document.getElementById('ecoPoints').textContent = currentUser.puntiEco;
        document.getElementById('userInfo').style.display = 'flex';
    }
}

async function loadUserProfile() {
    try {
        const response = await fetch(`${API_BASE}/users/profile`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            currentUser = await response.json();
            localStorage.setItem('currentUser', JSON.stringify(currentUser));
            showMainSection();
        } else {
            logout();
        }
    } catch (error) {
        console.error('Profile load error:', error);
        logout();
    }
}

// Tab navigation
function showTab(tabName) {
    // Hide all tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Hide all nav buttons
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    // Show selected tab
    const tabElement = document.getElementById(tabName + 'Tab');
    if (tabElement) {
        tabElement.classList.add('active');
    }
    
    // Activate nav button
    event.target.classList.add('active');
    
    // Load content based on tab
    switch(tabName) {
        case 'mezzi':
            loadMezzi();
            break;
        case 'parcheggi':
            loadParcheggi();
            break;
        case 'corse':
            loadCorse();
            break;
        case 'profilo':
            loadProfilo();
            break;
        case 'gestione':
            loadGestione();
            break;
    }
}

// Mezzi functions
async function loadMezzi() {
    try {
        let url = `${API_BASE}/vehicles`;
        
        // Add location filter if available
        if (userLocation) {
            url += `?lat=${userLocation.latitude}&lng=${userLocation.longitude}&radius=2`;
        }
        
        const response = await fetch(url);
        const mezzi = await response.json();
        
        displayMezzi(mezzi);
    } catch (error) {
        console.error('Error loading mezzi:', error);
        showNotification('Errore nel caricamento dei mezzi', 'error');
    }
}

function displayMezzi(mezzi) {
    const container = document.getElementById('mezziList');
    container.innerHTML = '';
    
    const filtroTipo = document.getElementById('tipoMezzoFilter')?.value;
    
    const filteredMezzi = mezzi.filter(mezzo => {
        if (filtroTipo && mezzo.tipo.toString() !== filtroTipo) return false;
        return mezzo.stato === 0; // Solo mezzi disponibili
    });
    
    if (filteredMezzi.length === 0) {
        container.innerHTML = '<div class="no-results">Nessun mezzo disponibile</div>';
        return;
    }
    
    filteredMezzi.forEach(mezzo => {
        const card = createMezzoCard(mezzo);
        container.appendChild(card);
    });
}

function createMezzoCard(mezzo) {
    const card = document.createElement('div');
    card.className = 'mezzo-card';
    
    const tipoNames = ['Bici Muscolare', 'Bici Elettrica', 'Monopattino'];
    const tipoIcons = ['🚴‍♂️', '🚴‍♀️', '🛴'];
    const statusNames = ['Disponibile', 'In Uso', 'Manutenzione', 'Batteria Scarica'];
    const statusClasses = ['disponibile', 'inuso', 'manutenzione', 'batteriascarica'];
    
    let batteryHtml = '';
    if (mezzo.percentualeBatteria !== null) {
        const batteryClass = mezzo.percentualeBatteria > 50 ? 'battery-high' : 
                           mezzo.percentualeBatteria > 20 ? 'battery-medium' : 'battery-low';
        batteryHtml = `
            <div class="battery-indicator">
                🔋 ${mezzo.percentualeBatteria}%
                <div class="battery-bar">
                    <div class="battery-fill ${batteryClass}" style="width: ${mezzo.percentualeBatteria}%"></div>
                </div>
            </div>
        `;
    }
    
    card.innerHTML = `
        <div class="mezzo-header">
            <div class="mezzo-icon">${tipoIcons[mezzo.tipo]}</div>
            <div class="mezzo-status status-${statusClasses[mezzo.stato]}">
                ${statusNames[mezzo.stato]}
            </div>
        </div>
        <div class="mezzo-info">
            <div class="info-row">
                <strong>Modello:</strong>
                <span>${mezzo.modello}</span>
            </div>
            <div class="info-row">
                <strong>Tipo:</strong>
                <span>${tipoNames[mezzo.tipo]}</span>
            </div>
            <div class="info-row">
                <strong>Tariffa:</strong>
                <span>€${mezzo.tariffaPerMinuto}/min</span>
            </div>
            ${batteryHtml}
        </div>
        ${mezzo.stato === 0 ? `
            <button onclick="startRide('${mezzo.id}')" class="btn btn-primary">
                <i class="fas fa-play"></i> Inizia Corsa
            </button>
        ` : ''}
    `;
    
    return card;
}

function filterMezzi() {
    loadMezzi();
}

function getCurrentLocation() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                userLocation = {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                };
                showNotification('Posizione aggiornata', 'success');
                loadMezzi();
            },
            (error) => {
                showNotification('Impossibile ottenere la posizione', 'error');
                console.error('Geolocation error:', error);
            }
        );
    } else {
        showNotification('Geolocalizzazione non supportata', 'error');
    }
}

// Ride functions
async function startRide(mezzoId) {
    if (activeRide) {
        showNotification('Hai già una corsa attiva', 'warning');
        return;
    }
    
    if (currentUser.credito < 2) {
        showNotification('Credito insufficiente (minimo €2.00)', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/rides/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ vehicleId: mezzoId })
        });
        
        if (response.ok) {
            const ride = await response.json();
            activeRide = ride;
            showNotification('Corsa iniziata!', 'success');
            
            // Refresh mezzi list and show active ride
            loadMezzi();
            showTab('corse');
            displayActiveRide();
            startRideTimer();
        } else {
            const error = await response.text();
            showNotification('Errore nell\'avvio della corsa: ' + error, 'error');
        }
    } catch (error) {
        console.error('Start ride error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function endRide() {
    if (!activeRide) return;
    
    const destinationParkingId = document.getElementById('destinationParking').value;
    if (!destinationParkingId) {
        showNotification('Seleziona un parcheggio di destinazione', 'warning');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/rides/${activeRide.id}/end`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ destinationParkingId })
        });
        
        if (response.ok) {
            const completedRide = await response.json();
            
            // Stop timer
            if (rideTimer) {
                clearInterval(rideTimer);
                rideTimer = null;
            }
            
            activeRide = null;
            document.getElementById('corsaAttiva').style.display = 'none';
            
            showNotification(`Corsa terminata! Costo: €${completedRide.costo.toFixed(2)}`, 'success');
            
            // Update user credit
            currentUser.credito -= completedRide.costo;
            if (completedRide.puntiEcoAccumulati > 0) {
                currentUser.puntiEco += completedRide.puntiEcoAccumulati;
                showNotification(`+${completedRide.puntiEcoAccumulati} punti eco!`, 'success');
            }
            
            updateUserInfo();
            loadCorse();
            loadMezzi();
        } else {
            const error = await response.text();
            showNotification('Errore nel terminare la corsa: ' + error, 'error');
        }
    } catch (error) {
        console.error('End ride error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function checkActiveRide() {
    if (!authToken) return;
    
    try {
        const response = await fetch(`${API_BASE}/rides/active`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            const ride = await response.json();
            activeRide = ride;
            displayActiveRide();
            if (!rideTimer) {
                startRideTimer();
            }
        } else if (response.status === 404) {
            // No active ride
            activeRide = null;
            document.getElementById('corsaAttiva').style.display = 'none';
            if (rideTimer) {
                clearInterval(rideTimer);
                rideTimer = null;
            }
        }
    } catch (error) {
        console.error('Check active ride error:', error);
    }
}

function displayActiveRide() {
    if (!activeRide) return;
    
    document.getElementById('corsaAttiva').style.display = 'block';
    document.getElementById('activeMezzo').textContent = activeRide.vehicleId;
    document.getElementById('activeStart').textContent = new Date(activeRide.dataInizio).toLocaleString();
    document.getElementById('activeParking').textContent = activeRide.parcheggioDiPartenzaId;
    
    // Load destination parkings
    loadDestinationParkings();
}

async function loadDestinationParkings() {
    try {
        const response = await fetch(`${API_BASE}/parkings`);
        const parkings = await response.json();
        
        const select = document.getElementById('destinationParking');
        select.innerHTML = '<option value="">Seleziona parcheggio di destinazione</option>';
        
        parkings.forEach(parking => {
            const option = document.createElement('option');
            option.value = parking.id;
            option.textContent = parking.nome;
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading destination parkings:', error);
    }
}

function startRideTimer() {
    if (rideTimer) clearInterval(rideTimer);
    
    rideTimer = setInterval(() => {
        if (!activeRide) return;
        
        const startTime = new Date(activeRide.dataInizio);
        const now = new Date();
        const elapsed = Math.floor((now - startTime) / 1000);
        
        const hours = Math.floor(elapsed / 3600);
        const minutes = Math.floor((elapsed % 3600) / 60);
        const seconds = elapsed % 60;
        
        const timeStr = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        document.getElementById('rideTimer').textContent = timeStr;
        
        // Calculate estimated cost
        if (activeRide.vehicle) {
            const elapsedMinutes = elapsed / 60;
            const estimatedCost = elapsedMinutes * activeRide.vehicle.tariffaPerMinuto;
            document.getElementById('activeCost').textContent = estimatedCost.toFixed(2);
        }
    }, 1000);
}

// Parcheggi functions
async function loadParcheggi() {
    try {
        const response = await fetch(`${API_BASE}/parkings`);
        const parcheggi = await response.json();
        
        displayParcheggi(parcheggi);
    } catch (error) {
        console.error('Error loading parcheggi:', error);
        showNotification('Errore nel caricamento dei parcheggi', 'error');
    }
}

function displayParcheggi(parcheggi) {
    const container = document.getElementById('parcheggiList');
    container.innerHTML = '';
    
    parcheggi.forEach(parcheggio => {
        const card = createParcheggioCard(parcheggio);
        container.appendChild(card);
    });
}

function createParcheggioCard(parcheggio) {
    const card = document.createElement('div');
    card.className = 'parcheggio-card';
    
    card.innerHTML = `
        <div class="mezzo-header">
            <div class="mezzo-icon">🅿️</div>
            <div class="mezzo-status status-disponibile">
                ${parcheggio.slotsDisponibili}/${parcheggio.capacita} liberi
            </div>
        </div>
        <div class="mezzo-info">
            <div class="info-row">
                <strong>Nome:</strong>
                <span>${parcheggio.nome}</span>
            </div>
            <div class="info-row">
                <strong>Capacità:</strong>
                <span>${parcheggio.capacita} slot</span>
            </div>
            <div class="info-row">
                <strong>Disponibili:</strong>
                <span>${parcheggio.slotsDisponibili} slot</span>
            </div>
        </div>
        <button onclick="viewParcheggioDetails('${parcheggio.id}')" class="btn btn-outline">
            <i class="fas fa-eye"></i> Dettagli
        </button>
    `;
    
    return card;
}

async function viewParcheggioDetails(parcheggioId) {
    try {
        const response = await fetch(`${API_BASE}/parkings/${parcheggioId}`);
        const parcheggio = await response.json();
        
        // Create modal or expand card to show slots
        showParcheggioModal(parcheggio);
    } catch (error) {
        console.error('Error loading parcheggio details:', error);
        showNotification('Errore nel caricamento dei dettagli', 'error');
    }
}

function showParcheggioModal(parcheggio) {
    const slotsHtml = parcheggio.slots.map(slot => {
        const colorClass = slot.coloreLED === 0 ? 'led-verde' : 
                          slot.coloreLED === 1 ? 'led-rosso' : 'led-giallo';
        return `
            <div class="led-slot">
                <div class="slot-led ${colorClass}"></div>
                Slot ${slot.numero}
            </div>
        `;
    }).join('');
    
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.innerHTML = `
        <div class="modal-content">
            <div class="modal-header">
                <h3>${parcheggio.nome}</h3>
                <button onclick="closeModal()" class="btn btn-outline">×</button>
            </div>
            <div class="modal-body">
                <div class="slots-indicator">
                    ${slotsHtml}
                </div>
                <div class="mezzi-presenti">
                    <h4>Mezzi Presenti:</h4>
                    ${parcheggio.mezziPresenti.map(mezzo => `
                        <div class="mezzo-item">${mezzo.modello} (${['Bici Muscolare', 'Bici Elettrica', 'Monopattino'][mezzo.tipo]})</div>
                    `).join('')}
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
}

function closeModal() {
    const modal = document.querySelector('.modal');
    if (modal) {
        modal.remove();
    }
}

// Corse functions
async function loadCorse() {
    await checkActiveRide();
    
    try {
        const response = await fetch(`${API_BASE}/rides/history`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        const corse = await response.json();
        displayCorse(corse);
    } catch (error) {
        console.error('Error loading corse:', error);
        showNotification('Errore nel caricamento delle corse', 'error');
    }
}

function displayCorse(corse) {
    const container = document.getElementById('corseList');
    container.innerHTML = '';
    
    if (corse.length === 0) {
        container.innerHTML = '<div class="no-results">Nessuna corsa trovata</div>';
        return;
    }
    
    corse.forEach(corsa => {
        const item = createCorsaItem(corsa);
        container.appendChild(item);
    });
}

function createCorsaItem(corsa) {
    const item = document.createElement('div');
    item.className = 'ride-item';
    
    const statusNames = ['In Corso', 'Completata', 'Annullata'];
    const tipoIcons = ['🚴‍♂️', '🚴‍♀️', '🛴'];
    
    const pointsHtml = corsa.puntiEcoAccumulati > 0 ? 
        `<div class="ride-points">+${corsa.puntiEcoAccumulati} eco</div>` : '';
    
    item.innerHTML = `
        <div class="ride-icon">${tipoIcons[corsa.vehicle?.tipo || 0]}</div>
        <div class="ride-details">
            <h4>${corsa.vehicle?.modello || corsa.vehicleId}</h4>
            <div class="ride-meta">
                ${new Date(corsa.dataInizio).toLocaleString()}
                ${corsa.dataFine ? ` - ${new Date(corsa.dataFine).toLocaleString()}` : ''}
                <br>
                ${corsa.durata > 0 ? `Durata: ${corsa.durata} min` : ''}
                | Stato: ${statusNames[corsa.stato]}
            </div>
        </div>
        <div class="ride-cost">€${corsa.costo.toFixed(2)}</div>
        ${pointsHtml}
    `;
    
    return item;
}

// Profile functions
async function loadProfilo() {
    await loadUserProfile();
    
    document.getElementById('profileUsername').textContent = currentUser.username;
    document.getElementById('profileEmail').textContent = currentUser.email;
    document.getElementById('profileTipo').textContent = currentUser.tipo === 0 ? 'Cliente' : 'Gestore';
    document.getElementById('profileDataRegistrazione').textContent = new Date(currentUser.dataRegistrazione).toLocaleDateString();
    document.getElementById('profileCredito').textContent = currentUser.credito.toFixed(2);
    document.getElementById('profilePuntiEco').textContent = currentUser.puntiEco;
    
    await loadVouchers();
}

async function loadVouchers() {
    try {
        const response = await fetch(`${API_BASE}/users/vouchers`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        const vouchers = await response.json();
        displayVouchers(vouchers);
    } catch (error) {
        console.error('Error loading vouchers:', error);
    }
}

function displayVouchers(vouchers) {
    const container = document.getElementById('vouchersList');
    container.innerHTML = '';
    
    if (vouchers.length === 0) {
        container.innerHTML = '<div class="no-results">Nessun buono sconto disponibile</div>';
        return;
    }
    
    vouchers.forEach(voucher => {
        const item = document.createElement('div');
        item.className = 'voucher-item';
        
        item.innerHTML = `
            <div>
                <div class="voucher-value">€${voucher.valore.toFixed(2)}</div>
                <div class="voucher-expiry">Scade: ${new Date(voucher.dataScadenza).toLocaleDateString()}</div>
            </div>
            <button onclick="useVoucher('${voucher.id}')" class="btn btn-success">Usa</button>
        `;
        
        container.appendChild(item);
    });
}

async function ricaricaCredito() {
    const amount = parseFloat(document.getElementById('rechargeAmount').value);
    
    if (!amount || amount < 5 || amount > 100) {
        showNotification('Importo non valido (min €5, max €100)', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/users/credito`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ importo: amount })
        });
        
        if (response.ok) {
            currentUser.credito += amount;
            updateUserInfo();
            document.getElementById('profileCredito').textContent = currentUser.credito.toFixed(2);
            document.getElementById('rechargeAmount').value = '';
            showNotification(`Credito ricaricato: +€${amount.toFixed(2)}`, 'success');
        } else {
            showNotification('Errore nella ricarica', 'error');
        }
    } catch (error) {
        console.error('Recharge error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function convertiPunti() {
    const points = parseInt(document.getElementById('pointsToConvert').value);
    
    if (!points || points < 100 || points % 100 !== 0) {
        showNotification('Inserire un multiplo di 100 punti (min 100)', 'error');
        return;
    }
    
    if (points > currentUser.puntiEco) {
        showNotification('Punti insufficienti', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/users/convert-points`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ puntiDaConvertire: points })
        });
        
        if (response.ok) {
            const result = await response.json();
            currentUser.puntiEco -= points;
            
            updateUserInfo();
            document.getElementById('profilePuntiEco').textContent = currentUser.puntiEco;
            document.getElementById('pointsToConvert').value = '';
            
            showNotification(`Buono sconto creato: €${result.valore.toFixed(2)}`, 'success');
            loadVouchers();
        } else {
            showNotification('Errore nella conversione', 'error');
        }
    } catch (error) {
        console.error('Convert points error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function useVoucher(voucherId) {
    try {
        const response = await fetch(`${API_BASE}/users/use-voucher`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ voucherId })
        });
        
        if (response.ok) {
            showNotification('Buono sconto utilizzato!', 'success');
            loadUserProfile();
            loadVouchers();
        } else {
            showNotification('Errore nell\'utilizzo del buono', 'error');
        }
    } catch (error) {
        console.error('Use voucher error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Management functions (Gestore only)
async function loadGestione() {
    if (currentUser.tipo !== 1) return;
    
    await loadSystemStats();
    await loadLEDMonitor();
}

async function loadSystemStats() {
    try {
        const [mezziResponse, parcheggiResponse] = await Promise.all([
            fetch(`${API_BASE}/vehicles`),
            fetch(`${API_BASE}/parkings`)
        ]);
        
        const mezzi = await mezziResponse.json();
        const parcheggi = await parcheggiResponse.json();
        
        const totalMezzi = mezzi.length;
        const mezziInUso = mezzi.filter(m => m.stato === 1).length;
        const mezziDisponibili = mezzi.filter(m => m.stato === 0).length;
        
        document.getElementById('totalMezzi').textContent = totalMezzi;
        document.getElementById('mezziInUso').textContent = mezziInUso;
        document.getElementById('mezziDisponibili').textContent = mezziDisponibili;
        document.getElementById('totalParcheggi').textContent = parcheggi.length;
    } catch (error) {
        console.error('Error loading system stats:', error);
    }
}

async function loadLEDMonitor() {
    try {
        const response = await fetch(`${API_BASE}/parkings`);
        const parcheggi = await response.json();
        
        const container = document.getElementById('ledMonitor');
        container.innerHTML = '';
        
        for (const parcheggio of parcheggi) {
            const detailResponse = await fetch(`${API_BASE}/parkings/${parcheggio.id}`);
            const details = await detailResponse.json();
            
            const parkingDiv = document.createElement('div');
            parkingDiv.className = 'parking-leds';
            
            const slotsHtml = details.slots.map(slot => {
                const colorClass = slot.coloreLED === 0 ? 'led-verde' : 
                                  slot.coloreLED === 1 ? 'led-rosso' : 'led-giallo';
                return `
                    <div class="led-slot">
                        <div class="slot-led ${colorClass}"></div>
                        ${slot.numero}
                    </div>
                `;
            }).join('');
            
            parkingDiv.innerHTML = `
                <div class="parking-name">${details.nome}</div>
                <div class="leds-grid">${slotsHtml}</div>
            `;
            
            container.appendChild(parkingDiv);
        }
    } catch (error) {
        console.error('Error loading LED monitor:', error);
    }
}

function showAddMezzoForm() {
    document.getElementById('addMezzoForm').style.display = 'block';
    loadParcheggiOptions();
}

function hideAddMezzoForm() {
    document.getElementById('addMezzoForm').style.display = 'none';
    document.getElementById('addMezzoForm').querySelector('form').reset();
}

async function loadParcheggiOptions() {
    try {
        const response = await fetch(`${API_BASE}/parkings`);
        const parcheggi = await response.json();
        
        const select = document.getElementById('newMezzoParcheggio');
        select.innerHTML = '<option value="">Seleziona parcheggio</option>';
        
        parcheggi.forEach(parcheggio => {
            const option = document.createElement('option');
            option.value = parcheggio.id;
            option.textContent = parcheggio.nome;
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading parcheggi options:', error);
    }
}

async function aggiungiMezzo() {
    const modello = document.getElementById('newMezzoModello').value;
    const tipo = parseInt(document.getElementById('newMezzoTipo').value);
    const tariffa = parseFloat(document.getElementById('newMezzoTariffa').value);
    const parkingId = document.getElementById('newMezzoParcheggio').value;
    
    if (!modello || !tariffa || !parkingId) {
        showNotification('Compila tutti i campi', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/vehicles`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ tipo, modello, tariffaPerMinuto: tariffa, parkingId })
        });
        
        if (response.ok) {
            showNotification('Mezzo aggiunto con successo', 'success');
            hideAddMezzoForm();
            loadSystemStats();
        } else {
            showNotification('Errore nell\'aggiunta del mezzo', 'error');
        }
    } catch (error) {
        console.error('Add mezzo error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

function showAddParcheggioForm() {
    document.getElementById('addParcheggioForm').style.display = 'block';
}

function hideAddParcheggioForm() {
    document.getElementById('addParcheggioForm').style.display = 'none';
}

async function aggiungiParcheggio() {
    const nome = document.getElementById('newParcheggioNome').value;
    const lat = parseFloat(document.getElementById('newParcheggioLat').value);
    const lng = parseFloat(document.getElementById('newParcheggioLng').value);
    const capacita = parseInt(document.getElementById('newParcheggioCapacita').value);
    
    if (!nome || !lat || !lng || !capacita) {
        showNotification('Compila tutti i campi', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/parkings`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ nome, latitudine: lat, longitudine: lng, capacita })
        });
        
        if (response.ok) {
            showNotification('Parcheggio aggiunto con successo', 'success');
            hideAddParcheggioForm();
            loadSystemStats();
            loadLEDMonitor();
        } else {
            showNotification('Errore nell\'aggiunta del parcheggio', 'error');
        }
    } catch (error) {
        console.error('Add parcheggio error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Utility functions
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;
    
    document.getElementById('notifications').appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 5000);
}

// Additional CSS for modal
const modalCSS = `
.modal {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
}

.modal-content {
    background: white;
    border-radius: 15px;
    padding: 2rem;
    max-width: 600px;
    width: 90%;
    max-height: 80vh;
    overflow-y: auto;
}

.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    border-bottom: 2px solid #f1f3f4;
    padding-bottom: 1rem;
}

.no-results {
    text-align: center;
    padding: 2rem;
    color: #666;
    font-style: italic;
}

.mezzo-item {
    background: #f8f9fa;
    padding: 0.5rem;
    border-radius: 5px;
    margin-bottom: 0.5rem;
}
`;

// Add modal CSS to document
const style = document.createElement('style');
style.textContent = modalCSS;
document.head.appendChild(style);