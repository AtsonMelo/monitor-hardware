# Monitor Hardware

## What It Does

Real-time hardware monitoring application for Windows, built in C#/.NET 8. Reads live sensor data (CPU, GPU, RAM, temperature, fans, voltages) and provides a professional dashboard interface, system tray monitoring, CSV logging, and HTML reporting for technical diagnostics and system analysis.

**Current Version:** 0.7.2

---

## Why It Exists

Understanding your hardware's behavior is essential for:
- Diagnosing performance issues
- Monitoring system health
- Detecting thermal problems
- Validating hardware configuration
- Learning Windows Internals and sensor APIs

This project combines practical monitoring needs with software engineering best practices: testing, CI/CD, GitHub workflows, and real-world troubleshooting.

---

## Features

- ✓ **Real-time Monitoring:** Live sensor data for CPU, GPU, RAM, SSD, fans
- ✓ **Dashboard Interface:** Professional cards and graphs showing key metrics
- ✓ **System Tray:** Real-time temperature display in Windows system tray
- ✓ **Logging:** Automatic CSV logging of sensor data
- ✓ **Reports:** HTML reports and technical diagnostics
- ✓ **Virtual Oscilloscope:** Graph visualization and analysis
- ✓ **Auto-Update:** Download and install updates from GitHub Releases
- ✓ **Configurable:** JSON configuration for temperature limits and monitoring behavior
- ✓ **Multiple Modes:** GUI, tray, diagnostics, reporting, console output

---

## Tech Stack

- **Language:** C#
- **Framework:** .NET 8
- **UI:** Windows Forms
- **Hardware Library:** LibreHardwareMonitorLib
- **Testing:** xUnit
- **CI/CD:** GitHub Actions
- **Distribution:** GitHub Releases
- **Platform:** Windows 10+

---

## Project Status

**Status:** Active Development  
**Version:** 0.7.2  
**Phase:** Feature-complete for current scope  
**Last Updated:** May 2026

This is a mature portfolio project demonstrating professional development practices.

---

## Installation & Usage

### Download

