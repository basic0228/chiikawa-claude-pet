$ErrorActionPreference='Stop'
try {
 & (Join-Path $PSScriptRoot 'install.ps1')
 & (Join-Path $PSScriptRoot 'enable-autostart.ps1')
 Write-Host ''
 Write-Host '설치 완료! Claude 앱 또는 Claude Code를 켜 주세요.'
 Write-Host '이미 실행 중인 Claude Code 세션은 한 번 다시 시작해 주세요.'
 Write-Host '설치한 폴더는 이동하거나 삭제하지 마세요.'
} catch {
 Write-Host ('설치 중 문제가 생겼어요: '+$_.Exception.Message)
 Write-Host '이 PC의 작업 스케줄러 또는 스크립트 실행 정책을 확인해 주세요.'
 exit 1
}
