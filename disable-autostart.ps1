$ErrorActionPreference='Stop'
$task=Get-ScheduledTask -TaskName 'ChiikawaClaudePet-Autostart' -ErrorAction SilentlyContinue
if($task){
 Stop-ScheduledTask -TaskName $task.TaskName
 Unregister-ScheduledTask -TaskName $task.TaskName -Confirm:$false
}
Write-Output 'Claude app auto-start watcher disabled. Claude Code session hooks remain installed.'
