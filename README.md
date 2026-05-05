# Monitor Hardware

Monitor de hardware em C#/.NET 8 para Windows, usando a biblioteca `LibreHardwareMonitorLib`.

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

Para rodar o monitor em modo normal:

```powershell
dotnet run
```

O monitor exibe um resumo no console, atualiza os dados em intervalo configurável e grava automaticamente um log CSV.

## Relatório HTML

O projeto pode gerar um relatório HTML usando o CSV mais recente da pasta `logs/`.

Para executar:

```powershell
dotnet run -- --relatorio
```

O relatório é salvo na pasta `reports/` com o mesmo nome-base do CSV analisado.

O HTML gerado inclui:

- cards de métricas com mini-histórico;
- gráficos de desempenho em grade;
- histórico completo das leituras do CSV.

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
  "IntervaloMs": 2000
}
```

Campos:

- `CpuTempMax`: temperatura máxima esperada para CPU, em graus Celsius;
- `GpuTempMax`: temperatura máxima esperada para GPU, em graus Celsius;
- `SsdTempMax`: temperatura máxima esperada para SSD, em graus Celsius;
- `IntervaloMs`: intervalo entre leituras, em milissegundos.

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
