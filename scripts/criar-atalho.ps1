param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

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

$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "Monitor Hardware.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.Arguments = "--gui"
$shortcut.WorkingDirectory = $publishDirectory
$shortcut.IconLocation = "$exePath,0"
$shortcut.Description = "Monitor Hardware"
$shortcut.Save()

Write-Host "Atalho criado em: $shortcutPath"
Write-Host "Destino: $exePath --gui"
