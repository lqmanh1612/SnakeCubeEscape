Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceFile = 'D:\ResLab3D\ArrowCubeEscape\GDD\GAME DESIGN DOCUMENT.docx'
$tempFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'temp_gdd.docx')
Copy-Item -Path $sourceFile -Destination $tempFile -Force

$zip = [System.IO.Compression.ZipFile]::OpenRead($tempFile)
$entry = $zip.GetEntry('word/document.xml')
$stream = $entry.Open()
$reader = New-Object IO.StreamReader($stream)
$xmlString = $reader.ReadToEnd()
$reader.Close()
$zip.Dispose()

$xml = [xml]$xmlString
$textNodes = $xml.SelectNodes('//*[local-name()="t"]')
$result = ($textNodes | Select-Object -ExpandProperty '#text') -join ' '
Write-Host $result

Remove-Item -Path $tempFile -Force
