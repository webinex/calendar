$slnPath = [IO.Path]::Combine($PSScriptRoot, '..', '..', 'src') | Resolve-Path

function Get-ProjectPath([Parameter(Mandatory = $true)] [string] $Project) {
  return [IO.Path]::Combine($slnPath, $Project)
}

function Get-ProjectXmlPath([Parameter(Mandatory = $true)] [string] $Project) {
  return [IO.Path]::Combine($slnPath, $Project, "$Project.csproj")
}

function Get-ProjectExists([Parameter(Mandatory = $true)] [string] $Project) {
  $projectPath = Get-ProjectPath -Project $Project
  return [IO.Directory]::Exists($projectPath)
}

function Assert-ProjectExists([Parameter(Mandatory = $true)] [string] $Project) {
  if ((Get-ProjectExists -Project $Project) -eq $false) {
    Write-Host "$Project`: project doesn't exist";
    exit 1;
  }
}

function Get-ProjectXml([Parameter(Mandatory = $true)] [string] $Project) {  
  $csprojPath = Get-ProjectXmlPath -Project $Project;
  return [xml](Get-Content $csprojPath);
}

function Get-PackagePropsXml([Parameter(Mandatory = $true)] [string] $Project) {
  $path = [IO.Path]::Combine($slnPath, $Project, "Package.props")
  return [xml](Get-Content $path);
}

function Get-PackageVersion([Parameter(Mandatory = $true)] [string] $Project) {
  $xml = Get-PackagePropsXml -Project $Project
  $version = $xml.Project.PropertyGroup.PackageVersion;

  if ($null -eq $version) {
    Write-Error "$Project`: doesn't have PackageVersion";
    exit 1;
  }

  return $version;
}

function Get-OutputDir([Parameter(Mandatory = $true)] [string] $Project) {
  $path = Get-ProjectPath -Project $Project
  return [IO.Path]::Combine($path, ".nuget");
}

function Get-IncludeProjectName($Include, $RootPath) {
  $path = [IO.Path]::Combine($RootPath, $Include) | Resolve-Path
  $file = Get-ChildItem $path
  return $file.BaseName;
}

function Get-ChildProjects(
  [Parameter(Mandatory = $true)] [string] $Project,
  [Parameter(Mandatory = $true)] [string] $Name
) {
  $path = Get-ProjectPath -Project $Project
  $xml = Get-ProjectXml -Project $Project
  $nodes = Select-Xml "//Project/ItemGroup/ProjectReference" $xml

  $nodes | Select-Object -Property @{Name = 'Include'; Expression = { $_.Node.Include } }, `
    @{Name = 'Name'; Expression = { Get-IncludeProjectName -Include $_.Node.Include -RootPath $path } } | Out-String | Write-Host

  $names = $nodes |`
    Select-Object -Property @{Name = 'Name'; Expression = { Get-IncludeProjectName -Include $_.Node.Include -RootPath $path } } |`
    Where-Object { $_.Name -like "$Name*" } |`
    Select-Object -ExpandProperty Name

  $projects = @();
  foreach ($name in $names) {
    $prj = @{
      Name = $name;
      Path = $(Get-ProjectPath -Project $name)
    }

    $projects += $prj;
  }

  return $projects;
}


function Get-ProjectInfo([Parameter(Mandatory = $true)] [string] $Project, [Parameter(Mandatory = $true)][string] $Name) {
  Assert-ProjectExists -Project $Project

  $path = Get-ProjectPath -Project $Project
  $version = Get-PackageVersion -Project $Project
  $output = Get-OutputDir -Project $Project

  $childProjects = Get-ChildProjects -Project $Project -Name $Name
  foreach ($prj in $childProjects) {
    $prj.Version = $version;
    $prj.ChildProjects = @();
    $prj.Output = $output;
  }

  $projectInfo = @{ Path = $path; Version = $version; Name = $Project; Output = $output; ChildProjects = $childProjects; };
  return $projectInfo;
}

Export-ModuleMember -Function Get-ProjectInfo