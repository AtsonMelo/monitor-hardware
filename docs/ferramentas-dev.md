Claro — segue em formato **Markdown puro** para você copiar e salvar como `docs/ferramentas-dev.md`:

````markdown
# Ferramentas Dev

Este arquivo é meu manual prático de ferramentas de desenvolvimento.

A ideia é registrar ferramentas que aparecem durante o projeto, para que eu entenda:

- o que é;
- para que serve;
- quando usar;
- comando básico;
- alternativa no Windows/PowerShell.

---

## 1. Busca em código

### ripgrep / rg

**O que é:**  
Ferramenta rápida para procurar textos dentro de arquivos e projetos.

**Para que serve:**  
Encontrar métodos, classes, variáveis, propriedades, erros e trechos de código.

**Quando usar:**  
Quando eu precisar localizar rapidamente onde algo aparece no projeto.

**Exemplos:**

```powershell
rg "ApplyRootLayout" .
rg "RowStyles|ColumnStyles|Margin|Padding" .\Forms\HardwareDashboardForm.cs
rg "TODO|FIXME" .
````

**Instalação no Windows:**

```powershell
winget install BurntSushi.ripgrep.MSVC
```

**Alternativa nativa no PowerShell:**

```powershell
Select-String -Path ".\Forms\HardwareDashboardForm.cs" -Pattern "RowStyles|Margin|Padding"
```

---

## 2. Git

### git status

**Para que serve:**
Ver arquivos modificados, novos ou pendentes.

```powershell
git status --short
```

### git diff

**Para que serve:**
Ver exatamente o que mudou nos arquivos.

```powershell
git diff
git diff --stat
```

### git log

**Para que serve:**
Ver histórico de commits.

```powershell
git log --oneline -10
```

---

## 3. GitHub CLI

### gh

**O que é:**
Ferramenta de terminal para interagir com o GitHub.

**Para que serve:**
Criar releases, ver issues, abrir PRs e consultar repositório.

**Exemplos:**

```powershell
gh issue list
gh release list
gh release view v0.7.0 --web
```

**Instalação:**

```powershell
winget install GitHub.cli
```

---

## 4. .NET

### dotnet build

**Para que serve:**
Compilar o projeto e verificar erros de código.

```powershell
dotnet build /p:UseSharedCompilation=false /m:1
```

### dotnet test

**Para que serve:**
Rodar testes automatizados.

```powershell
dotnet test
```

### dotnet run

**Para que serve:**
Rodar o projeto.

```powershell
dotnet run
dotnet run -- --gui
dotnet run -- --console
```

### dotnet publish

**Para que serve:**
Gerar versão distribuível do app.

```powershell
dotnet publish .\monitor-hardware.csproj -c Release -r win-x64 --self-contained true
```

---

## 5. PowerShell

### Select-String

**Para que serve:**
Buscar texto em arquivos usando PowerShell.

```powershell
Select-String -Path ".\Forms\HardwareDashboardForm.cs" -Pattern "ApplyRootLayout"
```

### Get-ChildItem

**Para que serve:**
Listar arquivos e pastas.

```powershell
Get-ChildItem
Get-ChildItem .\Forms
```

### Get-Content

**Para que serve:**
Ler conteúdo de arquivo.

```powershell
Get-Content .\README.md
```

### Set-Content

**Para que serve:**
Criar ou substituir conteúdo de arquivo.

```powershell
"texto" | Set-Content .\arquivo.txt -Encoding UTF8
```

---

## 6. Ferramentas úteis para futuro

### jq

**Para que serve:**
Ler e filtrar JSON pelo terminal.

**Exemplo:**

```powershell
curl https://api.github.com | jq
```

### curl

**Para que serve:**
Fazer requisições HTTP pelo terminal.

```powershell
curl https://github.com
```

### Sysinternals

**Para que serve:**
Diagnóstico avançado no Windows.

Ferramentas importantes:

* Process Explorer;
* Process Monitor;
* Autoruns;
* TCPView.

---

## 7. Método de aprendizado

Sempre que aparecer uma ferramenta nova, registrar:

```text
Nome:
Para que serve:
Quando usar:
Comando básico:
Alternativa no Windows:
Observação prática:
```

---

## 8. Regra pessoal

Não preciso decorar todas as ferramentas.

O objetivo é aprender assim:

```text
problema real -> ferramenta útil -> comando mínimo -> repetição -> domínio
```

---

## Histórico

Criado durante o desenvolvimento do projeto Monitor Hardware, após uso de ferramentas como Git, GitHub CLI, PowerShell, .NET CLI e pesquisa de código com ripgrep/Select-String.

````

Comando para criar direto no projeto:

```powershell
cd "C:\Users\atson\Documents\estudo-windows-internals\monitor-hardware"

