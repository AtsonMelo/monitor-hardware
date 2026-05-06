# Monitor Hardware

Monitor de hardware em C#/.NET 8 para Windows, usando a biblioteca `LibreHardwareMonitorLib`.

Versão atual: `0.5.1`.

O projeto lê sensores reais do computador, como temperatura da CPU, uso da CPU, potência, clock, temperatura da GPU, temperatura do SSD, uso de memória RAM, velocidade de fan e informações de rede.

## Objetivo

Este projeto foi criado como estudo prático de:

- monitoramento de hardware no Windows;
- leitura de sensores via C#;
- uso da biblioteca `LibreHardwareMonitorLib`;
- organização de projeto .NET;
- versionamento com Git e GitHub;
- testes automatizados básicos.

## Como executar

Para baixar o app pronto, acesse a página de Releases do GitHub:

```text
https://github.com/AtsonMelo/monitor-hardware/releases
```

Baixe o arquivo `monitor-hardware-vX.Y.Z-win-x64.zip`, extraia para uma pasta e execute `monitor-hardware.exe`.

Para rodar o monitor em modo normal:

```powershell
dotnet run
```

O monitor exibe um resumo no console, atualiza os dados em intervalo configurável e grava automaticamente um log CSV.
Para encerrar, pressione `Ctrl + C`. O loop de monitoramento usa cancelamento controlado para finalizar sem deixar o processo preso.

Também é possível escolher o modo pela linha de comando:

```powershell
dotnet run -- --mode resumo
dotnet run -- --mode detalhado
dotnet run -- --mode somente-log
```

- `resumo`: mostra o painel resumido atual;
- `detalhado`: mostra todos os sensores reais detectados em loop;
- `somente-log`: grava CSV sem redesenhar o painel no console.

## Modo bandeja

O projeto também possui um protótipo de ícone na bandeja do Windows:

```powershell
dotnet run -- --tray
```

O ícone mostra um tooltip com CPU/GPU/RAM em tempo real e possui menu para abrir a pasta de logs ou sair do app.

## Modo gráfico

Para abrir uma interface gráfica simples, sem depender do painel do terminal para visualizar as métricas:

```powershell
dotnet run -- --gui
```

Esse modo abre uma janela própria do Windows com cards de CPU, GPU, RAM e SSD. A GPU mostra temperatura, uso, potência e fan; a RAM mostra percentual, memória usada, disponível e total. As leituras continuam usando os sensores reais detectados pela `LibreHardwareMonitorLib`.

No modo bandeja, o menu do ícone também possui a opção `Abrir painel`, que abre a mesma interface gráfica.

Ao abrir o modo gráfico, o app também inicia automaticamente o ícone da bandeja.

A interface gráfica permite verificar atualizações no GitHub e ativar/desativar a inicialização automática com o Windows.

## Execução como administrador

Algumas leituras físicas, principalmente temperatura da CPU, podem exigir privilégios de administrador no Windows. O executável usa `app.manifest` com `requireAdministrator`, então o Windows solicita elevação via UAC ao abrir o app.

Durante o desenvolvimento, se a temperatura da CPU aparecer como indisponível, abra o PowerShell ou o VS Code como administrador e execute novamente.

## Atalho na Área de Trabalho

Para publicar o app e criar um atalho na Área de Trabalho apontando para o modo gráfico:

```powershell
.\scripts\criar-atalho.ps1
```

O script executa `dotnet publish` e cria o atalho `Monitor Hardware.lnk`. Ao abrir o atalho, o app inicia em modo gráfico e também mostra o ícone na bandeja.

## Inicialização com o Windows

Para iniciar o Monitor Hardware automaticamente quando o usuário entrar no Windows, execute em um PowerShell aberto como administrador:

```powershell
.\scripts\instalar-inicializacao.ps1
```

Esse script publica o app e cria uma tarefa no Agendador de Tarefas do Windows com privilégios elevados. Isso é necessário porque algumas leituras físicas, como temperatura da CPU, podem exigir administrador.

Para remover a inicialização automática:

```powershell
.\scripts\remover-inicializacao.ps1
```

Observação: apps com interface gráfica aparecem após o login do usuário. Antes do login, ainda não existe uma área de trabalho interativa para mostrar janela ou ícone na bandeja.

Também é possível ativar ou desativar essa opção pela própria interface gráfica, marcando `Iniciar com o Windows`.

## Atualizações

O app verifica automaticamente se existe uma versão mais recente publicada nas Releases do GitHub. Também é possível verificar manualmente pela interface gráfica ou pelo menu do ícone na bandeja.

Quando uma nova versão é encontrada, o app abre a página ou o arquivo de download da Release. A substituição automática dos arquivos instalados ainda é uma evolução futura.

## Solução de problemas

Se o app baixado não abrir:

- extraia o arquivo `.zip` antes de executar;
- execute `monitor-hardware.exe` como administrador;
- verifique se o Windows SmartScreen bloqueou o arquivo baixado;
- consulte o log em `%LOCALAPPDATA%\MonitorHardware\logs\app.log`.

O app registra erros de inicialização nesse arquivo para facilitar diagnóstico em outras máquinas.

## Suporte e envio de logs

Para reportar erro, enviar log, sugerir melhoria ou deixar comentário, use a área de Issues do GitHub:

```text
https://github.com/AtsonMelo/monitor-hardware/issues/new/choose
```

O repositório possui formulários específicos para:

- erro no app;
- envio de log para análise;
- sugestão de melhoria;
- pergunta ou comentário.

Veja também:

```text
docs/suporte.md
```

