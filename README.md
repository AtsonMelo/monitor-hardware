# Monitor Hardware

Monitor de hardware para Windows feito em C#/.NET 8, usando `LibreHardwareMonitorLib`.

Versão atual: `0.6.0`.

O app lê sensores reais do computador e mostra informações de CPU, GPU, RAM, SSD, fans, rede, alertas, logs CSV, relatório HTML, interface gráfica e ícone na bandeja do Windows.

## Download

Baixe a versão mais recente em:

```text
https://github.com/AtsonMelo/monitor-hardware/releases/latest
```

Ou baixe pelo PowerShell:

```powershell
$repo = "AtsonMelo/monitor-hardware"
$release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
$asset = $release.assets | Where-Object { $_.name -like "*win-x64.zip" } | Select-Object -First 1
$zipPath = Join-Path $env:USERPROFILE "Downloads\$($asset.name)"
$installPath = Join-Path $env:USERPROFILE "Documents\MonitorHardware"

Invoke-WebRequest $asset.browser_download_url -OutFile $zipPath
Unblock-File $zipPath
Expand-Archive $zipPath -DestinationPath $installPath -Force
Start-Process (Join-Path $installPath "monitor-hardware.exe") -Verb RunAs
```

Se já existir uma versão aberta, feche o app pelo menu `Sair` no ícone da bandeja antes de extrair uma nova versão.

## Funcionalidades

- Interface gráfica com cards de CPU, GPU, RAM e SSD;
- ícone na bandeja com temperatura em tempo real;
- leitura de sensores reais via `LibreHardwareMonitorLib`;
- logs CSV automáticos em `logs/`;
- relatório HTML histórico em `reports/`;
- modo diagnóstico para listar sensores detectados;
- verificação de atualizações via GitHub Releases;
- opção de iniciar com o Windows;
- suporte a limites configuráveis em `config.json`.

## Modos principais

```powershell
dotnet run
dotnet run -- --gui
dotnet run -- --tray
dotnet run -- --diagnostico
dotnet run -- --relatorio
dotnet run -- --mode resumo
dotnet run -- --mode detalhado
dotnet run -- --mode somente-log
```

Resumo:

- `--gui`: abre a interface gráfica;
- `--tray`: inicia apenas o ícone na bandeja;
- `--diagnostico`: lista todos os sensores detectados;
- `--relatorio`: gera relatório HTML;
- sem argumentos: abre a interface gráfica por padrão;
- `resumo`: painel resumido no console;
- `detalhado`: sensores reais em loop;
- `somente-log`: grava CSV sem redesenhar o console.

## Documentação

- [Uso do app](docs/uso.md)
- [Configuração](docs/configuracao.md)
- [Sensores e métricas](docs/sensores.md)
- [Comandos do projeto](docs/comandos.md)
- [Suporte e envio de logs](docs/suporte.md)
- [Roadmap](docs/roadmap.md)
- [Proposta técnica para Microsoft](docs/proposta-microsoft.md)

## Suporte

Para reportar erro, enviar log, sugerir melhoria ou deixar comentário:

```text
https://github.com/AtsonMelo/monitor-hardware/issues/new/choose
```

Log principal do app:

```text
%LOCALAPPDATA%\MonitorHardware\logs\app.log
```

Abrir o log pelo PowerShell:

```powershell
notepad "$env:LOCALAPPDATA\MonitorHardware\logs\app.log"
```

## Desenvolvimento

Requisitos:

- Windows;
- .NET 8 SDK;
- PowerShell;
- Git;
- GitHub CLI, para fluxo de PR via terminal.

Comandos básicos:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run -- --gui
```

## Tecnologias

- C#;
- .NET 8;
- Windows Forms;
- LibreHardwareMonitorLib;
- GitHub Actions;
- GitHub Releases;
- xUnit.

## Ideia central

O objetivo do projeto é aprender Windows Internals na prática, entendendo como uma aplicação conversa com o hardware por meio de sensores, drivers, APIs do Windows, bibliotecas de leitura e interfaces de visualização.

O próximo grande caminho é uma edição técnica do app, voltada a diagnóstico, vida útil de hardware, rede, histórico e análise de comportamento do computador.
