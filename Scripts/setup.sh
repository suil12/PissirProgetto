#!/bin/bash
echo "========================================"
echo "    MobiShare - Setup Progetto"
echo "========================================"

echo
echo "1. Creazione struttura progetto..."
mkdir -p MobiShare
cd MobiShare

mkdir -p MobiShare.API/wwwroot/{css,js}
mkdir -p MobiShare.Core
mkdir -p MobiShare.Infrastructure
mkdir -p MobiShare.IoT

echo
echo "2. Creazione solution e progetti..."
dotnet new sln -n MobiShare

cd MobiShare.API
dotnet new webapi --no-https
cd ..

cd MobiShare.Core
dotnet new classlib
cd ..

cd MobiShare.Infrastructure
dotnet new classlib
cd ..

cd MobiShare.IoT
dotnet new classlib
cd ..

echo
echo "3. Aggiunta progetti alla solution..."
dotnet sln add MobiShare.API/MobiShare.API.csproj
dotnet sln add MobiShare.Core/MobiShare.Core.csproj
dotnet sln add MobiShare.Infrastructure/MobiShare.Infrastructure.csproj
dotnet sln add MobiShare.IoT/MobiShare.IoT.csproj

echo
echo "4. Aggiunta riferimenti..."
cd MobiShare.API
dotnet add reference ../MobiShare.Core/MobiShare.Core.csproj
dotnet add reference ../MobiShare.Infrastructure/MobiShare.Infrastructure.csproj
dotnet add reference ../MobiShare.IoT/MobiShare.IoT.csproj
cd ..

cd MobiShare.Infrastructure
dotnet add reference ../MobiShare.Core/MobiShare.Core.csproj
cd ..

cd MobiShare.IoT
dotnet add reference ../MobiShare.Core/MobiShare.Core.csproj
cd ..

echo
echo "5. Installazione pacchetti NuGet..."
cd MobiShare.API
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package MQTTnet --version 4.3.1.873
dotnet add package MQTTnet.Extensions.ManagedClient --version 4.3.1.873
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.3
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
cd ..

cd MobiShare.Core
dotnet add package BCrypt.Net-Next --version 4.0.3
cd ..

cd MobiShare.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package BCrypt.Net-Next --version 4.0.3
cd ..

cd MobiShare.IoT
dotnet add package MQTTnet --version 4.3.1.873
dotnet add package MQTTnet.Extensions.ManagedClient --version 4.3.1.873
dotnet add package Microsoft.Extensions.Hosting --version 8.0.0
dotnet add package Microsoft.Extensions.Logging --version 8.0.0
dotnet add package Newtonsoft.Json --version 13.0.3
cd ..

echo
echo "6. Setup completato!"
echo
echo "Per avviare il progetto:"
echo "1. Installa Mosquitto MQTT Broker:"
echo "   Ubuntu: sudo apt-get install mosquitto mosquitto-clients"
echo "   macOS: brew install mosquitto"
echo "2. Avvia Mosquitto: mosquitto -v"
echo "3. cd MobiShare.API"
echo "4. dotnet run"
echo
echo "URL Applicazione: http://localhost:5000"
echo "URL Swagger: http://localhost:5000/swagger"
echo

# Scripts/run.bat
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

# Scripts/run.sh
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
