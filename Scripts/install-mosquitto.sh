#!/bin/bash

echo "========================================"
echo "    Installazione Mosquitto MQTT Broker"
echo "========================================"

echo
echo "Rilevamento sistema operativo..."

if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Sistema: Linux"
    echo "Installazione Mosquitto..."
    
    if command -v apt-get >/dev/null 2>&1; then
        sudo apt-get update
        sudo apt-get install -y mosquitto mosquitto-clients
    elif command -v yum >/dev/null 2>&1; then
        sudo yum install -y mosquitto mosquitto-clients
    elif command -v dnf >/dev/null 2>&1; then
        sudo dnf install -y mosquitto mosquitto-clients
    else
        echo "Package manager non supportato. Installa manualmente da mosquitto.org"
        exit 1
    fi
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Sistema: macOS"
    echo "Installazione Mosquitto con Homebrew..."
    
    if command -v brew >/dev/null 2>&1; then
        brew install mosquitto
    else
        echo "Homebrew non installato. Installa Homebrew prima di continuare:"
        echo "/bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
        exit 1
    fi
    
else
    echo "Sistema operativo non supportato"
    exit 1
fi

echo
echo "✓ Mosquitto installato con successo!"
echo
echo "Test installazione:"
mosquitto --help

echo
echo "Per avviare Mosquitto:"
echo "mosquitto -v"
# Persistence
persistence true
persistence_location /var/lib/mosquitto/

# Security (uncomment for production)
# password_file /etc/mosquitto/passwd
# acl_file /etc/mosquitto/acl

# Max connections
max_connections 1000

# Message size limit (1MB)
message_size_limit 1048576

# QoS settings
max_inflight_messages 20
max_queued_messages 100

# Topic patterns for MobiShare
# mobishare/parking/+/mezzi
# mobishare/parking/+/stato/+  
# mobishare/parking/+/slots/+
# mobishare/sistema/notifiche
