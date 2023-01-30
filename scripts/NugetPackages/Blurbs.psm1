function Write-ProjectNameBlurb([Parameter(Mandatory = $true)][string] $Project) {
  Write-Host ""
  Write-Host -ForegroundColor "Cyan" "-------------------------------------------------"
  Write-Host -ForegroundColor "Cyan" "       $Project"
  Write-Host -ForegroundColor "Cyan" "-------------------------------------------------"
  Write-Host ""
}

function Write-PublishDecisionBlurb(
  [Parameter(Mandatory = $true)] [string] $Project,
  [Parameter(Mandatory = $true)] [bool] $skip) {
  
  if ($skip -eq $true) {
    Write-Host -ForegroundColor "Green" "$Project`: version published. Skipping..."
  }
  else {
    Write-Host -ForegroundColor "Green" "$Project`: version newer. Will be published..."
  }
}

Export-ModuleMember -Function Write-ProjectNameBlurb
Export-ModuleMember -Function Write-PublishDecisionBlurb
