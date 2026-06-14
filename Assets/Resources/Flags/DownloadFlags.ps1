# ────────────────────────────────────────────────────────────────────────────
# StateCraft — Bulk flag downloader from flagcdn.com
# Downloads 160x120 PNG flags for all 55 African nations in the game.
# Output folder: Assets/Resources/Flags/   (Unity loads via Resources.Load)
# ────────────────────────────────────────────────────────────────────────────

$outputDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Write-Host "Saving flags to: $outputDir`n"

# Map: Unity asset name -> ISO 3166-1 alpha-2 code used by flagcdn.com
$flags = [ordered]@{
    "Algeria"                    = "dz"
    "Angola"                     = "ao"
    "Benin"                      = "bj"
    "Botswana"                   = "bw"
    "Burkina_Faso"               = "bf"
    "Burundi"                    = "bi"
    "Cabo_Verde"                 = "cv"
    "Cameroon"                   = "cm"
    "Central_African_Republic"   = "cf"
    "Chad"                       = "td"
    "Comoros"                    = "km"
    "Cote_dIvoire"               = "ci"
    "Djibouti"                   = "dj"
    "DR_Congo"                   = "cd"
    "Egypt"                      = "eg"
    "Equatorial_Guinea"          = "gq"
    "Eritrea"                    = "er"
    "Eswatini"                   = "sz"
    "Ethiopia"                   = "et"
    "Gabon"                      = "ga"
    "Gambia"                     = "gm"
    "Ghana"                      = "gh"
    "Guinea"                     = "gn"
    "Guinea-Bissau"              = "gw"
    "Kenya"                      = "ke"
    "Lesotho"                    = "ls"
    "Liberia"                    = "lr"
    "Libya"                      = "ly"
    "Madagascar"                 = "mg"
    "Malawi"                     = "mw"
    "Mali"                       = "ml"
    "Mauritania"                 = "mr"
    "Mauritius"                  = "mu"
    "Morocco"                    = "ma"
    "Mozambique"                 = "mz"
    "Namibia"                    = "na"
    "Niger"                      = "ne"
    "Nigeria"                    = "ng"
    "Republic_of_the_Congo"      = "cg"
    "Rwanda"                     = "rw"
    "Sao_Tome_and_Principe"      = "st"
    "Senegal"                    = "sn"
    "Seychelles"                 = "sc"
    "Sierra_Leone"               = "sl"
    "Somalia"                    = "so"
    "South_Africa"               = "za"
    "South_Sudan"                = "ss"
    "Sudan"                      = "sd"
    "Tanzania"                   = "tz"
    "Togo"                       = "tg"
    "Tunisia"                    = "tn"
    "Uganda"                     = "ug"
    "Western_Sahara"             = "eh"
    "Zambia"                     = "zm"
    "Zimbabwe"                   = "zw"
}

$ok = 0; $fail = 0
foreach ($entry in $flags.GetEnumerator()) {
    $destFile = Join-Path $outputDir "$($entry.Key).png"
    # flagcdn.com — 160w PNG (crisp enough for UI, small file size)
    $url = "https://flagcdn.com/w160/$($entry.Value).png"

    try {
        Invoke-WebRequest -Uri $url -OutFile $destFile -UseBasicParsing -ErrorAction Stop
        Write-Host "  OK  $($entry.Key) ($($entry.Value))"
        $ok++
    } catch {
        Write-Host "  FAIL $($entry.Key): $_" -ForegroundColor Red
        $fail++
    }
}

Write-Host "`n Done: $ok downloaded, $fail failed."
Write-Host " Import into Unity via: Assets > Resources > Flags"
Write-Host " Load in C#: Resources.Load<Sprite>(`"Flags/Nigeria`")"
