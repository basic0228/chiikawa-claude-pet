param([switch]$Once)
$ErrorActionPreference='Stop'
$created=$false
$watchMutex=[Threading.Mutex]::new($true,'Local.ChiikawaClaudePet.Watcher.v1',[ref]$created)
if(!$created){$watchMutex.Dispose();exit 0}
try {
 $wasOpen=$false
 do {
  $isOpen=@(Get-Process -Name 'Claude' -ErrorAction SilentlyContinue).Count -gt 0
  if($isOpen -and !$wasOpen){& (Join-Path $PSScriptRoot 'start-if-needed.ps1')}
  if(!$isOpen){
   $stopSignal=$null
   try {
    $stopSignal=[Threading.EventWaitHandle]::OpenExisting('Local.ChiikawaClaudePet.Stop.v1')
    [void]$stopSignal.Set()
   } catch [Threading.WaitHandleCannotBeOpenedException] {
   } finally {
    if($null -ne $stopSignal){$stopSignal.Dispose()}
   }
  }
  $wasOpen=$isOpen
  if($Once){break}
  Start-Sleep -Seconds 3
 } while($true)
} finally {
 $watchMutex.ReleaseMutex()
 $watchMutex.Dispose()
}
