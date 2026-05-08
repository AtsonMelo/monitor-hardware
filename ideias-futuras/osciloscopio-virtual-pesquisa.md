# Pesquisa aprofundada: scope virtual do monitor-hardware

## 1. Visão geral

Um osciloscópio é um instrumento para visualizar sinais ao longo do tempo. Ele mostra a relação entre amplitude no eixo vertical e tempo no eixo horizontal, permitindo observar variação, estabilidade, transições e eventos de disparo.

No nosso app, o conceito deve ser tratado como **scope virtual didático baseado em sensores**, não como osciloscópio físico real. A diferença é importante:

- o osciloscópio real mede sinais elétricos contínuos ou amostrados em alta velocidade;
- o scope virtual do app mostra leituras prontas de sensores do `LibreHardwareMonitor`;
- essas leituras já chegam processadas pela pilha de hardware/software;
- portanto, não temos acesso ao sinal elétrico bruto nem a amostragem de alta frequência do mundo analógico.

Limitação principal:

- sensores como temperatura, carga, clock, fan e tensão reportada são **dados lentos**;
- muitas leituras mudam em segundos ou dezenas de segundos, não em microssegundos;
- isso exige uma interface inspirada em osciloscópio, mas adaptada para telemetria.

## 2. Sistema vertical

O sistema vertical define como o valor do sinal aparece na tela.

### Conceitos principais

| Conceito | Função | Adaptação no app |
|---|---|---|
| `V/div` | escala vertical por divisão | valor por divisão do gráfico |
| Offset vertical | desloca o traço para cima/baixo | centralizar sensor em torno de um baseline |
| Posição vertical | move o traço sem mudar a escala | ajustar visibilidade de valores altos/baixos |
| Escala automática | ajusta faixa exibida | encaixar dinamicamente o sensor na área útil |
| Saturação / clipping | topo ou fundo cortado | indicar limite visual atingido |
| AC coupling | remove componente DC | destacar variação em torno de um valor base |
| DC coupling | preserva valor absoluto | mostrar valor real do sensor |

### Diferença prática entre AC e DC

- `DC coupling`: mostra o valor real do sensor, útil para temperatura, tensão, RPM e uso.
- `AC coupling`: remove o valor médio e mostra só a oscilação, útil para ver pequenas variações em torno de uma linha base.

### Aplicação por tipo de sensor

| Sensor | Melhor modo | Observação |
|---|---|---|
| Temperatura | DC | valor absoluto importa mais |
| Tensão | DC | manter unidade real |
| Clock | DC ou normalizado | pode exigir escala própria |
| Carga | DC | varia em percentual |
| Fan | DC | RPM ou porcentagem, dependendo da origem |

### Recomendação prática

O scope virtual deve permitir:

- escala manual por unidade real;
- auto scale por janela temporal;
- offset para reposicionar o traço;
- modo normalizado opcional para comparação entre sensores diferentes.

## 3. Sistema horizontal

O sistema horizontal controla o tempo mostrado na tela.

### Conceitos principais

| Conceito | Função | Adaptação no app |
|---|---|---|
| `Timebase` | base de tempo do eixo X | largura temporal da janela |
| `ms/div` | milissegundos por divisão | resolução temporal do gráfico |
| Sample rate | quantas amostras por segundo | frequência de leitura do buffer |
| Janela de tempo | intervalo visível | últimos 30 s, 60 s, 5 min etc. |
| Buffer circular | histórico limitado | mantém dados recentes sem crescer infinito |
| Varredura esquerda-direita | atualização visual | onda entra pela direita e sai pela esquerda |

### Sinal parado x sinal em movimento

- sinal parado: a forma fica estática e pouco útil para telemetria contínua;
- sinal em movimento: o gráfico desloca continuamente, revelando tendência e variação.

### Como simular movimento temporal com leituras de 1 segundo

Como a leitura vem tipicamente a cada 1 s:

- cada amostra deve ser adicionada ao buffer com timestamp real;
- o eixo X deve refletir o tempo decorrido, não apenas a posição do item;
- a tela deve redesenhar como se o gráfico estivesse “andando”;
- a linha deve avançar da direita para a esquerda conforme chegam novas leituras;
- se não houver nova leitura, o último valor pode ser mantido visualmente, mas marcado como dado antigo ou interpolado de forma leve.

### Recomendação prática

Use uma janela temporal fixa, por exemplo:

- 30 s para visão rápida;
- 60 s para visão geral;
- 300 s para tendência.

## 4. Trigger

Trigger é o mecanismo que define **quando a aquisição ou a visualização deve se estabilizar** em torno de um evento.

### Conceitos principais

| Tipo | Função | Uso no scope virtual |
|---|---|---|
| Auto | atualiza mesmo sem evento | bom para telemetria contínua |
| Normal | atualiza só ao detectar condição | bom para eventos específicos |
| Single | captura uma vez e para | útil para congelar um evento |
| Borda de subida | dispara ao subir | detectar aumento brusco |
| Borda de descida | dispara ao cair | detectar queda rápida |
| Nível de trigger | valor de referência | temperatura, carga, RPM etc. |
| Holdoff | tempo mínimo entre disparos | evita repetição excessiva |
| Pré-trigger | mostra histórico anterior | ajuda a ver contexto do evento |

### Como adaptar trigger para sensores lentos

Como os sensores são lentos, o trigger precisa ser mais permissivo:

- usar limiares com histerese;
- evitar disparo por ruído pequeno;
- permitir trigger por ultrapassagem de limite configurável;
- fazer holdoff em segundos, não em microssegundos;
- usar pré-trigger para mostrar a tendência antes do evento.

Exemplos:

