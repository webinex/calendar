Import-Module -Name $PSScriptRoot\Blurbs.psm1 -Force
Import-Module -Name $PSScriptRoot\Nuget.psm1 -Force
Import-Module -Name $PSScriptRoot\SolutionFiles.psm1 -Force

function Build-NugetPackage(
  [Parameter(Mandatory = $true)] $ProjectInfo,
  [Parameter(Mandatory = $true)] [bool] $NoBuild
) {
  Write-Host -ForegroundColor Cyan "[$($ProjectInfo.Name)]: Build..."

  if ($NoBuild -eq $false) {
    dotnet restore -v minimal $ProjectInfo.Path
    if ($LASTEXITCODE -ne 0) { exit 1 }
    
    dotnet build -c Release $ProjectInfo.Path --no-restore
    if ($LASTEXITCODE -ne 0) { exit 1 }
  }
  
  $noBuildArgs = $(if ($NoBuild) { " --no-build" } else { "" })
  $command = "dotnet pack $($ProjectInfo.Path) -o $($ProjectInfo.Output) -c Release" + $noBuildArgs;

  Invoke-Expression $command
  if ($LASTEXITCODE -ne 0) { exit 1 }
}


function Build-NugetPackages([Parameter(Mandatory = $true)] $ProjectInfo) {
  Build-NugetPackage -ProjectInfo $ProjectInfo -NoBuild $false
  
  foreach ($info in $ProjectInfo.ChildProjects) {
    Build-NugetPackage -ProjectInfo $info -NoBuild $true
  }
}

function Write-ProjectInfo($ProjectInfo) {
  Write-Host ""
  Write-Host -ForegroundColor Green "++++++++ [$($ProjectInfo.Name)] projects to pack: ++++++++"
  Write-Host ""

  $projects = @($ProjectInfo)
  $projects += $ProjectInfo.ChildProjects
  
  $projects | ForEach-Object {[PSCustomObject]$_} |`
    Select-Object -Property Name, Version, Path |`
    Format-Table -AutoSize | Out-String | Write-Host -ForegroundColor Green
}

function Get-NugetPackagePath([Parameter(Mandatory = $true)] $ProjectInfo) {
  return "$($projectInfo.Output)/$($projectInfo.Name).$($projectInfo.Version).nupkg"
}

function Publish-NugetPackages([Parameter(Mandatory = $true)] $ProjectInfo) {
  Publish-NugetPackage -Path "$(Get-NugetPackagePath -ProjectInfo $ProjectInfo)" -ApiKey $ApiKey

  foreach ($childProject in $ProjectInfo.ChildProjects) {
    Publish-NugetPackage -Path "$(Get-NugetPackagePath -ProjectInfo $childProject)" -ApiKey $ApiKey
  }
}

function Publish-LibraryNugetPackage(
  [Parameter(Mandatory = $true)][string] $Project,
  [Parameter(Mandatory = $true)][string] $ApiKey,
  [bool] $NotPublishedOnly,
  [string] $Name
) {
  $Name = $(if ($Name -eq "") { $Project } else { $Name })

  Write-ProjectNameBlurb -Project $Project
  $projectInfo = Get-ProjectInfo -Project $Project -Name $Name
  $skip = $(if ($NotPublishedOnly -eq $false) { $false } else { [bool] (Get-IsNugetPackagePublished -Package $projectInfo.Name -Version $projectInfo.Version) }) 
  Write-PublishDecisionBlurb -Project $Project -Skip $skip

  if ($skip -eq $true) {
    return;
  }

  Write-ProjectInfo -ProjectInfo $projectInfo
  Build-NugetPackages -ProjectInfo $projectInfo
  Publish-NugetPackages -ProjectInfo $projectInfo
}

Export-ModuleMember -Function Publish-LibraryNugetPackage