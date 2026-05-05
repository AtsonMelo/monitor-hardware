# Comandos do projeto monitor-hardware

Este arquivo reúne os comandos mais usados no projeto `monitor-hardware`.

Use como manual rápido para rodar, testar, versionar, abrir PRs e acompanhar checks pelo PowerShell.

## Acessar o projeto

```powershell
cd C:\Users\atson\Documents\estudo-windows-internals\monitor-hardware
```

Abrir o projeto inteiro no VS Code:

```powershell
code .
```

Abrir um arquivo específico:

```powershell
code .\README.md
code .\Program.cs
code .\docs\comandos.md
```

## Comandos .NET

Restaurar dependências:

```powershell
dotnet restore
```

Compilar o projeto:

```powershell
dotnet build
```

Rodar testes automatizados:

```powershell
dotnet test
```

Rodar o monitor em modo normal:

```powershell
dotnet run
```

Rodar explicitamente em modo resumo:

```powershell
dotnet run -- --mode resumo
```

Rodar em modo detalhado, exibindo todos os sensores reais em loop:

```powershell
dotnet run -- --mode detalhado
```

Rodar somente gravando CSV:

```powershell
dotnet run -- --mode somente-log
```

Parar o monitor em modo normal:

```text
Ctrl + C
```

## Modos do aplicativo

Rodar diagnóstico de sensores reais:

```powershell
dotnet run -- --diagnostico
```

Gerar relatório HTML consolidando todos os CSVs da pasta `logs/`:

```powershell
dotnet run -- --relatorio
```

Abrir o relatório HTML gerado:

```powershell
Start-Process .\reports\monitor-hardware-historico.html
```

## Arquivos gerados localmente

Logs CSV:

```text
logs/monitor-hardware-YYYYMMDD.csv
```

Relatórios HTML:

```text
reports/monitor-hardware-historico.html
```

Essas pastas são artefatos locais e não devem ser commitadas.

## Fluxo Git básico

Ver estado atual:

```powershell
git status
```

Ver branch atual:

```powershell
git branch --show-current
```

Atualizar a `main`:

```powershell
git checkout main
git pull
```

Criar nova branch:

```powershell
git checkout -b nome-da-branch
```

Exemplos:

```powershell
git checkout -b docs/comandos
git checkout -b feature/nova-funcionalidade
git checkout -b tests/novos-testes
```

## Preparar commit

Ver arquivos alterados:

```powershell
git status
```

Ver diferenças:

```powershell
git diff
```

Ver resumo das diferenças:

```powershell
git diff --stat
```

Adicionar arquivos específicos:

```powershell
git add README.md
git add Program.cs
git add Services\HtmlReportService.cs
```

Adicionar tudo que foi alterado:

```powershell
git add .
```

Criar commit:

```powershell
git commit -m "Mensagem do commit"
```

Exemplo:

```powershell
git commit -m "Adiciona documentação de comandos"
```

## Publicar branch no GitHub

Enviar branch para o GitHub:

```powershell
git push -u origin nome-da-branch
```

Exemplo:

```powershell
git push -u origin docs/comandos
```

## Pull Request com GitHub CLI

Criar PR:

```powershell
gh pr create --base main --head nome-da-branch --title "Título do PR" --body "Descrição do PR"
```

Exemplo:

```powershell
gh pr create --base main --head docs/comandos --title "Adiciona documentação de comandos" --body "Documenta comandos de uso, testes, Git, PR, checks e merge do projeto."
```

Ver PR:

```powershell
gh pr view NUMERO_DO_PR
```

Ver checks do PR:

```powershell
gh pr checks NUMERO_DO_PR
```

Acompanhar checks até terminar:

```powershell
gh pr checks NUMERO_DO_PR --watch
```

Fazer merge do PR e apagar branch:

```powershell
gh pr merge NUMERO_DO_PR --merge --delete-branch
```

## Higiene pós-merge

Depois de fazer merge:

```powershell
git checkout main
git pull
git status
dotnet test
```

Resultado esperado:

```text
Your branch is up to date with 'origin/main'.
nothing to commit, working tree clean
Aprovado!
```

## GitHub Actions

Listar execuções recentes:

```powershell
gh run list
```

Ver uma execução:

```powershell
gh run view
```

Ver logs de uma execução:

```powershell
gh run view --log
```

Listar workflows:

```powershell
gh workflow list
```

## Checklist antes de abrir PR

Antes de criar um PR, rode:

```powershell
dotnet build
dotnet test
git status
```

Confirme:

```text
build sem erros
testes passando
somente arquivos esperados alterados
```

## Checklist depois do merge

Depois do merge, rode:

```powershell
git checkout main
git pull
git status
dotnet test
```

Confirme:

```text
main atualizada
working tree clean
testes passando
```
