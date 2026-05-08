# Monitor Hardware

Monitor de hardware para Windows feito em C#/.NET 8, usando LibreHardwareMonitorLib.

**Versão atual:** `0.7.2`

O app lê sensores reais do computador e mostra informações de CPU, GPU, RAM, SSD, fans, voltagens, rede, alertas, logs, relatório técnico, interface gráfica, ícone na bandeja do Windows, dashboard visual e ferramentas auxiliares de diagnóstico.

---

## Download

Baixe a versão mais recente em:

https://github.com/AtsonMelo/monitor-hardware/releases/latest

Ou baixe pelo PowerShell:

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

Se já existir uma versão aberta, feche o app pelo menu Sair no ícone da bandeja antes de extrair uma nova versão.

Destaques da v0.7.2
Implementa suporte ao comando --version.
Melhora o fluxo de atualização automática.
Após baixar e extrair uma atualização, o app tenta abrir automaticamente o executável atualizado.
Exibe mensagem informando onde a nova versão foi baixada.
Atualiza a versão interna do projeto para 0.7.2.
Corrige o comportamento em que o botão Verificar atualizações baixava a release, mas fechava o app sem explicar claramente o próximo passo.
Destaques da v0.7.1
Refinamento visual do dashboard principal.
Cards superiores para CPU, temperatura, ventoinha, voltagem e diagnóstico.
Botão Osciloscópio destacado na tela principal.
Melhorias no Scope/Osciloscópio virtual.
Ajustes nas telas de sensores e dados brutos.
Estrutura inicial para teste de estresse.
Guia de ferramentas dev em docs/ferramentas-dev.md.
Diagnóstico por IA mantido como estrutura futura.
Funcionalidades
Interface gráfica em Windows Forms;
dashboard técnico com cards principais;
leitura de sensores reais via LibreHardwareMonitorLib;
cards de CPU, GPU, RAM, SSD, fans, temperatura, voltagem e diagnóstico;
gráfico principal com tendência do sensor selecionado;
mini gráficos nos cards;
painel de sensores principais;
tela de dados brutos do hardware;
inspetor de bits para análise didática de valores float32;
Scope/Osciloscópio virtual;
estrutura inicial para teste de estresse por hardware;
diagnóstico por IA preparado como recurso futuro;
ícone na bandeja com temperatura em tempo real;
logs CSV automáticos;
relatório HTML histórico;
relatório técnico de sensores;
verificação, download e aplicação de atualizações via GitHub Releases;
opção de iniciar com o Windows;
suporte a limites configuráveis em config.json.
Modos principais
dotnet run
dotnet run -- --gui
dotnet run -- --tray
dotnet run -- --diagnostico
dotnet run -- --relatorio
dotnet run -- --relatorio-tecnico
dotnet run -- --version
dotnet run -- --mode resumo
dotnet run -- --mode detalhado
dotnet run -- --mode somente-log

Resumo:

--gui: abre a interface gráfica;
--tray: inicia apenas o ícone na bandeja;
--diagnostico: lista todos os sensores detectados;
--relatorio: gera relatório HTML;
--relatorio-tecnico: gera relatório técnico com sensores, hardware detectado e mapa de fans;
--version: mostra a versão atual do app;
sem argumentos: abre a interface gráfica por padrão;
resumo: painel resumido no console;
detalhado: sensores reais em loop;
somente-log: grava CSV sem redesenhar o console.
Caminhos usados pelo app

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
Suporte

Para reportar erro, enviar log, sugerir melhoria ou deixar comentário:

https://github.com/AtsonMelo/monitor-hardware/issues/new/choose

Desenvolvimento

Requisitos:

Windows;
.NET 8 SDK;
PowerShell;
Git;
GitHub CLI.

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
Tecnologias
C#;
.NET 8;
Windows Forms;
LibreHardwareMonitorLib;
GitHub Actions;
GitHub Releases;
xUnit;
PowerShell.
Ideia central

O objetivo do projeto é aprender Windows Internals na prática, entendendo como uma aplicação conversa com o hardware por meio de sensores, drivers, APIs do Windows, bibliotecas de leitura e interfaces de visualização.

O projeto também serve como laboratório prático para estudar:

monitoramento de hardware;
diagnóstico de sensores;
organização de software desktop;
atualização automática via GitHub Releases;
leitura e interpretação de dados brutos;
visualização gráfica de comportamento do computador;
boas práticas de Git, PR, merge e release.

O próximo grande caminho é uma edição técnica do app, voltada a diagnóstico, vida útil de hardware, rede, histórico, análise de comportamento do computador e evolução do Scope/Osciloscópio virtual.
