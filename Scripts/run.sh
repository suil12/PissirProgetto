#!/bin/bash
echo "========================================"
echo "    MobiShare - Avvio Applicazione"
echo "========================================"

echo
echo "Controllo Mosquitto..."
if pgrep mosquitto > /dev/null; then
    echo "✓ Mosquitto è già in esecuzione"
else
    echo "! Avvio Mosquitto..."
    mosquitto -v &
    sleep 3
fi

echo
echo "Avvio MobiShare API..."
cd MobiShare.API
dotnet run