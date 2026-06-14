# Downloads Font Awesome 6 Free (Solid) TTF into the Unity project
$dest = "$PSScriptRoot\fa-solid-900.ttf"
$url  = "https://github.com/FortAwesome/Font-Awesome/raw/6.x/webfonts/fa-solid-900.ttf"

Write-Host "Downloading Font Awesome 6 Solid -> $dest"
Invoke-WebRequest -Uri $url -OutFile $dest
Write-Host "Done. Now import the .ttf in Unity and create a Font Asset via Window > TextMeshPro (if using TMP) OR assign it directly as a PanelSettings font in UI Toolkit."
