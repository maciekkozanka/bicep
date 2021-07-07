
param (
    [Parameter(Mandatory = $true)]
    [string]
    $ExamplesPath,

    [Parameter(Mandatory = $true)]
    [string]
    $Tag
)

$ErrorActionPreference = 'Stop';

$levelDirs = Get-ChildItem -Path $ExamplesPath;

foreach ($levelDir in $levelDirs) {
  $moduleDirs = Get-ChildItem -Path $levelDir.FullName;
  
  foreach ($moduleDir in $moduleDirs) {
    $bicep = Join-Path -Path $moduleDir.Fullname -ChildPath 'main.bicep';
    $json = Join-Path -Path $moduleDir.Fullname -ChildPath 'main.json';

    if((Test-Path $bicep) -and (Test-Path $json))
    {
      $artifactRef = "majastrzoci.azurecr.io/examples/$($levelDir.Name)/$($moduleDir.Name):$($Tag)"
      Write-Output $artifactRef

      cd "$($moduleDir.Fullname)"
      oras push $artifactRef --manifest-config NUL:application/vnd.unknown.config.v1 ".\main.bicep:application/vnd.ms.bicep" ".\main.json:application/vnd.ms.arm-template+json"
    }
  }
}
