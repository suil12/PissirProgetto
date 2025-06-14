# README.md

# MobiShare - Sistema di Gestione Sharing Mezzi

## 🚀 Panoramica

MobiShare è un sistema completo per la gestione del sharing di mezzi di trasporto (bici muscolari, bici elettriche e monopattini) sviluppato con .NET 8, SQLite e comunicazione MQTT per l'IoT.

## 🏗️ Architettura

```
┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Mobile App    │
│  (JavaScript)   │    │   (Opzionale)   │
└─────────┬───────┘    └─────────┬───────┘
          │                      │
          └──────────┬───────────┘
                     │ API REST
          ┌──────────▼───────────┐
          │      Backend         │
          │   (Microservizi)     │
          └──────────┬───────────┘
                     │
                     │ MQTT
          ┌──────────▼───────────┐
          │  Message Broker      │
          │   (Mosquitto)        │
          └──────────┬───────────┘
                     │
          ┌──────────▼───────────┐
          │   Gateway IoT        │
          │ (Gestore Sensori)    │
          └──────────┬───────────┘
                     │
          ┌──────────▼───────────┐
          │ Sensori e Attuatori  │
          │  (reali o emulati)   │
          └──────────────────────┘
```

## 🛠️ Setup e Installazione

### Prerequisiti

- .NET 8 SDK
- Visual Studio Code
- Mosquitto MQTT Broker

### Installazione Rapida

1. **Clona e configura il progetto:**

```bash
# Esegui script di setup
./Scripts/setup.sh  # Linux/Mac
# oppure
Scripts\setup.bat   # Windows
```

2. **Installa Mosquitto:**

```bash
# Ubuntu/Debian
sudo apt-get install mosquitto mosquitto-clients

# macOS
brew install mosquitto

# Windows: scarica da mosquitto.org
```

3. **Avvia l'applicazione:**

```bash
# Avvia Mosquitto
mosquitto -v

# In un nuovo terminal
cd MobiShare.API
dotnet run
```

4. **Apri l'applicazione:**

- Frontend: http://localhost:5000
- API Swagger: http://localhost:5000/swagger

## 🎯 Funzionalità Principali

### 👥 Gestione Utenti

- **Clienti**: Registrazione, login, gestione credito e corse
- **Gestori**: Amministrazione sistema, mezzi e parcheggi
- Sistema punti eco per bici muscolari (2 punti/minuto)
- Conversione punti in buoni sconto (100 punti = €2.00)

### 🚲 Gestione Mezzi

- **Tipi supportati**: Bici muscolari, bici elettriche, monopattini
- Monitoraggio batteria per mezzi elettrici
- Stati: Disponibile, In Uso, Manutenzione, Batteria Scarica
- Controllo sblocco/blocco via MQTT

### 🅿️ Gestione Parcheggi

- Slot con sensori di luce colorati (Verde/Rosso/Giallo)
- Emulazione LED Philips Hue per demo
- Monitoraggio stato slot in tempo reale
- Capacità configurabile per parcheggio

### 🏃‍♂️ Sistema Corse

- Avvio corsa con verifica credito minimo (€2.00)
- Timer tempo reale e calcolo costo
- Accumulo punti eco per bici muscolari
- Storico corse completo

### 📡 Comunicazione MQTT

Topic Structure:

```
mobishare/parking/{id_parcheggio}/mezzi          # Stato mezzi
mobishare/parking/{id_parcheggio}/stato/{id_mezzo} # Comandi
mobishare/parking/{id_parcheggio}/slots/{id_slot}  # Sensori slot
mobishare/sistema/notifiche                        # Notifiche sistema
```

## 🔧 Configurazione VS Code

### Comandi Utili

- `Ctrl+Shift+P` → "Tasks: Run Task" → "build"
- `F5` → Avvia debug
- `Ctrl+Shift+P` → "Tasks: Run Task" → "start-mosquitto"

### Extension Raccomandate

- C# Dev Kit
- SQLite Viewer
- REST Client
- Thunder Client (per test API)

## 📊 API Endpoints

### Autenticazione

```http
POST /api/auth/login
POST /api/auth/register
```

### Mezzi

