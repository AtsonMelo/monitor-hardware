param(
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

$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue

if ($null -eq $task) {
    Write-Host "Nenhuma tarefa de inicializacao encontrada com o nome: $TaskName"
    return
}

Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false

Write-Host "Inicializacao automatica removida."
Write-Host "Tarefa removida: $TaskName"
