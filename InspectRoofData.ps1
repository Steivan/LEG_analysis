# PowerShell script to inspect roof data from SwissTopo APIs
$featureId = "18020307"
$buildingX = 2691962.5
$buildingY = 1241951.25

# Query the Identify endpoint
$identifyUrl = "https://api3.geo.admin.ch/rest/services/api/MapServer/identify?layers=all:ch.bfe.solarenergie-eignung-daecher&featureIds=$featureId&tolerance=0&imageDisplay=1,1,96&mapExtent=0,0,1,1&returnGeometry=true&geometryFormat=geojson&sr=2056&geometryType=esriGeometryPoint&geometry=$buildingX,$buildingY"
Write-Host "Querying Identify endpoint: $identifyUrl"
$identifyResponse = Invoke-RestMethod -Uri $identifyUrl -Method Get
$identifyResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath "IdentifyResponse_$featureId.json" -Encoding utf8
Write-Host "Identify response saved to IdentifyResponse_$featureId.json"
if ($identifyResponse.results -and $identifyResponse.results.Count -gt 0) {
    $properties = $identifyResponse.results[0].properties
    Write-Host "Available properties for featureId $featureId : $($properties.PSObject.Properties.Name -join ', ')"
}

# Query the WFS endpoint for MTEMP_MONAT
$wfsUrl = "https://data.geo.admin.ch/ch.bfe.solarenergie-eignung-daecher/wfs?service=WFS&version=1.1.0&request=GetFeature&typeName=ch.bfe.solarenergie-eignung-daecher&propertyName=MTEMP_MONAT,DF_UID&outputFormat=application/json&srsname=EPSG:2056&filter=<Filter><PropertyIsEqualTo><PropertyName>DF_UID</PropertyName><Literal>$featureId</Literal></PropertyIsEqualTo></Filter>"
Write-Host "Querying WFS endpoint: $wfsUrl"
$wfsResponse = Invoke-RestMethod -Uri $wfsUrl -Method Get
$wfsResponse | ConvertTo-Json -Depth 10 | Out-File -FilePath "WfsResponse_$featureId.json" -Encoding utf8
Write-Host "WFS response saved to WfsResponse_$featureId.json"
if ($wfsResponse.features -and $wfsResponse.features.Count -gt 0) {
    $wfsProperties = $wfsResponse.features[0].properties
    Write-Host "Available WFS properties for DF_UID $featureId : $($wfsProperties.PSObject.Properties.Name -join ', ')"
}