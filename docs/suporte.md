# Suporte e envio de logs

Este projeto usa o GitHub Issues como canal de suporte, comentários e análise de erros.

## Onde abrir um chamado

Acesse:

```text
https://github.com/AtsonMelo/monitor-hardware/issues/new/choose
```

Escolha o formulário mais adequado:

- `Erro no app`: quando o app trava, fecha sozinho ou se comporta de forma inesperada;
- `Enviar log para análise`: quando existe um erro registrado no arquivo de log;
- `Sugestão de melhoria`: para ideias de novas funcionalidades;
- `Pergunta ou comentário`: para dúvidas e feedback geral.

## Onde fica o log do app

O log principal fica em:

```text
%LOCALAPPDATA%\MonitorHardware\logs\app.log
```

Para abrir pelo PowerShell:

```powershell
notepad "$env:LOCALAPPDATA\MonitorHardware\logs\app.log"
```

## O que enviar

Ao abrir um chamado, informe:

- versão do app;
- versão do Windows;
- CPU, GPU, SSD/HD e se é notebook ou desktop;
- modo usado: interface gráfica, bandeja, console ou inicialização com Windows;
- trecho relevante do `app.log`;
- prints, se ajudarem a explicar o problema.

## Cuidados

Antes de colar logs publicamente, revise se existe algum dado pessoal, como nome de usuário em caminhos de pasta. Se quiser, substitua por algo como:

```text
C:\Users\SEU_USUARIO\...
```

Não envie senhas, tokens, documentos pessoais ou informações privadas.
