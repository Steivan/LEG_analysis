# PowerShell script to test GeoAdmin API URLs for roof data
param (
    [double]$buildingX = 2691962.5,  # Centroid X from your output
    [double]$buildingY = 1241951.25, # Centroid Y from your output
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
        $response = Invoke-RestMethod -Uri $url -Method Get -ErrorAction Stop
        $content = $response | ConvertTo-Json -Depth 10 -Compress
        $output = "${testName} Response: 200 OK - $content"
        Write-Host "${testName} Response: 200 OK" -ForegroundColor Green
        Add-Content -Path $LogFile -Value $output
        $jsonFile = "${testName}_full.json"
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

# Feature IDs to test
$featureIds = @("18020307", "18020302", "18020303", "18020301", "18020306")

# Test URL: Identify with featureId, geometryType, and geometry
$baseUrl = "https://api3.geo.admin.ch/rest/services/api/MapServer/identify?layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds={0}&tolerance=0&imageDisplay=1,1,96&returnGeometry=true&geometryFormat=geojson&sr=2056&geometryType=esriGeometryPoint&geometry=$buildingX,$buildingY"

# Execute tests for each featureId
foreach ($i in 0..($featureIds.Count - 1)) {
    $featureId = $featureIds[$i]
    $url = [string]::Format($baseUrl, $featureId)
    $testName = "Test Roof $featureId"
    Test-ApiUrl -url $url -testName $testName
}

# Pause to view output
Read-Host "Press Enter to exit"