Start-Sleep -s 1
$AssemblyDir = (Resolve-Path .\).Path

if(-not ($AssemblyDir -eq $null)){

    Get-ChildItem -Recurse -Force $AssemblyDir -File | where {-not ($_.Extension -eq ".zip") } | Remove-Item

    Get-ChildItem -Force -Path $AssemblyDir | where {-not $_.Extension -eq ".zip"  } | Remove-Item -Recurse -Force -Confirm:$false
    
    Expand-Archive -Force "update.zip" -DestinationPath $AssemblyDir
    $RunMe = Get-ChildItem -Path $AssemblyDir -Include *.exe -Recurse
    Start-Process -FilePath $RunMe[0].FullName -Wait -NoNewWindow

}