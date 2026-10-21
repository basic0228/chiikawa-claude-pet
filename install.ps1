$ErrorActionPreference='Stop'
$settingsPath=Join-Path $env:USERPROFILE '.claude\settings.json'
$settings=if(Test-Path -LiteralPath $settingsPath){Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json}else{[pscustomobject]@{}}
if(!$settings.PSObject.Properties['hooks']){$settings | Add-Member hooks ([pscustomobject]@{})}
$events=@('SessionStart','UserPromptSubmit','PreToolUse','PostToolUse','PermissionRequest','Notification','Stop','StopFailure','SessionEnd')
foreach($eventName in $events){
 $command='powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "'+(Join-Path $PSScriptRoot 'event.ps1')+'" -EventName '+$eventName
 $existing=if($settings.hooks.PSObject.Properties[$eventName]){@($settings.hooks.$eventName)}else{@()}
 $kept=@($existing | Where-Object { !(@($_.hooks | Where-Object { $_.command -like ('*'+(Join-Path $PSScriptRoot 'event.ps1')+'*') }).Count) })
 $entry=[pscustomobject]@{matcher='';hooks=@([pscustomobject]@{type='command';command=$command;timeout=5})}
 $settings.hooks | Add-Member -MemberType NoteProperty -Name $eventName -Value @($kept+$entry) -Force
}
if(Test-Path -LiteralPath $settingsPath){Copy-Item -LiteralPath $settingsPath -Destination ($settingsPath+'.chiikawa-backup-'+(Get-Date -Format 'yyyyMMddHHmmss'))}
[IO.Directory]::CreateDirectory((Split-Path -Parent $settingsPath))|Out-Null
[IO.File]::WriteAllText($settingsPath,($settings|ConvertTo-Json -Depth 50),[Text.UTF8Encoding]::new($false))
Write-Output 'Installed Chiikawa hooks; restart Claude Code sessions.'
