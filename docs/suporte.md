# Suporte e envio de logs

Este projeto usa o GitHub Issues como canal de suporte, comentarios e analise de erros.

## Onde abrir um chamado

Acesse:

```text
https://github.com/AtsonMelo/monitor-hardware/issues/new/choose
```

Escolha o formulario mais adequado:

- `Erro no app`: quando o app trava, fecha sozinho ou se comporta de forma inesperada;
- `Enviar log para analise`: quando existe um erro registrado no arquivo de log;
- `Sugestao de melhoria`: para ideias de novas funcionalidades;
- `Pergunta ou comentario`: para duvidas e feedback geral.

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

- versao do app;
- versao do Windows;
- CPU, GPU, SSD/HD e se e notebook ou desktop;
- modo usado: interface grafica, bandeja, console ou inicializacao com Windows;
- trecho relevante do `app.log`;
- prints, se ajudarem a explicar o problema.

## Cuidados

Antes de colar logs publicamente, revise se existe algum dado pessoal, como nome de usuario em caminhos de pasta. Se quiser, substitua por algo como:

```text
C:\Users\SEU_USUARIO\...
```

Nao envie senhas, tokens, documentos pessoais ou informacoes privadas.