- temperatura passou de `80 °C`;
- fan caiu abaixo de `1000 RPM`;
- CPU subiu acima de `90 %`;
- clock caiu abruptamente após throttling.

## 5. Aquisição

Aquisição é o processo de coletar e organizar os dados antes de desenhar o gráfico.

### Conceitos principais

| Conceito | Função | Adaptação no app |
|---|---|---|
| Sample rate | taxa de amostragem | intervalo de leitura do sensor |
| Profundidade de memória | quantidade de histórico | tamanho do buffer circular |
| Decimação | reduzir pontos exibidos | simplificar dados antigos |
| Interpolação | preencher lacunas | suavizar traços quando faltar amostra |
| Média | reduzir ruído | estabilizar visual |
| Peak detect | preservar extremos | manter picos curtos visíveis |
| Roll mode | rolagem contínua | ideal para sinais lentos |
| Persistência | manter rastros antigos | mostrar comportamento histórico |

### Como aplicar ao buffer de sensores

- manter buffer com timestamp, valor bruto e valor normalizado;
- registrar origem da leitura;
- marcar amostras atrasadas ou ausentes;
- usar média móvel apenas quando fizer sentido;
- usar `peak detect` para não perder pico rápido em intervalos longos;
- usar `roll mode` para sensores lentos e contínuos.

## 6. Medições automáticas

As medições automáticas transformam o gráfico em informação prática.

### Medições recomendadas

| Medição | Utilidade |
|---|---|
| Valor mínimo | identificar menor leitura do intervalo |
| Valor máximo | identificar maior leitura do intervalo |
| Valor médio | tendência central |
| Pico a pico | amplitude total |
| Frequência estimada | útil só quando houver padrão periódico |
| Período estimado | complementar à frequência |
| Duty cycle | útil para sinais tipo liga/desliga |
| RMS | útil quando o sinal tiver significado energético ou oscilatório |
| Slew rate | velocidade de mudança do valor |
| Delta por amostra | variação entre amostras consecutivas |
| Alertas por limite | notificar ultrapassagem de faixa |

### Observação prática

Nem toda métrica faz sentido para todo sensor:

- frequência e duty cycle são úteis para sinais binários ou periódicos;
- RMS faz mais sentido em sinais oscilatórios;
- slew rate e delta por amostra são úteis para temperatura, carga e clock;
- alertas por limite são valiosos para temperatura, fan e tensão.

## 7. Melhorias propostas para nosso scope virtual

### Evolução do desenho

- trocar linha reta por buffer temporal real mais inteligente;
- adicionar modo `Roll`;
- adicionar modo `Persistência`;
- adicionar `Auto Scale`;
- adicionar `Normalizar sinal`;
- adicionar cursor vertical e horizontal;
- adicionar medições na lateral;
- adicionar legenda com unidade real do sensor;
- adicionar congelar tela;
- adicionar exportar captura;
- adicionar comparação entre dois sensores;
- adicionar canal `CH1/CH2` futuramente;
- adicionar `FFT` futuramente como ideia exploratória.

### Prioridade técnica

O ganho mais importante não é “parecer bonito”, e sim:

- representar o tempo corretamente;
- mostrar unidade e faixa real do sensor;
- evitar que o gráfico pareça uma animação falsa;
- permitir leitura rápida de tendências e eventos.

## 8. Plano de implementação por fases

### Fase 1

- melhorar buffer temporal;
- mostrar eixo Y com unidade real;
- mostrar janela de tempo real;
- melhorar movimento da onda.

### Fase 2

- `Auto Scale`;
- cursores;
- medições automáticas.

### Fase 3

- persistência;
- `Roll mode`;
- pré-trigger;
- holdoff.

### Fase 4

- dois canais;
- comparação entre sensores;
- exportar imagem/captura.

### Fase 5

- `FFT`;
- análise avançada.

## 9. Observações importantes

- não chamar isso de osciloscópio físico real;
- chamar de **scope virtual didático baseado em sensores**;
- o `LibreHardwareMonitor` entrega leituras prontas, não amostras elétricas de alta frequência;
- temperatura, carga, fan e clock são sinais lentos comparados a sinais elétricos reais;
- o valor do recurso está em visualização temporal, comparação e detecção de tendência.

## 10. Checklist para a próxima implementação

- [ ] Criar buffer temporal com timestamp real.
- [ ] Usar janela deslizante com histórico limitado.
- [ ] Exibir unidade real do sensor no eixo Y.
- [ ] Diferenciar valor bruto, valor normalizado e valor exibido.
- [ ] Implementar auto scale.
- [ ] Implementar modo roll.
- [ ] Implementar persistência visual.
- [ ] Adicionar cursor vertical e horizontal.
- [ ] Adicionar medições automáticas na lateral.
- [ ] Adicionar freeze da tela.
- [ ] Adicionar exportação de captura.
- [ ] Preparar arquitetura para dois canais.

## Fontes

- Tektronix, [How to Use an Oscilloscope](https://www.tek.com/en/documents/primer/how-to-use-an-oscilloscope)
- Tektronix, [Setting and Using an Oscilloscope](https://www.tek.com/en/documents/primer/setting-and-using-oscilloscope)
- Tektronix, [Oscilloscope Systems and Controls](https://www.tek.com/en/documents/primer/oscilloscope-systems-and-controls)
- Tektronix, [Oscilloscope Fundamentals Poster](https://download.tek.com/document/Oscilloscope%20Fundamentals%20Poster%203GW_60028_0_11x17.pdf)
- Tektronix, [Oscilloscope Fundamentals PDF](https://engineering.case.edu/sites/default/files/Tektronix%20Oscilloscope%20-%20Fundamentals.pdf)
