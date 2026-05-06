# Roadmap

Ideias e próximos caminhos do projeto.

## Curto prazo

- Melhorar estabilidade do ícone na bandeja;
- melhorar abertura em PCs diferentes;
- organizar Releases e instruções de instalação;
- fortalecer logs de erro;
- evoluir o painel gráfico.

## Edição técnica

Criar uma versão voltada para público técnico, mostrando como o Windows conversa com o hardware em camadas:

```text
hardware físico -> firmware/driver -> Windows/.NET -> biblioteca de leitura -> Monitor Hardware
```

Possibilidades:

- diagnóstico por componente;
- análise de temperatura, uso, potência, fans e memória;
- leitura de vida útil de SSD/HD quando disponível;
- histórico de comportamento do computador;
- alertas técnicos explicativos;
- relatório para análise de manutenção.

## Rede

Primeira etapa:

- bytes enviados e recebidos;
- pacotes enviados e recebidos;
- taxa por adaptador;
- erros RX/TX;
- histórico por intervalo.

Captura profunda no estilo Wireshark fica para uma etapa posterior, porque exige outro nível de permissão, bibliotecas e possivelmente drivers de captura.

## Interface

- Gráficos em tempo real;
- cards no estilo painel técnico;
- widget/mini painel inspirado no clima da barra de tarefas;
- modo jogo/overlay leve;
- ícones específicos para fans/coolers.

## Distribuição

- Instalador;
- publicação via `winget`;
- atualização automática ou assistida;
- opção clara para ativar/desativar inicialização com o Windows.

## Android

Possibilidade futura de versão Android, provavelmente como app complementar em .NET MAUI.

Uma opção seria o Android consumir dados do app Windows por uma API local ou por sincronização futura.
