# Sensores e métricas

O app lê sensores reais do computador usando `LibreHardwareMonitorLib`.

## CPU

- Temperatura do pacote da CPU;
- temperatura máxima dos núcleos;
- uso total da CPU;
- potência do pacote da CPU;
- clock dos núcleos;
- tensão dos núcleos;
- fan provável da CPU.

## GPU

- Temperatura da GPU;
- uso da GPU;
- potência da GPU;
- clock do núcleo;
- clock da memória;
- fan da GPU.

O app procura sensores de GPU de forma genérica, cobrindo `GpuIntel`, `GpuAmd` e `GpuNvidia` quando a biblioteca consegue detectá-los.

## SSD/HD

- Temperatura;
- vida útil, quando disponível;
- espaço usado;
- atividade de leitura/escrita.

Para temperatura de SSD/HD, o app tenta nomes comuns como `Composite Temperature`, `Temperature`, `Temperature #1` e `Temperature #2`, ignorando limites como `Warning Temperature` e `Critical Temperature` na leitura atual.

## Memória RAM

- Uso total;
- memória usada;
- memória disponível;
- capacidade total estimada.

## Placa-mãe

- Sensores do chip Super I/O, quando disponíveis;
- temperaturas da placa;
- tensões;
- velocidade de fans.

## Rede

- Velocidade de upload;
- velocidade de download;
- utilização da rede.

## Hardware testado

O projeto foi testado principalmente em:

- Placa-mãe: Biostar H510MH 2.0;
- CPU: Intel Core i3-10100F;
- GPU: Radeon RX 470 Series;
- Armazenamento: SSD 512 GB;
- Sistema: Windows 11 Insider Preview.

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

GPU
  Temperatura : 55,0 °C
  Uso         : 0,0 %
  Potência    : 24,0 W

SSD
  Temperatura : 40,0 °C
  Vida útil   : 100,0 %

Alertas
  Nenhum alerta crítico.
```
