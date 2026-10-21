param([string]$EventName)
$ErrorActionPreference='Stop'
[Console]::InputEncoding=[Text.UTF8Encoding]::new($false)
try {
 $data=[Console]::In.ReadToEnd() | ConvertFrom-Json
 if(!$data.session_id){exit 0}
 if($EventName -eq 'SessionStart'){& (Join-Path $PSScriptRoot 'start-if-needed.ps1')}
 $state=switch($EventName){
  'SessionStart' {'idle'}
  'UserPromptSubmit' {'working'}
  'PreToolUse' {'working'}
  'PostToolUse' {'working'}
  'PermissionRequest' {'waiting'}
  'Notification' {if($data.notification_type -in @('permission_prompt','idle_prompt','elicitation_dialog')){'waiting'}else{'ignore'}}
  'Stop' {'done'}
  'StopFailure' {'error'}
  'SessionEnd' {'offline'}
  default {'ignore'}
 }
 if($state -eq 'ignore'){exit 0}
 $dir=Join-Path $PSScriptRoot 'status'
 [IO.Directory]::CreateDirectory($dir) | Out-Null
 $sha=[Security.Cryptography.SHA256]::Create()
 try{$key=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes([string]$data.session_id)))).Replace('-','')}finally{$sha.Dispose()}
 function ShortText([string]$text,[int]$max=160) {
  $text=($text -replace '[\r\n\t]+',' ' -replace '\s+',' ').Trim()
  if($text.Length -gt $max){return $text.Substring(0,$max-1)+'…'}
  return $text
 }
 $inputData=$data.tool_input
 $filename=if($inputData.file_path){[IO.Path]::GetFileName([string]$inputData.file_path)}else{''}
 $detail=switch([string]$data.tool_name){
  'Read' {if($filename){$filename+' 내용을 살펴보고 있어요.'}else{'파일 내용을 살펴보고 있어요.'}}
  'Edit' {if($filename){$filename+' 수정하는 중이에요!'}else{'필요한 부분을 고치고 있어요!'}}
  'Write' {if($filename){$filename+' 작성하고 있어요.'}else{'새 파일을 작성하고 있어요.'}}
  'Bash' {if($inputData.description){[string]$inputData.description}else{'명령을 실행하고 결과를 기다려요.'}}
  'Grep' {'필요한 내용을 파일에서 찾고 있어요.'}
  'Glob' {'관련 파일이 어디 있는지 찾고 있어요.'}
  'WebSearch' {if($inputData.query){'검색 중: '+[string]$inputData.query}else{'필요한 정보를 검색하고 있어요.'}}
  'WebFetch' {'웹페이지 내용을 확인하고 있어요.'}
  'TaskCreate' {[string]$inputData.subject}
  'TaskUpdate' {if($inputData.activeForm){[string]$inputData.activeForm}else{'작업 목록을 정리하고 있어요.'}}
  'TodoWrite' {$task=@($inputData.todos | Where-Object {$_.status -eq 'in_progress'} | Select-Object -First 1);if($task.Count){if($task[0].activeForm){[string]$task[0].activeForm}else{[string]$task[0].content}}else{'작업 목록을 정리하고 있어요.'}}
  default {if($inputData.description){[string]$inputData.description}else{'다음 단계를 진행하고 있어요.'}}
 }
 if($EventName -eq 'UserPromptSubmit'){$detail='부탁한 내용을 읽었어요. 차근차근 시작할게요!'}
 if($EventName -eq 'PostToolUse'){$detail='방금 단계 확인했어요. 다음 작업을 이어갈게요.'}
 # Read only visible assistant text, never private thinking or tool output.
 if($state -eq 'working' -and $EventName -ne 'UserPromptSubmit' -and $data.transcript_path -and (Test-Path -LiteralPath $data.transcript_path)){
  $spoken=''
  foreach($record in (Get-Content -LiteralPath $data.transcript_path -Encoding UTF8 -Tail 50)){
   try{
    $entry=$record | ConvertFrom-Json
    if($entry.type -eq 'user' -and ($entry.message.content -is [string] -or @($entry.message.content | Where-Object {$_.type -eq 'text'}).Count -gt 0)){$spoken=''}
    if($entry.type -eq 'assistant'){
     foreach($block in $entry.message.content){if($block.type -eq 'text' -and $block.text){$spoken=[string]$block.text}}
    }
   }catch{}
  }
  if($spoken){$detail=(ShortText $spoken 110)+' / '+$detail}
 }
 if($state -eq 'waiting'){$detail='잠깐! 클로드 창에서 확인해 주세요.'}
 if($state -eq 'done'){$detail='응답을 마쳤어요! 결과를 보러 가볼까요?'}
 if($state -eq 'idle'){$detail='준비됐어요. 무엇부터 해볼까요?'}
 if($state -eq 'error'){$detail='진행하다 문제가 생겼어요. 클로드 창을 봐주세요.'}
 if($state -eq 'offline'){$detail='다음에 또 불러주세요!'}
 $detail=ShortText $detail 180
 $target=Join-Path $dir ($key+'.status')
 $chatTitle=''
 if(Test-Path -LiteralPath $target){$previous=[IO.File]::ReadAllText($target).Split("`t");if($previous.Count -ge 5){$chatTitle=$previous[4]}}
 if($data.transcript_path -and (Test-Path -LiteralPath $data.transcript_path)){
  foreach($record in (Get-Content -LiteralPath $data.transcript_path -Encoding UTF8 -Tail 200)){
   if($record -match '"type"\s*:\s*"(custom-title|ai-title)"'){
    try{$entry=$record|ConvertFrom-Json;if($entry.customTitle){$chatTitle=[string]$entry.customTitle}elseif($entry.aiTitle){$chatTitle=[string]$entry.aiTitle}}catch{}
   }
  }
 }
 $chatTitle=ShortText $chatTitle 70
 $line=([DateTime]::UtcNow.ToString('o'))+"`t"+$state+"`t"+$detail+"`t"+$EventName+"`t"+$chatTitle
 $temp=$target+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
 [IO.File]::WriteAllText($temp,$line,[Text.UTF8Encoding]::new($false))
 Move-Item -LiteralPath $temp -Destination $target -Force
} catch { }
exit 0
