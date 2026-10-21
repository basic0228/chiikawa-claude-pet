$ErrorActionPreference='Stop'
try {
 & (Join-Path $PSScriptRoot 'disable-autostart.ps1')
 $stopSignal=$null
 try{$stopSignal=[Threading.EventWaitHandle]::OpenExisting('Local.ChiikawaClaudePet.Stop.v1');[void]$stopSignal.Set()}catch [Threading.WaitHandleCannotBeOpenedException]{}finally{if($stopSignal){$stopSignal.Dispose()}}
 $settingsPath=Join-Path $env:USERPROFILE '.claude\settings.json'
 if(Test-Path -LiteralPath $settingsPath){
  $settings=Get-Content -LiteralPath $settingsPath -Raw|ConvertFrom-Json
  if($settings.PSObject.Properties['hooks']){
   $eventScript=Join-Path $PSScriptRoot 'event.ps1'
   foreach($eventProperty in @($settings.hooks.PSObject.Properties)){
    $kept=@()
    foreach($group in @($eventProperty.Value)){
     $handlers=@($group.hooks|Where-Object {$_.command -notlike ('*'+$eventScript+'*')})
     if($handlers.Count -gt 0){$group.hooks=$handlers;$kept+=,$group}
    }
    if($kept.Count -gt 0){$settings.hooks|Add-Member -MemberType NoteProperty -Name $eventProperty.Name -Value $kept -Force}else{$settings.hooks.PSObject.Properties.Remove($eventProperty.Name)}
   }
   Copy-Item -LiteralPath $settingsPath -Destination ($settingsPath+'.chiikawa-uninstall-backup-'+(Get-Date -Format 'yyyyMMddHHmmss'))
   [IO.File]::WriteAllText($settingsPath,($settings|ConvertTo-Json -Depth 50),[Text.UTF8Encoding]::new($false))
  }
 }
 Write-Host '연결과 자동 실행 설정을 제거했어요. Claude 세션을 다시 시작해 주세요.'
 Write-Host '이제 이 펫 폴더를 삭제해도 됩니다.'
} catch {Write-Host $_.Exception.Message;exit 1}
