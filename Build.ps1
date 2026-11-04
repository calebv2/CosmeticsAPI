param(
 [Parameter(Mandatory=$true)][string]$ClientGame,
 [Parameter(Mandatory=$true)][string]$ServerGame,
 [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\2020.3.23f1\Editor',
 [string]$Output=(Join-Path $PSScriptRoot 'Build')
)
$ErrorActionPreference='Stop'
$compiler=Join-Path $UnityEditor 'Data\Tools\Roslyn\csc.exe'
$games=@{Client=$ClientGame;Server=$ServerGame}
foreach($role in @('Client','Server')){
 $dest=Join-Path $Output ($role+'\Mods');New-Item -ItemType Directory -Force $dest | Out-Null
 $arguments=@('/nologo','/target:library','/langversion:8','/nostdlib+','/warnaserror+','/optimize+',('/define:'+$role.ToUpperInvariant()),('/out:"'+(Join-Path $dest 'CosmeticsAPI.dll')+'"'))
 $arguments+=Get-ChildItem -LiteralPath (Join-Path $games[$role] 'A Township Tale_Data\Managed') -Filter '*.dll' | ForEach-Object {'/reference:"'+$_.FullName+'"'}
 foreach($reference in @('MelonLoader.dll','0Harmony.dll')){$arguments+='/reference:"'+(Join-Path $games[$role] ('MelonLoader\net35\'+$reference))+'"'}
 foreach($sample in @(@('Wayfarer','caleb.wayfarer','Wayfarer'),@('NaturalSkin','caleb.skin-natural','Natural'),@('FantasySkin','caleb.skin-fantasy','Fantasy'))){
  $folder=Join-Path $PSScriptRoot ('Samples\'+$sample[0]+'\'+$role+'\UserData\CosmeticsAPI\Packs\'+$sample[1])
  $manifest=Join-Path $folder 'pack.json';if(!(Test-Path -LiteralPath $manifest)){throw "Missing bundled manifest: $manifest"}
  $arguments+='/resource:"'+$manifest+'",ATT.Cosmetics.Bundled.'+$sample[2]+'.Manifest'
  if($role -eq 'Client' -and $sample[0] -eq 'Wayfarer'){
   $bundle=Join-Path $folder 'assets.bundle';if(!(Test-Path -LiteralPath $bundle)){throw "Missing bundled model: $bundle"}
   $arguments+='/resource:"'+$bundle+'",ATT.Cosmetics.Bundled.Wayfarer.Assets'
  }
 }
 $arguments+=Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Source') -Filter '*.cs' | ForEach-Object {'"'+$_.FullName+'"'}
 $rsp=Join-Path $Output ($role+'.rsp');Set-Content -LiteralPath $rsp $arguments
 & $compiler ('@'+$rsp);if($LASTEXITCODE -ne 0){throw "$role API compilation failed"}
}
