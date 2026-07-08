$files = Get-ChildItem -Path "c:\Users\Tobi\StateCraft\Assets\UI\Layouts" -Filter "*.uxml"
foreach ($file in $files) {
    if ($file.Name -eq "MilitaryDashboard_Recruit.uxml") {
        continue
    }
    $path = $file.FullName
    $text = [IO.File]::ReadAllText($path)
    $newText = [regex]::Replace($text, 'font-size: (\d+)px', { 
        param($m) 
        $size = [int]$m.Groups[1].Value
        if ($size -lt 12) { 
            $size += 3 
        } elseif ($size -lt 18) { 
            $size += 2 
        } else {
            $size += 1
        }
        'font-size: ' + $size + 'px' 
    })
    [IO.File]::WriteAllText($path, $newText, [System.Text.Encoding]::UTF8)
    Write-Host "Updated $($file.Name)"
}
