$petMutex=$null
try {
 $petMutex=[Threading.Mutex]::OpenExisting('Local.ChiikawaClaudePet.v1')
} catch [Threading.WaitHandleCannotBeOpenedException] {
 Start-Process -FilePath (Join-Path $PSScriptRoot 'ChiikawaDesktopPet.exe') -WindowStyle Hidden
} finally {
 if($null -ne $petMutex){$petMutex.Dispose()}
}
