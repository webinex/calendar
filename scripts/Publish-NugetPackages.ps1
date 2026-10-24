param (
  [Parameter(Mandatory = $true)] [string] $ApiKey
)

Import-Module -Name $PSScriptRoot\NugetPackages\Package.psm1 -Force

$ErrorActionPreference = "Stop"

function Invoke-PackAndPublishNugetPackages() {
  Publish-LibraryNugetPackage -ApiKey $ApiKey -NotPublishedOnly $true -Project "Webinex.Calendar.All" -Name "Webinex.Calendar"
}

Push-Location -LiteralPath (Join-Path $PSScriptRoot '..\src')
try {
  Write-Host "Build working directory: $(Get-Location)"
  dotnet --version
  if ($LASTEXITCODE -ne 0) { exit 1 }

  Invoke-PackAndPublishNugetPackages
}
finally {
  Pop-Location
}
exit 0
