# PowerShell script to test GeoAdmin API URLs for roof data
param (
    [string]$featureId = "18020303", # Default to 18020303, can change via parameter
    [double]$buildingX = 2691962.5,  # Centroid X from your output
    [double]$buildingY = 1241951.25, # Centroid Y from your output
    [int]$TestNumber = 0,            # 0 for all tests, 1-6 for specific test
    [string]$LogFile = "api_test_output.txt" # Log file path
)

# Clear log file if it exists
if (Test-Path $LogFile) { Remove-Item $LogFile }

# Function to make HTTP request and return response, logging to file
function Test-ApiUrl {
    param (
        [string]$url,
        [string]$testName
    )
    try {
        # Use Invoke-RestMethod to handle large JSON responses
        $response = Invoke-RestMethod -Uri $url -Method Get -ErrorAction Stop
        $content = $response | ConvertTo-Json -Depth 10 -Compress
        $output = "${testName} Response: 200 OK - $content"
        Write-Host "${testName} Response: 200 OK" -ForegroundColor Green
        Add-Content -Path $LogFile -Value $output
        # Save full JSON to a separate file
        $jsonFile = "$testName`_full.json"
        $response | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonFile -Encoding utf8
        Write-Host "Full JSON saved to $jsonFile" -ForegroundColor Cyan
        $label = $null
        if ($response.results) { 
            if ($response.results[0].properties.label) { $label = $response.results[0].properties.label }
            elseif ($response.results[0].id) { $label = $response.results[0].id }
            elseif ($response.results[0].featureId) { $label = $response.results[0].featureId }
        } elseif ($response.features) { 
            if ($response.features[0].properties.label) { $label = $response.features[0].properties.label }
            elseif ($response.features[0].id) { $label = $response.features[0].id }
            elseif ($response.features[0].featureId) { $label = $response.features[0].featureId }
        }
        $labelOutput = "Label from ${testName}: $label"
        Write-Host $labelOutput -ForegroundColor Yellow
        Add-Content -Path $LogFile -Value $labelOutput
        return $content
    }
    catch {
        $errorMsg = "${testName} Response: $($_.Exception.Response.StatusCode) - $($_.Exception.Message)"
        Write-Host $errorMsg -ForegroundColor Red
        Add-Content -Path $LogFile -Value $errorMsg
        return $null
    }
}

# Test URLs (adapted based on document)
$urls = @(
    # Test 1: WFS without namespace prefix (data.geo.admin.ch)
    "https://data.geo.admin.ch/ch.bfe.solarenergie-eignung-daecher/wfs?service=WFS&version=1.1.0&request=GetFeature&typeName=ch.bfe.solarenergie-eignung-daecher&featureId=$featureId&outputFormat=application/json&srsname=EPSG:2056",
    # Test 2: WFS with namespace prefix (data.geo.admin.ch)
    "https://data.geo.admin.ch/ch.bfe.solarenergie-eignung-daecher/wfs?service=WFS&version=1.1.0&request=GetFeature&typeName=ch.bfe.solarenergie-eignung-daecher&featureId=ch.bfe.solarenergie-eignung-daecher.$featureId&outputFormat=application/json&srsname=EPSG:2056",
    # Test 3: Identify with featureId only (api.geo.admin.ch, with geometryType)
    "https://api3.geo.admin.ch/rest/services/api/MapServer/identify?layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds=$featureId&tolerance=0&imageDisplay=1,1,96&returnGeometry=true&geometryFormat=geojson&sr=2056&geometryType=esriGeometryPoint&geometry=$buildingX,$buildingY",
    # Test 4: Identify with featureId and minimal spatial hint (api.geo.admin.ch, tolerance=5)
    "https://api3.geo.admin.ch/rest/services/api/MapServer/identify?layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds=$featureId&tolerance=5&imageDisplay=1,1,96&returnGeometry=true&geometryFormat=geojson&sr=2056&geometryType=esriGeometryPoint&geometry=$buildingX,$buildingY",
    # Test 5: Identify with featureId without geometry (api.geo.admin.ch, no geometry)
    "https://api3.geo.admin.ch/rest/services/api/MapServer/identify?layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds=$featureId&tolerance=0&imageDisplay=1,1,96&returnGeometry=true&geometryFormat=geojson&sr=2056",
    # Test 6: WFS using api.geo.admin.ch base (alternative base URL)
    "https://api3.geo.admin.ch/rest/services/api/MapServer/wfs?service=WFS&version=1.1.0&request=GetFeature&typeName=ch.bfe.solarenergie-eignung-daecher&featureId=$featureId&outputFormat=application/json&srsname=EPSG:2056"
)

# Execute tests based on TestNumber
if ($TestNumber -ge 1 -and $TestNumber -le $urls.Count) {
    $testName = "Test $TestNumber"
    Test-ApiUrl -url $urls[$TestNumber - 1] -testName $testName
}
else {
    foreach ($url in $urls) {
        $testName = "Test " + [array]::IndexOf($urls, $url) + 1
        Test-ApiUrl -url $url -testName $testName
    }
}

# Pause to view output
Read-Host "Press Enter to exit"