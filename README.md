\# Monitor Hardware



Monitor de hardware em C# para Windows, usando a biblioteca `LibreHardwareMonitorLib`.



O projeto lê sensores reais do computador, como temperatura da CPU, uso da CPU, potência, clock, temperatura da GPU, temperatura do SSD, uso de memória RAM, velocidade de fan e informações de rede.



\## Objetivo



Este projeto foi criado como estudo prático de:



\- Monitoramento de hardware no Windows

\- Leitura de sensores via C#

\- Uso da biblioteca LibreHardwareMonitorLib

\- Organização de projeto .NET

\- Versionamento com Git e GitHub



\## Hardware testado



O projeto foi testado em um PC com:



\- Placa-mãe: Biostar H510MH 2.0

\- CPU: Intel Core i3-10100F

\- GPU: Radeon RX 470 Series

\- Armazenamento: SSD 512 GB

\- Sistema: Windows 11 Insider Preview



\## Sensores lidos



Atualmente o app consegue exibir:



\### CPU



\- Temperatura do pacote da CPU

\- Temperatura máxima dos núcleos

\- Uso total da CPU

\- Potência do pacote da CPU

\- Clock dos núcleos

\- Tensão dos núcleos



\### Placa-mãe



\- Sensores do chip ITE IT8613E

\- Temperaturas da placa

\- Tensões

\- Velocidade de fan



\### GPU



\- Temperatura da GPU

\- Uso da GPU

\- Potência da GPU

\- Clock do núcleo

\- Clock da memória

\- Fan da GPU



\### SSD



\- Temperatura

\- Vida útil

\- Espaço usado

\- Atividade de leitura/escrita



\### Memória RAM



\- Uso total

\- Memória usada

\- Memória disponível



\### Rede



\- Velocidade de upload

\- Velocidade de download

\- Utilização da rede



\## Exemplo de saída



```text

=== Monitor de Hardware - Resumo ===

Atualizado em: 04/05/2026 22:30:00



CPU

&#x20; Temperatura Package : 42,0 °C

&#x20; Temperatura Core Max: 42,0 °C

&#x20; Uso total           : 7,8 %

&#x20; Potência Package    : 7,0 W

&#x20; Clock Core #1       : 4100 MHz

&#x20; Fan provável CPU    : 1603 RPM



GPU - Radeon RX 470

&#x20; Temperatura : 55,0 °C

&#x20; Uso         : 0,0 %

&#x20; Potência    : 24,0 W



SSD

&#x20; Temperatura : 40,0 °C

&#x20; Vida útil   : 100,0 %



Alertas

&#x20; Nenhum alerta crítico.

