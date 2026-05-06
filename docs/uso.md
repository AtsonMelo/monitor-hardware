# Uso do app

Este arquivo concentra os detalhes de execução do Monitor Hardware.

## Baixar versão pronta

Acesse:

```text
https://github.com/AtsonMelo/monitor-hardware/releases/latest
```

Baixe o arquivo `monitor-hardware-vX.Y.Z-win-x64.zip`, extraia para uma pasta e execute `monitor-hardware.exe`.

## Baixar pelo PowerShell

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

`Unblock-File` remove o bloqueio que o Windows pode aplicar em arquivos baixados da internet.

## Interface gráfica

```powershell
dotnet run
dotnet run -- --gui
```

Abre uma janela própria do Windows com cards de CPU, GPU, RAM e SSD.

Ao executar `monitor-hardware.exe` sem argumentos, o app também abre a interface gráfica por padrão. Isso melhora o uso por duplo clique.

## Ícone na bandeja

```powershell
dotnet run -- --tray
```

O ícone mostra temperatura em tempo real e menu com opções como abrir painel, verificar atualizações, abrir logs e sair.

## Console

```powershell
dotnet run -- --mode resumo
dotnet run -- --mode detalhado
dotnet run -- --mode somente-log
```

- `resumo`: mostra o painel resumido;
- `detalhado`: mostra todos os sensores reais detectados em loop;
- `somente-log`: grava CSV sem redesenhar o console.

Para encerrar no console:

```text
Ctrl + C
```

## Diagnóstico

```powershell
dotnet run -- --diagnostico
```

Lista todos os sensores físicos detectados pela `LibreHardwareMonitorLib`.

Esse modo é útil quando alguma métrica não aparece na interface principal.

## Relatório HTML

```powershell
dotnet run -- --relatorio
```

O relatório é salvo em:

```text
reports/monitor-hardware-historico.html
```

## Atalho na Área de Trabalho

```powershell
.\scripts\criar-atalho.ps1
```

O script executa `dotnet publish` e cria o atalho `Monitor Hardware.lnk`.

## Inicialização com o Windows

Para ativar:

```powershell
.\scripts\instalar-inicializacao.ps1
```

Para remover:

```powershell
.\scripts\remover-inicializacao.ps1
```

Também é possível ativar ou desativar essa opção pela interface gráfica, marcando `Iniciar com o Windows`.

## Permissão de administrador

Algumas leituras físicas, principalmente temperatura da CPU, podem exigir privilégios de administrador no Windows.

O executável usa `app.manifest` com `requireAdministrator`, então o Windows solicita elevação via UAC ao abrir o app.

UAC é o controle do Windows que pergunta se você permite que um programa rode como administrador.

## Atualizações

O app verifica se existe versão mais recente publicada nas Releases do GitHub.

Quando uma nova versão é encontrada, o app pode baixar o pacote ZIP, extrair em uma pasta temporária, fechar o processo atual, copiar os arquivos novos por cima da pasta instalada e abrir o app novamente.

Por segurança, o atualizador preserva `config.json` e a pasta `logs/`, para não apagar configurações e histórico local.
