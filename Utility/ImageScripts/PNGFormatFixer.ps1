
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$InputFolder,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputFolder
)

if (-not (Test-Path $InputFolder -PathType Container)) {
    Write-Error "Input folder does not exist: $InputFolder"
    exit 1
}

New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null

Get-ChildItem $InputFolder -Filter *.png -File | ForEach-Object {
    $input = $_.FullName
    $output = Join-Path $OutputFolder $_.Name

    magick $input -depth 8 "PNG32:$output"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Converted: $($_.Name)"
    } else {
        Write-Host "FAILED: $($_.Name)"
    }
}