## Relatório HTML

O projeto pode gerar um relatório HTML consolidando todos os CSVs encontrados na pasta `logs/`.

Para executar:

```powershell
dotnet run -- --relatorio
```

O relatório é salvo na pasta `reports/` como:

```text
reports/monitor-hardware-historico.html
```

O HTML gerado inclui:

- cards de métricas com mini-histórico;
- gráficos de desempenho em grade;
- histórico completo das leituras dos CSVs.

## Modo diagnóstico

O projeto possui um modo de diagnóstico para listar todos os sensores físicos detectados pela biblioteca `LibreHardwareMonitorLib`.

Para executar:

```powershell
dotnet run -- --diagnostico
```

Esse modo:

- lê os sensores reais da máquina uma única vez;
- mostra o nome do hardware;
- mostra o tipo do hardware;
- mostra o nome do sensor;
- mostra o tipo do sensor;
- mostra o valor atual;
- não entra no loop principal do monitor;
- não grava arquivo CSV.

Ele é útil para descobrir quais sensores estão disponíveis no computador antes de ajustar a lógica de leitura do monitor.

## Configuração

O arquivo `config.json` permite ajustar limites de alerta e intervalo de atualização:

```json
{
  "CpuTempMax": 80,
  "GpuTempMax": 80,
  "SsdTempMax": 60,
  "IntervaloMs": 2000,
  "EnableCsv": true,
  "EnableConsole": true,
  "Mode": "resumo",
  "CpuFanSensorName": "Fan #2",
  "TemperatureUnit": "C",
  "ShowTemperatureUnitInTrayIcon": false,
  "EnableAutoUpdateCheck": true
}
```

Campos:

- `CpuTempMax`: temperatura máxima esperada para CPU, em graus Celsius;
- `GpuTempMax`: temperatura máxima esperada para GPU, em graus Celsius;
- `SsdTempMax`: temperatura máxima esperada para SSD, em graus Celsius;
- `IntervaloMs`: intervalo entre leituras, em milissegundos. Deve ser maior que zero;
- `EnableCsv`: ativa ou desativa a gravação automática de CSV;
- `EnableConsole`: ativa ou desativa a exibição do resumo no console;
- `Mode`: modo de execução configurado. Modos suportados: `resumo`, `detalhado` e `somente-log`;
- `CpuFanSensorName`: nome do sensor usado como fan da CPU;
- `TemperatureUnit`: unidade usada na exibição da bandeja. Use `C` para Celsius ou `F` para Fahrenheit;
- `ShowTemperatureUnitInTrayIcon`: quando `true`, mostra a unidade no ícone da bandeja, como `40°C`. Quando `false`, mostra só `40°` para deixar a fonte maior;
- `EnableAutoUpdateCheck`: quando `true`, verifica automaticamente se existe versão nova no GitHub.

## Hardware testado

O projeto foi testado em um PC com:

- Placa-mãe: Biostar H510MH 2.0;
- CPU: Intel Core i3-10100F;
- GPU: Radeon RX 470 Series;
- Armazenamento: SSD 512 GB;
- Sistema: Windows 11 Insider Preview.

## Sensores lidos

Atualmente o app consegue exibir:

### CPU

- Temperatura do pacote da CPU;
- temperatura máxima dos núcleos;
- uso total da CPU;
- potência do pacote da CPU;
- clock dos núcleos;
- tensão dos núcleos.

### Placa-mãe

- Sensores do chip ITE IT8613E;
- temperaturas da placa;
- tensões;
- velocidade de fan.

### GPU

- Temperatura da GPU;
- uso da GPU;
- potência da GPU;
- clock do núcleo;
- clock da memória;
- fan da GPU.

### SSD

- Temperatura;
- vida útil;
- espaço usado;
- atividade de leitura/escrita.

### Memória RAM

- Uso total;
- memória usada;
- memória disponível.

### Rede

- Velocidade de upload;
- velocidade de download;
- utilização da rede.

## Exemplo de saída

```text
=== Monitor de Hardware - Resumo ===
Atualizado em: 04/05/2026 22:30:00

CPU
  Temperatura Package : 42,0 °C
  Temperatura Core Max: 42,0 °C
  Uso total           : 7,8 %
  Potência Package    : 7,0 W
  Clock Core #1       : 4100 MHz
  Fan provável CPU    : 1603 RPM

GPU - Radeon RX 470
  Temperatura : 55,0 °C
  Uso         : 0,0 %
  Potência    : 24,0 W

SSD
  Temperatura : 40,0 °C
  Vida útil   : 100,0 %

Alertas
  Nenhum alerta crítico.
```

## Log CSV automático

O programa gera automaticamente arquivos CSV na pasta `logs/`.

O arquivo segue o padrão:

```text
logs/monitor-hardware-YYYYMMDD.csv
```

## Testes

Para rodar os testes automatizados:

```powershell
dotnet test
```

## Ideias futuras

- Atualizações automáticas ou assistidas usando GitHub Releases;
- ícones específicos para fans/coolers, com estado visual por rotação/alerta;
- gráficos em tempo real na interface gráfica;
- widget/mini painel inspirado no clima da barra de tarefas;
- versão Android, provavelmente como app complementar em .NET MAUI consumindo dados do monitor Windows ou de uma API local;
- distribuição por instalador e possível publicação via `winget`;
- opção na interface para ativar/desativar inicialização automática com o Windows;
- modo jogo/overlay leve para acompanhar métricas durante jogos;
- perfis de exibição para desktop, jogos e diagnóstico técnico.
