# Proposta técnica para a Microsoft: sensores de hardware no Windows

## Resumo

Esta proposta sugere que o Windows ofereça uma experiência nativa mais completa para monitoramento de saúde do hardware, incluindo temperatura da CPU, temperatura da GPU, temperatura do SSD, velocidade de fan e uma API segura para leitura de sensores.

A ideia nasceu durante o desenvolvimento do projeto `monitor-hardware`, um protótipo em C#/.NET 8 que usa `LibreHardwareMonitorLib` para ler sensores reais do computador e gerar saída no console, CSV, diagnóstico e relatório HTML.

## Problema atual

O Gerenciador de Tarefas já mostra informações importantes como uso de CPU, memória, disco, rede e GPU. Porém, algumas métricas relevantes para diagnóstico térmico e manutenção ainda não aparecem de forma nativa ou consistente:

- temperatura do pacote da CPU;
- velocidade de fan em RPM;
- temperatura de SSD;
- estado térmico geral do computador;
- visão rápida de alertas térmicos;
- histórico simples de leituras.

Usuários avançados, técnicos, gamers, criadores de conteúdo e Windows Insiders geralmente precisam instalar ferramentas de terceiros para ver essas informações.

## Evidência do protótipo

O projeto `monitor-hardware` mostrou que é possível apresentar essas informações de forma útil usando sensores já expostos pelo hardware.

No hardware testado, o app conseguiu ler:

- CPU: Intel Core i3-10100F;
- GPU: Radeon RX 470 Series;
- placa-mãe: Biostar H510MH 2.0;
- chip de sensores: ITE IT8613E;
- SSD 512 GB;
- memória RAM;
- rede Ethernet;
- fan em RPM.

Exemplos de sensores identificados:

```text
CPU Package - Temperature
CPU Total - Load
CPU Package - Power
GPU Core - Temperature
GPU Package - Power
SSD Temperature
Total Memory - Load
Fan #2 - Fan
```

O protótipo também gera:

- log CSV automático;
- modo diagnóstico com todos os sensores detectados;
- relatório HTML com cards, gráficos e histórico completo.

## Sugestão 1: expandir o Gerenciador de Tarefas

Adicionar ao Gerenciador de Tarefas uma seção de saúde do hardware dentro da área de desempenho.

Métricas sugeridas:

- CPU temperature;
- CPU package power;
- CPU fan speed;
- GPU temperature;
- GPU power;
- SSD temperature;
- SSD health;
- thermal warnings.

Essa informação poderia aparecer como métricas adicionais nas páginas de CPU, GPU, disco e memória, mantendo a interface atual do Gerenciador de Tarefas.

## Sugestão 2: visão rápida na barra de tarefas

Adicionar uma visão rápida opcional de saúde do hardware na barra de tarefas, semelhante ao ponto de entrada atual do clima/widgets.

Exemplos de exibição compacta:

```text
CPU 47°C
GPU 50°C
```

Ou:

```text
CPU 47°C | Fan 1658 RPM
```

Ao clicar, o usuário poderia abrir um painel com mais detalhes, gráficos recentes e alertas.

Esse recurso deveria ser opcional e configurável, permitindo escolher quais métricas aparecem na barra de tarefas.

## Sugestão 3: API segura para sensores

Criar uma API pública e documentada para leitura segura de sensores de hardware no Windows.

A API poderia permitir:

- consultar sensores disponíveis;
- ler temperatura, fan, potência e carga;
- expor unidades padronizadas;
- respeitar permissões e privacidade;
- evitar acesso direto inseguro a baixo nível;
- permitir integração com apps confiáveis.

Isso reduziria dependência de soluções não oficiais e criaria uma base mais estável para ferramentas de diagnóstico.

## Benefícios esperados

- Diagnóstico térmico mais rápido;
- melhor experiência para usuários avançados;
- suporte mais fácil para técnicos;
- menos dependência de ferramentas externas;
- integração com Windows Insider e Feedback Hub;
- base para alertas preventivos de superaquecimento;
- experiência mais consistente entre PCs.

## Texto sugerido para o Feedback Hub

Title:

```text
Add hardware sensor temperatures and fan speed to Task Manager and the taskbar
```

Feedback:

```text
Task Manager already shows CPU, GPU, memory, disk and network usage. It would be useful to add more hardware health information, such as CPU package temperature, GPU temperature, SSD temperature, fan speed and thermal warnings.

Many advanced users, technicians, gamers, creators and Windows Insiders rely on third-party tools to see these metrics. Bringing core sensor visibility to Windows would improve diagnostics, maintenance and thermal troubleshooting.

I also suggest adding an optional hardware health glance to the Windows taskbar, similar to the current weather widget entry point. It could show compact information such as CPU temperature, GPU temperature, fan speed or SSD temperature, with a click opening a detailed performance panel.

In addition, Microsoft could consider a documented and secure hardware sensor API for trusted monitoring applications. This would allow apps to query available sensors and read standardized values without relying on unsafe or inconsistent low-level access.
```

## Materiais de apoio sugeridos

Para enviar junto com a sugestão:

- print do Gerenciador de Tarefas;
- print da barra de tarefas com widget de clima;
- print do relatório HTML do `monitor-hardware`;
- CSV gerado pelo app;
- link do repositório GitHub;
- descrição do hardware testado.

## Relação com o projeto monitor-hardware

O `monitor-hardware` funciona como um protótipo educacional e técnico dessa ideia. Ele demonstra leitura real de sensores, persistência em CSV, diagnóstico bruto dos sensores e geração de relatório visual.

O projeto não substitui uma solução nativa do Windows, mas mostra um caso de uso concreto e ajuda a formular uma proposta mais objetiva para o Feedback Hub.
