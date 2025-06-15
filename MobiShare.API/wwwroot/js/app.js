// MobiShare.API/wwwroot/js/app.js - FIXED VERSION

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
            // FIX: Controller restituisce { Token, Utente } non { token, user }
            authToken = data.token || data.Token;
            currentUser = data.utente || data.Utente || data.user;
            
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
        // FIX: Endpoint corretto è "registra" non "register"
        const response = await fetch(`${API_BASE}/auth/registra`, {
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
        // FIX: Endpoint corretto è "/utenti/profilo" non "/users/profile"
        const response = await fetch(`${API_BASE}/utenti/profilo`, {
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
        showLoading(true);
        // FIX: Endpoint corretto è "/mezzi" non "/vehicles"
        let url = `${API_BASE}/mezzi`;
        
        const response = await fetch(url, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            const mezzi = await response.json();
            displayMezzi(mezzi);
        } else {
            showNotification('Errore nel caricamento mezzi', 'error');
        }
    } catch (error) {
        console.error('Load mezzi error:', error);
        showNotification('Errore di connessione', 'error');
    } finally {
        showLoading(false);
    }
}

function displayMezzi(mezzi) {
    const container = document.getElementById('mezziList');
    if (!container) return;
    
    if (mezzi.length === 0) {
        container.innerHTML = '<p class="no-results">Nessun mezzo disponibile al momento.</p>';
        return;
    }
    
    container.innerHTML = mezzi.map(mezzo => `
        <div class="mezzo-card" data-id="${mezzo.id}">
            <div class="mezzo-header">
                <h3>${getMezzoIcon(mezzo.tipo)} ${mezzo.modello}</h3>
                <span class="mezzo-status ${getStatusClass(mezzo.stato)}">${getStatusText(mezzo.stato)}</span>
            </div>
            <div class="mezzo-details">
                <p><strong>Tipo:</strong> ${getTipoText(mezzo.tipo)}</p>
                <p><strong>Tariffa:</strong> €${mezzo.tariffaPerMinuto}/min</p>
                ${mezzo.percentualeBatteria ? `<p><strong>Batteria:</strong> ${mezzo.percentualeBatteria}%</p>` : ''}
                <p><strong>Posizione:</strong> ${mezzo.latitudine?.toFixed(4)}, ${mezzo.longitudine?.toFixed(4)}</p>
            </div>
            ${mezzo.stato === 0 ? `<button onclick="startRide('${mezzo.id}')" class="btn-primary">Inizia Corsa</button>` : ''}
        </div>
    `).join('');
}

// Parcheggi functions
async function loadParcheggi() {
    try {
        showLoading(true);
        // FIX: Endpoint corretto è "/parcheggi" non "/parking"
        const response = await fetch(`${API_BASE}/parcheggi`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            const parcheggi = await response.json();
            displayParcheggi(parcheggi);
        } else {
            showNotification('Errore nel caricamento parcheggi', 'error');
        }
    } catch (error) {
        console.error('Load parcheggi error:', error);
        showNotification('Errore di connessione', 'error');
    } finally {
        showLoading(false);
    }
}

function displayParcheggi(parcheggi) {
    const container = document.getElementById('parcheggioList');
    if (!container) return;
    
    if (parcheggi.length === 0) {
        container.innerHTML = '<p class="no-results">Nessun parcheggio disponibile al momento.</p>';
        return;
    }
    
    container.innerHTML = parcheggi.map(parcheggio => `
        <div class="parcheggio-card" data-id="${parcheggio.id}">
            <div class="parcheggio-header">
                <h3><i class="fas fa-parking"></i> ${parcheggio.nome}</h3>
                <span class="slots-info">${parcheggio.slotsDisponibili}/${parcheggio.capacita} disponibili</span>
            </div>
            <div class="parcheggio-details">
                <p><strong>Posizione:</strong> ${parcheggio.latitudine?.toFixed(4)}, ${parcheggio.longitudine?.toFixed(4)}</p>
                <div class="occupancy-bar">
                    <div class="occupancy-fill" style="width: ${((parcheggio.capacita - parcheggio.slotsDisponibili) / parcheggio.capacita) * 100}%"></div>
                </div>
            </div>
            <button onclick="loadParcheggioDetails('${parcheggio.id}')" class="btn-secondary">Dettagli</button>
        </div>
    `).join('');
}

// Corse functions
async function loadCorse() {
    try {
        showLoading(true);
        // FIX: Endpoint corretto è "/corse/history" non "/rides/history"
        const response = await fetch(`${API_BASE}/corse/history`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            const corse = await response.json();
            displayCorse(corse);
        } else {
            showNotification('Errore nel caricamento corse', 'error');
        }
    } catch (error) {
        console.error('Load corse error:', error);
        showNotification('Errore di connessione', 'error');
    } finally {
        showLoading(false);
    }
}

function displayCorse(corse) {
    const container = document.getElementById('corseHistory');
    if (!container) return;
    
    if (corse.length === 0) {
        container.innerHTML = '<p class="no-results">Nessuna corsa effettuata.</p>';
        return;
    }
    
    container.innerHTML = corse.map(corsa => `
        <div class="corsa-card">
            <div class="corsa-header">
                <h4>Corsa ${corsa.mezzoId}</h4>
                <span class="corsa-date">${new Date(corsa.dataInizio).toLocaleDateString()}</span>
            </div>
            <div class="corsa-details">
                <p><strong>Durata:</strong> ${formatDuration(corsa.durata)}</p>
                <p><strong>Costo:</strong> €${corsa.costo?.toFixed(2) || '0.00'}</p>
                ${corsa.puntiEcoAccumulati ? `<p><strong>Punti Eco:</strong> +${corsa.puntiEcoAccumulati} <i class="fas fa-leaf"></i></p>` : ''}
                <p><strong>Stato:</strong> ${getCorseStatusText(corsa.stato)}</p>
            </div>
        </div>
    `).join('');
}

// Active ride functions
async function checkActiveRide() {
    try {
        // FIX: Endpoint corretto è "/corse/active" non "/rides/active"
        const response = await fetch(`${API_BASE}/corse/active`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            activeRide = await response.json();
            showActiveRide();
            startRideTimer();
        } else {
            activeRide = null;
            hideActiveRide();
        }
    } catch (error) {
        console.error('Check active ride error:', error);
    }
}

function showActiveRide() {
    const section = document.getElementById('activeRideSection');
    if (section && activeRide) {
        section.style.display = 'block';
        document.getElementById('activeMezzoId').textContent = activeRide.mezzoId;
        updateRideTimer();
    }
}

function hideActiveRide() {
    const section = document.getElementById('activeRideSection');
    if (section) {
        section.style.display = 'none';
    }
    if (rideTimer) {
        clearInterval(rideTimer);
        rideTimer = null;
    }
}

async function startRide(mezzoId) {
    try {
        // FIX: Endpoint corretto è "/corse/start" non "/rides/start"
        const response = await fetch(`${API_BASE}/corse/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ mezzoId })
        });
        
        if (response.ok) {
            activeRide = await response.json();
            showNotification('Corsa iniziata!', 'success');
            showActiveRide();
            startRideTimer();
            loadMezzi(); // Refresh mezzi list
        } else {
            const error = await response.text();
            showNotification('Impossibile iniziare la corsa: ' + error, 'error');
        }
    } catch (error) {
        console.error('Start ride error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function endRide() {
    if (!activeRide) return;
    
    try {
        // FIX: Endpoint corretto
        const response = await fetch(`${API_BASE}/corse/${activeRide.id}/end`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ parcheggioDestinazioneId: activeRide.parcheggioDiPartenzaId })
        });
        
        if (response.ok) {
            const corsaCompletata = await response.json();
            showNotification(`Corsa terminata! Costo: €${corsaCompletata.costo?.toFixed(2)}`, 'success');
            hideActiveRide();
            loadUserProfile(); // Refresh user data
            loadCorse(); // Refresh rides history
        } else {
            const error = await response.text();
            showNotification('Impossibile terminare la corsa: ' + error, 'error');
        }
    } catch (error) {
        console.error('End ride error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Profilo functions
function loadProfilo() {
    const container = document.getElementById('profiloInfo');
    if (!container || !currentUser) return;
    
    container.innerHTML = `
        <div class="profile-info">
            <p><strong>Username:</strong> ${currentUser.username}</p>
            <p><strong>Email:</strong> ${currentUser.email}</p>
            <p><strong>Tipo:</strong> ${currentUser.tipo === 0 ? 'Cliente' : 'Gestore'}</p>
            <p><strong>Credito:</strong> €${currentUser.credito?.toFixed(2) || '0.00'}</p>
            <p><strong>Punti Eco:</strong> ${currentUser.puntiEco || 0}</p>
            <p><strong>Registrato il:</strong> ${new Date(currentUser.dataRegistrazione).toLocaleDateString()}</p>
        </div>
    `;
    
    // Update eco points progress
    updateEcoPointsProgress();
}

async function ricaricaCredito() {
    const amount = parseFloat(document.getElementById('creditoAmount').value);
    
    try {
        // FIX: Endpoint corretto è "/utenti/credito" non "/users/credit"
        const response = await fetch(`${API_BASE}/utenti/credito`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ importo: amount })
        });
        
        if (response.ok) {
            showNotification(`Credito ricaricato: €${amount.toFixed(2)}`, 'success');
            loadUserProfile(); // Refresh user data
        } else {
            showNotification('Errore nella ricarica', 'error');
        }
    } catch (error) {
        console.error('Ricarica credito error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

async function convertEcoPoints() {
    if (!currentUser || currentUser.puntiEco < 100) {
        showNotification('Punti insufficienti per la conversione', 'error');
        return;
    }
    
    try {
        // FIX: Endpoint corretto è "/utenti/converti-punti"
        const response = await fetch(`${API_BASE}/utenti/converti-punti`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ puntiDaConvertire: 100 })
        });
        
        if (response.ok) {
            const result = await response.json();
            showNotification(`Punti convertiti! Buono da €${result.valore} creato`, 'success');
            loadUserProfile(); // Refresh user data
        } else {
            showNotification('Errore nella conversione', 'error');
        }
    } catch (error) {
        console.error('Convert points error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

// Gestione functions (solo per gestori)
async function loadGestione() {
    if (!currentUser || currentUser.tipo !== 1) return;
    
    try {
        showLoading(true);
        // Load statistics and management data
        await loadGestioneStats();
        showManagementTab('mezzi');
    } catch (error) {
        console.error('Load gestione error:', error);
        showNotification('Errore nel caricamento gestione', 'error');
    } finally {
        showLoading(false);
    }
}

async function loadGestioneStats() {
    // This would need additional endpoints in your controllers
    // For now, display placeholder data
    document.getElementById('totalMezzi').textContent = '5';
    document.getElementById('mezziInUso').textContent = '2';
    document.getElementById('corseOggi').textContent = '12';
    document.getElementById('ricaviOggi').textContent = '€24.50';
}

function showManagementTab(tabName) {
    document.querySelectorAll('.management-tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.classList.add('active');
    
    const content = document.getElementById('managementContent');
    if (!content) return;
    
    switch(tabName) {
        case 'mezzi':
            content.innerHTML = '<p>Gestione mezzi in sviluppo...</p>';
            break;
        case 'parcheggi':
            content.innerHTML = '<p>Gestione parcheggi in sviluppo...</p>';
            break;
        case 'utenti':
            content.innerHTML = '<p>Gestione utenti in sviluppo...</p>';
            break;
    }
}

// Utility functions
function getMezzoIcon(tipo) {
    switch(tipo) {
        case 0: return '<i class="fas fa-bicycle"></i>';
        case 1: return '<i class="fas fa-bicycle"></i>';
        case 2: return '<i class="fas fa-motorcycle"></i>';
        default: return '<i class="fas fa-bicycle"></i>';
    }
}

function getTipoText(tipo) {
    switch(tipo) {
        case 0: return 'Bici Muscolare';
        case 1: return 'Bici Elettrica';
        case 2: return 'Monopattino';
        default: return 'Sconosciuto';
    }
}

function getStatusClass(stato) {
    switch(stato) {
        case 0: return 'available';
        case 1: return 'in-use';
        case 2: return 'maintenance';
        case 3: return 'low-battery';
        default: return 'unknown';
    }
}

function getStatusText(stato) {
    switch(stato) {
        case 0: return 'Disponibile';
        case 1: return 'In Uso';
        case 2: return 'Manutenzione';
        case 3: return 'Batteria Scarica';
        default: return 'Sconosciuto';
    }
}

function getCorseStatusText(stato) {
    switch(stato) {
        case 0: return 'In Corso';
        case 1: return 'Completata';
        case 2: return 'Annullata';
        default: return 'Sconosciuto';
    }
}

function formatDuration(duration) {
    if (!duration) return '00:00:00';
    
    // Duration is in TimeSpan format (hh:mm:ss)
    return duration;
}

function startRideTimer() {
    if (rideTimer) clearInterval(rideTimer);
    
    rideTimer = setInterval(updateRideTimer, 1000);
}

function updateRideTimer() {
    if (!activeRide) return;
    
    const start = new Date(activeRide.dataInizio);
    const now = new Date();
    const elapsed = Math.floor((now - start) / 1000);
    
    const hours = Math.floor(elapsed / 3600);
    const minutes = Math.floor((elapsed % 3600) / 60);
    const seconds = elapsed % 60;
    
    const duration = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    document.getElementById('rideDuration').textContent = duration;
    
    // Calculate cost (rough estimate)
    const cost = (elapsed / 60) * 0.10; // €0.10 per minute
    document.getElementById('rideCost').textContent = cost.toFixed(2);
    
    // Calculate eco points for muscle bikes
    const ecoPoints = Math.floor(elapsed / 60) * 2; // 2 points per minute
    document.getElementById('rideEcoPoints').textContent = ecoPoints;
}

function updateEcoPointsProgress() {
    if (!currentUser) return;
    
    const points = currentUser.puntiEco || 0;
    const progress = (points % 100) / 100 * 100;
    
    const progressBar = document.getElementById('ecoProgressBar');
    if (progressBar) {
        progressBar.style.width = progress + '%';
    }
    
    const currentPointsEl = document.getElementById('currentEcoPoints');
    if (currentPointsEl) {
        currentPointsEl.textContent = points;
    }
    
    const convertBtn = document.getElementById('convertEcoBtn');
    if (convertBtn) {
        convertBtn.disabled = points < 100;
    }
}

function showNotification(message, type = 'info') {
    const container = document.getElementById('notifications');
    if (!container) return;
    
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.innerHTML = `
        <span>${message}</span>
        <button onclick="this.parentElement.remove()">&times;</button>
    `;
    
    container.appendChild(notification);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 5000);
}

function showLoading(show) {
    const spinner = document.getElementById('loadingSpinner');
    if (spinner) {
        spinner.style.display = show ? 'flex' : 'none';
    }
}

// Additional utility functions for parcheggio details
async function loadParcheggioDetails(parcheggioId) {
    try {
        const response = await fetch(`${API_BASE}/parcheggi/${parcheggioId}`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });
        
        if (response.ok) {
            const parcheggio = await response.json();
            showParcheggioModal(parcheggio);
        } else {
            showNotification('Errore nel caricamento dettagli parcheggio', 'error');
        }
    } catch (error) {
        console.error('Load parcheggio details error:', error);
        showNotification('Errore di connessione', 'error');
    }
}

function showParcheggioModal(parcheggio) {
    // Simple modal implementation
    alert(`Parcheggio: ${parcheggio.nome}\nSlots disponibili: ${parcheggio.slotsDisponibili}/${parcheggio.capacita}\nMezzi presenti: ${parcheggio.mezziPresenti?.length || 0}`);
}