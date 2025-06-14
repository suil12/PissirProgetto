@echo off
echo ========================================
echo    MobiShare - Avvio Applicazione
echo ========================================

echo.
echo Controllo Mosquitto...
tasklist /FI "IMAGENAME eq mosquitto.exe" 2>NUL | find /I /N "mosquitto.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo ✓ Mosquitto è già in esecuzione
) else (
    echo ! Avvio Mosquitto...
    start "Mosquitto MQTT Broker" mosquitto -v
    timeout /t 3
)

echo.
echo Avvio MobiShare API...
cd MobiShare.API
dotnet run