```http
GET /api/vehicles              # Lista mezzi
GET /api/vehicles/{id}         # Dettaglio mezzo
POST /api/vehicles             # Crea mezzo (gestore)
PUT /api/vehicles/{id}/status  # Aggiorna stato
```

### Corse

```http
POST /api/rides/start          # Inizia corsa
PUT /api/rides/{id}/end        # Termina corsa
GET /api/rides/active          # Corsa attiva
GET /api/rides/history         # Storico
```

### Utenti

```http
GET /api/users/profile         # Profilo utente
POST /api/users/credito        # Ricarica credito
POST /api/users/convert-points # Converti punti eco
```

## 🧪 Testing e Demo

### Utenti Pre-configurati

```
Admin:
- Username: admin
- Password: admin123
- Tipo: Gestore

Test User:
- Username: mario.rossi
- Password: password123
- Tipo: Cliente
- Credito: €25.00
- Punti Eco: 150
```

### Dati Demo Pre-caricati

- 3 Parcheggi (Centro, Università, Stazione)
- 5 Mezzi (2 bici muscolari, 2 elettriche, 1 monopattino)
- 45 Slot totali con sensori LED emulati
- Simulatori per aggiornamento batteria e sensori

### Scenario Demo

1. Login come mario.rossi
2. Visualizza mezzi disponibili vicini
3. Inizia corsa con bici muscolare
4. Monitora timer e accumulo punti eco
5. Termina corsa e converti punti in buono sconto
6. Login come admin per gestione sistema

## 🔍 Monitoraggio Sistema

### Dashboard Gestore

- Statistiche mezzi (totali, in uso, disponibili)
- Monitor LED slot in tempo reale
- Gestione mezzi e parcheggi
- Notifiche sistema e manutenzione

### Simulatori IoT

- **Batteria**: Consumo automatico durante corse
- **Sensori Slot**: Aggiornamento stato LED
- **Movimenti GPS**: Simulazione posizioni
- **Manutenzione**: Alert automatici batteria scarica

## 🚀 Sviluppo e Personalizzazione

### Struttura Progetto

```
MobiShare/
├── MobiShare.API/          # Web API e Frontend
├── MobiShare.Core/         # Entità e Interfacce
├── MobiShare.Infrastructure/ # Repository e DbContext
├── MobiShare.IoT/          # Servizi MQTT e Simulatori
└── Scripts/                # Setup e utilità
```

### Aggiungere Nuovi Mezzi

1. Estendi enum `TipoMezzo` in Core/Enums
2. Aggiorna seed data in DbContext
3. Modifica frontend per supporto nuovo tipo
4. Aggiorna logiche business se necessario

### Estendere MQTT Topics

1. Definisci nuovi MessageType in Core/Models
2. Aggiorna MqttService per gestire nuovi topic
3. Implementa handlers negli adapter
4. Testa con simulatori

## 📱 Mobile App (Opzionale)

Il sistema è predisposto per app mobile tramite API REST. Endpoints ottimizzati per:

- Geolocalizzazione mezzi
- Notifiche push
- Gestione offline credito
- QR code scanning

## 🔒 Sicurezza

- Autenticazione JWT Bearer
- Hash password con BCrypt
- Autorizzazione role-based (Cliente/Gestore)
- Validazione input API
- Rate limiting (configurabile)

## 🌱 Funzionalità Eco

- **Punti Eco**: 2 punti per minuto con bici muscolari
- **Conversione**: 100 punti = €2.00 buono sconto
- **Incentivi**: Promozione trasporto sostenibile
- **Statistiche**: Tracking CO2 risparmiata (futuro)

## 📋 TODO/Roadmap

- [ ] Integrazione pagamenti reali (Stripe/PayPal)
- [ ] App mobile React Native/Flutter
- [ ] Notifiche push real-time
- [ ] Sistema prenotazioni
- [ ] Analytics avanzate e reporting
- [ ] Integrazione mappe interattive
- [ ] API webhook per terze parti
- [ ] Supporto multi-tenant

---

**MobiShare** - Sistema completo per il futuro della mobilità urbana sostenibile! 🚴‍♀️🌱
# PissirProgetto
# PissirProgetto
