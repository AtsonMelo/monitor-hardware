# Ideias futuras - monitor-hardware

## Objetivo

Registrar ideias técnicas futuras do projeto monitor-hardware sem misturar com a implementação atual.

O projeto tem foco em Windows Internals, WinForms, LibreHardwareMonitorLib, sensores reais, drivers, firmware, diagnóstico e visualização técnica.

---

## 1. Função osciloscópio simulada

### Ideia

Criar uma tela futura que represente sinais digitais, estados de bits ou valores de sensores em gráfico temporal, com visual parecido com um osciloscópio.

### Observação técnica

Não será um osciloscópio físico real sem hardware de aquisição.

A função deve ser tratada como uma visualização temporal de dados coletados pelo software.

### Exemplos de sinais

- bit 0/1
- estado ligado/desligado
- temperatura ao longo do tempo
- clock ao longo do tempo
- carga de CPU/GPU
- rotação de fan
- tensão reportada por sensor
- uso de memória
- tráfego de rede

### Status

Não implementado.

---

## 2. Correlação de GPU integrada com CPU, placa-mãe e zonas térmicas

### Ideia

Investigar notebooks com GPU integrada e tentar correlacionar temperatura da iGPU com CPU Package, placa-mãe, ACPI Thermal Zone ou sensores próximos.

### Cuidado técnico

Correlação não significa que os sensores são iguais.

Em muitos notebooks, CPU e GPU integrada compartilham encapsulamento, dissipador, heatpipe ou zona térmica ACPI, mas isso não garante que a leitura venha do mesmo sensor.

### Possíveis fontes

- LibreHardwareMonitor
- CPU Package
- GPU integrada
- ACPI Thermal Zone
- Super I/O
- Embedded Controller
- SMBIOS

### Status

Não implementado.

---

## 3. Expandir tela Origem dos sensores

### Estado atual

A tela já mostra algumas origens principais:

- Storage
- GPU
- BIOS
- BaseBoard

### Ideia futura

Expandir para mais classes Windows/CIM/WMI.

### Possíveis classes

- Win32_Processor
- Win32_PhysicalMemory
- Win32_NetworkAdapter
- Win32_Battery
- Win32_USBController
- Win32_USBHub
- Win32_SoundDevice
- WmiMonitorID
- MSAcpi_ThermalZoneTemperature

### Status

Parcialmente implementado.

---

## 4. Correlação Sensor → Hardware → Driver → Firmware

### Ideia

Criar uma correlação entre sensores do LibreHardwareMonitor e dados Windows/CIM/WMI.

### Fluxo desejado

Sensor
→ Hardware
→ Driver Windows
→ Firmware
→ PNPDeviceID
→ Fonte provável da leitura

### Observação

Essa etapa é mais delicada porque os nomes do LibreHardwareMonitor nem sempre batem diretamente com os nomes do Windows/WMI.

### Status

Não implementado.

---

## 5. Melhorias na tela Origem dos sensores

### Pendências visuais

- corrigir cabeçalhos técnicos do DataGridView
- remover barra horizontal branca
- usar AutoGenerateColumns = false
- usar AutoSizeColumnsMode = Fill
- usar ScrollBars = Vertical
- ajustar FillWeight das colunas
- adicionar tooltip para PNPDeviceID longo
- adicionar botão para copiar PNPDeviceID
- adicionar filtro por hardware, driver ou fonte

### Status

Parcialmente implementado.

---

## 6. Ajuste responsivo do dashboard

### Problema

Após adicionar o botão Origem dos sensores, o dashboard pode cortar texto dos cards em janelas mais baixas.

### Ideias de ajuste

- ShouldStackHeader considerar altura da janela
- ConfigureActionsLayout usar 3 linhas reais no modo 2 colunas
- cards usarem modo compacto quando a altura disponível for pequena
- evitar que o bloco de botões roube altura dos cards

### Status

Em andamento.

---

## 7. Alertas de temperatura acima de 80 °C

### Ideia

Emitir alerta sonoro e janela de advertência quando qualquer sensor de temperatura ultrapassar 80 °C.

### Cuidados

- evitar repetição excessiva de alerta
- permitir configurar limite
- registrar evento em log
- permitir silenciar alerta
- diferenciar alerta crítico de aviso preventivo

### Status

Não implementado.

---

## 8. Visualização completa estilo LibreHardwareMonitor

### Ideia

Evoluir a tela Conferir todos os sensores para uma visualização mais completa e organizada.

### Possíveis melhorias

- mais detalhes por sensor
- valores mínimos e máximos
- histórico simples
- agrupamento mais claro por hardware
- filtros avançados
- exportação
- visual mais parecido com LibreHardwareMonitor oficial

### Status

Parcialmente implementado.

---

## 9. Exportação de dados

### Ideia

Permitir exportar dados técnicos do app.

### Possíveis formatos

- CSV
- JSON
- Markdown
- TXT

### Dados úteis

- sensores atuais
- origem dos sensores
- drivers
- firmware
- PNPDeviceID
- logs de erro
- snapshot completo do PC

### Status

Não implementado.

---

## 10. Logs técnicos avançados

### Ideia

Melhorar logs para estudo e diagnóstico.

### Possíveis logs

- falhas WMI
- falhas LibreHardwareMonitor
- sensores indisponíveis
- origem provável da leitura
- abertura e fechamento de telas
- eventos de alerta
- falhas de atualização
- falhas de permissão

### Status

Parcialmente implementado.
