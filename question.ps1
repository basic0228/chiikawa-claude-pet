$ErrorActionPreference='Stop'
[Console]::InputEncoding=[Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$OutputEncoding=[Text.UTF8Encoding]::new($false)
$requestPath=$null;$answerPath=$null
try {
 $raw=[Console]::In.ReadToEnd()
 $data=$raw|ConvertFrom-Json
 if($data.tool_name -ne 'AskUserQuestion' -or !$data.session_id -or @($data.tool_input.questions).Count -eq 0){exit 0}
 if($data.tool_input.answers -and @($data.tool_input.questions|Where-Object {[string]::IsNullOrWhiteSpace([string]$data.tool_input.answers.PSObject.Properties[[string]$_.question].Value)}).Count -eq 0){exit 0}
 & (Join-Path $PSScriptRoot 'start-if-needed.ps1')
 $raw | & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'event.ps1') -EventName PreToolUse
 $folder=Join-Path $PSScriptRoot 'questions'
 [IO.Directory]::CreateDirectory($folder)|Out-Null
 $id=[Guid]::NewGuid().ToString('N')
 $requestPath=Join-Path $folder ($id+'.request.json')
 $answerPath=Join-Path $folder ($id+'.answer.json')
 $title='클로드 질문'
 $sha=[Security.Cryptography.SHA256]::Create()
 try{$key=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes([string]$data.session_id)))).Replace('-','')}finally{$sha.Dispose()}
 $statePath=Join-Path $PSScriptRoot ('status\'+$key+'.status')
 if(Test-Path -LiteralPath $statePath){$row=[IO.File]::ReadAllText($statePath).Split("`t");if($row.Count -ge 5 -and $row[4]){$title=$row[4]}}
 $deadline=[DateTime]::UtcNow.AddMinutes(10)
 $request=@{id=$id;sessionId=[string]$data.session_id;title=$title;expiresAt=$deadline.ToString('o');questions=@($data.tool_input.questions)}
 $temp=$requestPath+'.tmp'
 [IO.File]::WriteAllText($temp,($request|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
 Move-Item -LiteralPath $temp -Destination $requestPath
 while([DateTime]::UtcNow -lt $deadline){
  if(Test-Path -LiteralPath $answerPath){
   $answer=Get-Content -LiteralPath $answerPath -Raw -Encoding UTF8|ConvertFrom-Json
   if($answer.id -ne $id -or $answer.cancelled){exit 0}
   $answers=@{}
   foreach($question in $data.tool_input.questions){
    $value=$answer.answers.PSObject.Properties[[string]$question.question]
    if(!$value -or [string]::IsNullOrWhiteSpace([string]$value.Value)){exit 0}
    $answers[[string]$question.question]=[string]$value.Value
   }
   $inputCopy=$data.tool_input
   $inputCopy|Add-Member -MemberType NoteProperty -Name answers -Value $answers -Force
   @{hookSpecificOutput=@{hookEventName='PreToolUse';permissionDecision='allow';updatedInput=$inputCopy}}|ConvertTo-Json -Depth 30 -Compress
   exit 0
  }
  Start-Sleep -Milliseconds 200
 }
} catch {
 # No decision means Claude can display its normal question UI.
} finally {
 foreach($path in @($requestPath,$answerPath)){if($path -and (Test-Path -LiteralPath $path)){Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue}}
}
exit 0