Latest release: [GitHub Releases](https://github.com/AtsonMelo/monitor-hardware/releases/latest)

### PowerShell Auto-Install

```powershell
$repo = "AtsonMelo/monitor-hardware"
$release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
$asset = $release.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1

$version = $release.tag_name.TrimStart("v")
$basePath = "$env:LOCALAPPDATA\MonitorHardware"
$installPath = "$basePath\installed\$version"
$appPath = "$installPath\app"
$zipPath = Join-Path $installPath $asset.name

New-Item -ItemType Directory -Path $installPath -Force | Out-Null
Invoke-WebRequest $asset.browser_download_url -OutFile $zipPath
Unblock-File $zipPath
Expand-Archive $zipPath -DestinationPath $appPath -Force

Start-Process (Join-Path $appPath "monitor-hardware.exe") -ArgumentList "--gui"
```

### Development

```bash
# Clone
git clone https://github.com/AtsonMelo/monitor-hardware.git
cd monitor-hardware

# Build
dotnet restore
dotnet build

# Test
dotnet test

# Run
dotnet run -- --gui                    # GUI dashboard
dotnet run -- --tray                   # System tray only
dotnet run -- --diagnostico            # List all sensors
dotnet run -- --relatorio              # Generate HTML report
dotnet run -- --relatorio-tecnico      # Generate technical report
dotnet run -- --version                # Show version
dotnet run -- --mode resumo             # Console mode: summary
dotnet run -- --mode detalhado          # Console mode: detailed
dotnet run -- --mode somente-log        # Console mode: logging only
```

---

## Configuration

Edit `config.json` to customize:

```json
{
  "CpuTempMax": 80,                    # CPU temperature limit (°C)
  "GpuTempMax": 80,                    # GPU temperature limit (°C)
  "SsdTempMax": 60,                    # SSD temperature limit (°C)
  "IntervaloMs": 2000,                 # Refresh interval (ms)
  "EnableCsv": true,                   # Enable CSV logging
  "EnableConsole": true,               # Enable console output
  "Mode": "resumo",                    # Default mode
  "CpuFanSensorName": "Fan #2",       # Primary fan sensor name
  "TemperatureUnit": "C",              # Temperature unit (C/F)
  "ShowTemperatureUnitInTrayIcon": false,
  "EnableAutoUpdateCheck": true        # Check GitHub for updates
}
```

### Paths Used

```
%LOCALAPPDATA%\MonitorHardware\
├── installed\<version>\app\          # Application files
├── updates\                          # Downloaded updates
└── logs\
    └── app.log                       # Main log file
```

View logs:
```powershell
notepad "$env:LOCALAPPDATA\MonitorHardware\logs\app.log"
```

---

## Features & Roadmap

### Current Release (v0.7.2)

- [x] Real-time hardware monitoring via LibreHardwareMonitorLib
- [x] Dashboard interface with cards for CPU, GPU, RAM, SSD, fans, voltage
- [x] System tray integration with live temperature display
- [x] CSV logging for historical analysis
- [x] HTML and technical report generation
- [x] Virtual oscilloscope for trend analysis
- [x] Automatic update checking and installation
- [x] Multiple operation modes (GUI, tray, diagnostics, console)
- [x] Configurable temperature limits
- [x] Auto-start with Windows option

### Planned Improvements

- [ ] Enhanced AI diagnostics (future roadmap)
- [ ] Network monitoring integration
- [ ] Historical data analysis dashboard
- [ ] Hardware health predictions
- [ ] Stress test utilities
- [ ] Additional sensor support
- [ ] Performance optimization

---

## Security & Privacy

⚠️ **Important:**
- This tool reads **local** sensor data only
- No data is sent externally (unless you manually share reports)
- No credentials or authentication is handled
- No personal information is collected
- Safe to use in any Windows environment
- Keep config.json secure if storing in shared environments
- Do not commit real config files with sensitive paths or custom settings

The project includes `.gitignore` rules for logs and temporary files.

---

## Project Architecture

### Learning Goals

This project is a practical study of:
- **Windows Internals:** Hardware monitoring APIs
- **Sensor Reading:** LibreHardwareMonitorLib integration
- **GUI Development:** Windows Forms and responsive design
- **Data Visualization:** Charts and real-time graphs
- **System Monitoring:** Logging and reporting
- **Software Distribution:** GitHub Releases and auto-update
- **Professional Workflows:** Git, GitHub, CI/CD, testing

### Structure

```
monitor-hardware/
├── src/                              # Main application
│   ├── Forms/                        # WinForms UI components
│   ├── Models/                       # Data models
│   ├── Services/                     # Business logic
│   ├── Program.cs                    # Entry point
│   └── app.manifest                  # Admin privileges
├── monitor-hardware.Tests/           # Unit tests (xUnit)
├── scripts/                          # Helper and deployment scripts
├── docs/                             # Technical documentation
├── config.json                       # Configuration file
├── monitor-hardware.csproj           # Project file
├── monitor-hardware.sln              # Solution file
└── README.md                         # This file
```

---

## Contributing

This is a personal portfolio project. For improvements or suggestions:

1. Open an issue for feature requests or bug reports
2. Feel free to fork and experiment
3. Submit PRs for improvements or documentation

---

## License

**License:** Pending decision  
Currently no explicit license. If you plan to use or modify this code, ask for clarification on licensing terms.

---

## Author

**Atson Melo**  
Focus: Automation, hardware diagnostics, C#/.NET, PowerShell, Python  
Learning: Windows Internals, systems programming, professional workflows  
GitHub: [@AtsonMelo](https://github.com/AtsonMelo)

---

## Related Projects

- [atson-powershell-toolkit](https://github.com/AtsonMelo/atson-powershell-toolkit) – PowerShell automation
- [monitor-com](https://github.com/AtsonMelo/monitor-com) – Serial communication monitoring
- [testador-neon-hio115](https://github.com/AtsonMelo/testador-neon-hio115) – Industrial CLP testing

Resumo:

- `--gui`: abre a interface gráfica.
- `--tray`: inicia apenas o ícone na bandeja.
- `--diagnostico`: lista todos os sensores detectados.
- `--relatorio`: gera relatório HTML.
- `--relatorio-tecnico`: gera relatório técnico com sensores, hardware detectado e mapa de fans.
- `--version`: mostra a versão atual do app.
- Sem argumentos: abre a interface gráfica por padrão.
- `resumo`: painel resumido no console.
- `detalhado`: sensores reais em loop.
- `somente-log`: grava CSV sem redesenhar o console.

---

## Caminhos usados pelo app

Instalação recomendada:

    %LOCALAPPDATA%\MonitorHardware\installed\<versao>\app

Atualizações baixadas:

    %LOCALAPPDATA%\MonitorHardware\updates

Logs:

    %LOCALAPPDATA%\MonitorHardware\logs

Log principal:

    %LOCALAPPDATA%\MonitorHardware\logs\app.log

Abrir o log pelo PowerShell:

    notepad "$env:LOCALAPPDATA\MonitorHardware\logs\app.log"

---

## Suporte

Para reportar erro, enviar log, sugerir melhoria ou deixar comentário:

https://github.com/AtsonMelo/monitor-hardware/issues/new/choose

---

## Desenvolvimento

Requisitos:

- Windows.
- .NET 8 SDK.
- PowerShell.
- Git.
- GitHub CLI.

Comandos básicos:

    dotnet restore
    dotnet build
    dotnet test
    dotnet run -- --gui
    dotnet run -- --version

Fluxo recomendado:

    git status --short
    git checkout -b minha-branch
    dotnet build
    dotnet test
    git add .
    git commit -m "Descricao da alteracao"
    git push origin minha-branch
    gh pr create --base main --head minha-branch

---

## Tecnologias

- C#.
- .NET 8.
- Windows Forms.
- LibreHardwareMonitorLib.
- GitHub Actions.
- GitHub Releases.
- xUnit.
- PowerShell.

---

## Ideia central

O objetivo do projeto é aprender Windows Internals na prática, entendendo como uma aplicação conversa com o hardware por meio de sensores, drivers, APIs do Windows, bibliotecas de leitura e interfaces de visualização.

O projeto também serve como laboratório prático para estudar:

- Monitoramento de hardware.
- Diagnóstico de sensores.
- Organização de software desktop.
- Atualização automática via GitHub Releases.
- Leitura e interpretação de dados brutos.
- Visualização gráfica de comportamento do computador.
- Boas práticas de Git, PR, merge e release.

O próximo grande caminho é uma edição técnica do app, voltada a diagnóstico, vida útil de hardware, rede, histórico, análise de comportamento do computador e evolução do Scope/Osciloscópio virtual.
