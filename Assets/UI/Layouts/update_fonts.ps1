$path = "c:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard_Recruit.uxml"
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
