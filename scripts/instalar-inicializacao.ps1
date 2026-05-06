param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained,
    [string]$TaskName = "Monitor Hardware"
)

function Test-IsAdministrator {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [System.Security.Principal.WindowsPrincipal]::new($identity)

    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw "Execute este script em um PowerShell aberto como administrador."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "monitor-hardware.csproj"
$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained $selfContainedValue

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou."
}

$publishDirectory = Join-Path $repoRoot "bin\$Configuration\net8.0-windows\$Runtime\publish"
$exePath = Join-Path $publishDirectory "monitor-hardware.exe"

if (-not (Test-Path $exePath)) {
    throw "Executavel nao encontrado em: $exePath"
}

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$action = New-ScheduledTaskAction -Execute $exePath -Argument "--gui" -WorkingDirectory $publishDirectory
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 0)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "Inicia o Monitor Hardware em modo grafico ao entrar no Windows." `
    -Force | Out-Null

Write-Host "Inicializacao automatica instalada."
Write-Host "Tarefa: $TaskName"
Write-Host "Usuario: $currentUser"
Write-Host "Comando: $exePath --gui"
