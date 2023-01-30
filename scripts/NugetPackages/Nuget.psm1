$ErrorActionPreference = "Stop"

function Get-PublishedVersions([Parameter(Mandatory = $true)][string] $Package) {
  $name = $Package.ToLowerInvariant()
  $response = Invoke-RestMethod "https://api.nuget.org/v3/registration5-semver1/$name/index.json";
  return $response.items.GetValue(0).items`
  | Select-Object -Property @{ Name = "Version"; Expression = { $_.catalogEntry.version } };
}

function Get-IsNugetPackagePublished(
  [Parameter(Mandatory = $true)][string] $Package,
  [Parameter(Mandatory = $true)] [string] $Version
) {
    
  $version = $Version.ToLowerInvariant()
  $name = $Package.ToLowerInvariant()

  $ErrorActionPreference = "Stop"
  try {
    Invoke-RestMethod https://api.nuget.org/v3/registration5-semver1/$name/$version.json | Out-Null
    return $true
  }
  catch {
    $global:LASTEXITCODE = $null
    return $false
  }
}

function Publish-NugetPackage([Parameter(Mandatory = $true)][string] $Path, [Parameter(Mandatory = $true)][string] $ApiKey) {
  Write-Host -ForegroundColor Cyan "Publishing package: $Path"
  $command = "dotnet nuget push $Path --api-key $ApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate";
  Invoke-Expression $command
  if ($LASTEXITCODE -ne 0) { exit 1 }
}

Export-ModuleMember -Function Get-IsNugetPackagePublished
Export-ModuleMember -Function Publish-NugetPackage