# Ideia futura: integração com IA via API

## Objetivo

Adicionar futuramente uma integração opcional com IA ao projeto `monitor-hardware`, permitindo que o usuário envie dados técnicos do sistema para análise automática, com foco em diagnóstico, interpretação de sensores, logs e geração de relatórios.

A ideia não é substituir a análise técnica manual, mas criar uma camada auxiliar para acelerar o entendimento dos dados coletados pelo aplicativo.

---

## Possíveis funcionalidades

A integração com IA poderá incluir:

- Botão **"Analisar diagnóstico com IA"** na interface principal;
- Resumo automático dos sensores de hardware;
- Interpretação de logs gerados pelo aplicativo;
- Explicação automática de erros de build, execução ou leitura de sensores;
- Sugestão de possíveis causas para temperatura elevada, falha de leitura ou sensores ausentes;
- Sugestão automática de manutenção preventiva;
- Geração de relatório técnico em linguagem clara;
- Comparação entre leituras atuais e histórico salvo;
- Explicação didática dos dados para fins de estudo de Windows Internals, hardware e diagnóstico.

---

## Cuidados obrigatórios

Antes de implementar essa funcionalidade, o projeto deverá controlar cuidadosamente:

- Quantidade de texto enviada para a API;
- Modelo de IA utilizado;
- Custo estimado por requisição;
- Limite máximo de gasto configurável;
- Histórico enviado junto com cada análise;
- Tratamento de erro da API;
- Tempo limite de resposta;
- Privacidade dos dados coletados;
- Proteção da chave de API;
- Possibilidade de usar o aplicativo sem IA.

A chave da API nunca deverá ser salva diretamente no código-fonte, no GitHub ou em arquivos públicos.

O ideal é usar variável de ambiente, por exemplo:

    OPENAI_API_KEY

---

## Requisitos técnicos sugeridos

A integração com IA deve ser implementada como um serviço separado, por exemplo:

    Services/AiDiagnosticService.cs

Esse serviço deverá receber dados já organizados pelo sistema, como:

- MonitorSnapshot;
- SensorReading;
- Logs recentes;
- Relatórios técnicos;
- Erros capturados.

A interface gráfica não deve chamar a API diretamente.

Fluxo recomendado:

    Forms -> Service -> API -> Resultado tratado -> Interface

---

## Exemplo de fluxo desejado

1. O usuário clica em **"Analisar diagnóstico com IA"**.
2. O sistema coleta o snapshot atual dos sensores.
3. O sistema remove informações sensíveis.
4. O sistema monta um prompt técnico curto e objetivo.
5. O serviço envia os dados para a API.
6. A resposta é exibida em uma janela própria.
7. O usuário pode copiar ou salvar a análise em relatório.

---

## Exemplo de prompt interno

Você é um assistente técnico especializado em diagnóstico de hardware Windows.

Analise os dados abaixo coletados pelo aplicativo monitor-hardware.

Objetivo:

- Identificar possíveis anomalias;
- Explicar os sensores em linguagem técnica clara;
- Destacar temperaturas, tensões ou cargas fora do normal;
- Sugerir próximos testes práticos;
- Evitar conclusões absolutas quando os dados forem insuficientes.

Dados coletados:

    {{DADOS_DOS_SENSORES}}

Logs recentes:

    {{LOGS_RECENTES}}

Responda no seguinte formato:

1. Resumo geral
2. Pontos normais
3. Pontos de atenção
4. Possíveis causas
5. Próximos testes recomendados

---

## Critérios de segurança

A funcionalidade de IA deve ser opcional e desativada por padrão.

Antes do primeiro uso, o aplicativo deve informar ao usuário que alguns dados técnicos serão enviados para um serviço externo de IA.

O usuário deverá ter controle sobre:

- Ativar ou desativar IA;
- Escolher o modelo;
- Definir limite de envio de caracteres/tokens;
- Definir limite de gasto estimado;
- Limpar histórico;
- Visualizar o conteúdo antes do envio.

---

## Critérios de aceite

A implementação só será considerada pronta quando:

- A aplicação funcionar normalmente sem chave de API;
- A chave não estiver salva no código;
- Erros de API forem tratados sem travar o programa;
- O usuário conseguir revisar os dados antes do envio;
- O custo estimado for exibido ou limitado;
- A resposta da IA puder ser copiada ou salva;
- A funcionalidade estiver separada em serviço próprio;
- Existirem testes unitários para montagem do prompt e tratamento de erro.

---

## Status

Ideia registrada para implementação futura.

No momento, a recomendação é continuar usando o ChatGPT pelo navegador para análises longas e manter a API apenas como possibilidade futura para automação dentro do aplicativo.