New-Item -ItemType Directory -Path .\docs -Force | Out-Null

@'
# Ferramentas Dev

Este arquivo é meu manual prático de ferramentas de desenvolvimento.

A ideia é registrar ferramentas que aparecem durante o projeto, para que eu entenda:

- o que é;
- para que serve;
- quando usar;
- comando básico;
- alternativa no Windows/PowerShell.

---

## 1. Busca em código

### ripgrep / rg

**O que é:**  
Ferramenta rápida para procurar textos dentro de arquivos e projetos.

**Para que serve:**  
Encontrar métodos, classes, variáveis, propriedades, erros e trechos de código.

**Quando usar:**  
Quando eu precisar localizar rapidamente onde algo aparece no projeto.

**Exemplos:**

```powershell
rg "ApplyRootLayout" .
rg "RowStyles|ColumnStyles|Margin|Padding" .\Forms\HardwareDashboardForm.cs
rg "TODO|FIXME" .
````

**Instalação no Windows:**

```powershell
winget install BurntSushi.ripgrep.MSVC
```

**Alternativa nativa no PowerShell:**

```powershell
Select-String -Path ".\Forms\HardwareDashboardForm.cs" -Pattern "RowStyles|Margin|Padding"
```

---

## 2. Git

### git status

**Para que serve:**
Ver arquivos modificados, novos ou pendentes.

```powershell
git status --short
```

### git diff

**Para que serve:**
Ver exatamente o que mudou nos arquivos.

```powershell
git diff
git diff --stat
```

### git log

**Para que serve:**
Ver histórico de commits.

```powershell
git log --oneline -10
```

---

## 3. GitHub CLI

### gh

**O que é:**
Ferramenta de terminal para interagir com o GitHub.

**Para que serve:**
Criar releases, ver issues, abrir PRs e consultar repositório.

**Exemplos:**

```powershell
gh issue list
gh release list
gh release view v0.7.0 --web
```

**Instalação:**

```powershell
winget install GitHub.cli
```

---

## 4. .NET

### dotnet build

**Para que serve:**
Compilar o projeto e verificar erros de código.

```powershell
dotnet build /p:UseSharedCompilation=false /m:1
```

### dotnet test

**Para que serve:**
Rodar testes automatizados.

```powershell
dotnet test
```

### dotnet run

**Para que serve:**
Rodar o projeto.

```powershell
dotnet run
dotnet run -- --gui
dotnet run -- --console
```

### dotnet publish

**Para que serve:**
Gerar versão distribuível do app.

```powershell
dotnet publish .\monitor-hardware.csproj -c Release -r win-x64 --self-contained true
```

---

## 5. PowerShell

### Select-String

**Para que serve:**
Buscar texto em arquivos usando PowerShell.

```powershell
Select-String -Path ".\Forms\HardwareDashboardForm.cs" -Pattern "ApplyRootLayout"
```

### Get-ChildItem

**Para que serve:**
Listar arquivos e pastas.

```powershell
Get-ChildItem
Get-ChildItem .\Forms
```

### Get-Content

**Para que serve:**
Ler conteúdo de arquivo.

```powershell
Get-Content .\README.md
```

### Set-Content

**Para que serve:**
Criar ou substituir conteúdo de arquivo.

```powershell
"texto" | Set-Content .\arquivo.txt -Encoding UTF8
```

---

## 6. Ferramentas úteis para futuro

### jq

**Para que serve:**
Ler e filtrar JSON pelo terminal.

**Exemplo:**

```powershell
curl https://api.github.com | jq
```

### curl

**Para que serve:**
Fazer requisições HTTP pelo terminal.

```powershell
curl https://github.com
```

### Sysinternals

**Para que serve:**
Diagnóstico avançado no Windows.

Ferramentas importantes:

* Process Explorer;
* Process Monitor;
* Autoruns;
* TCPView.

---

## 7. Método de aprendizado

Sempre que aparecer uma ferramenta nova, registrar:

```text
Nome:
Para que serve:
Quando usar:
Comando básico:
Alternativa no Windows:
Observação prática:
```

---

## 8. Regra pessoal

Não preciso decorar todas as ferramentas.

O objetivo é aprender assim:

```text
problema real -> ferramenta útil -> comando mínimo -> repetição -> domínio
```

---

## Histórico

Criado durante o desenvolvimento do projeto Monitor Hardware, após uso de ferramentas como Git, GitHub CLI, PowerShell, .NET CLI e pesquisa de código com ripgrep/Select-String.
