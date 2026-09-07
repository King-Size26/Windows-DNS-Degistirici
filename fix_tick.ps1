$path = "C:\Users\alim\Desktop\DNSDegistiriciApp\MainForm.cs"
$c = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
# The file currently has "?" where tick/cross should be due to encoding loss
# Replace status texts
$c = $c.Replace('"?  Yonetici yetkisi yok', '"\u2715  Yonetici yetkisi yok')
$c = $c.Replace('"?  Yonetici yetkisi aktif', '"\u2713  Yonetici yetkisi aktif')
# For dots: there are two occurrences of lblStatusDot.Text = "?" - first is cross, second tick
# Split and rejoin to distinguish
$parts = $c.Split(@('lblStatusDot.Text = "?"'), [System.StringSplitOptions]::None)
if ($parts.Length -eq 3) {
    $c = $parts[0] + 'lblStatusDot.Text = "\u2715"' + $parts[1] + 'lblStatusDot.Text = "\u2713"' + $parts[2]
    Write-Host "Fixed 2 dot texts"
} else {
    Write-Host "Unexpected parts count $($parts.Length)"
}
[System.IO.File]::WriteAllText($path, $c, [System.Text.Encoding]::UTF8)
Write-Host "Done"
Get-Content -LiteralPath $path | Select-String -Pattern "lblStatus" | Select-Object -First 5 | ForEach-Object { Write-Host $_ }
