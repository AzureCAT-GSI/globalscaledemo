#Requires -Version 3.0
#Requires -Module AzureRM.Resources
#Requires -Module Azure.Storage

Param(
    [string] [Parameter(Mandatory=$true)] $ResourceGroupLocation,
    [string] $ResourceGroupName = 'AzureResourceGroup2',
    [switch] $UploadArtifacts,
    [string] $StorageAccountName,
    [string] $StorageAccountResourceGroupName, 
    [string] $StorageContainerName = $ResourceGroupName.ToLowerInvariant() + '-stageartifacts',
    [string] $TemplateFile = '..\Templates\WebSiteSQLDatabase.json',
    [string] $TemplateParametersFile = '..\Templates\WebSiteSQLDatabase.parameters.json',
    [string] $ArtifactStagingDirectory = '..\bin\Debug\staging',
    [string] $AzCopyPath = '..\Tools\AzCopy.exe',
    [string] $DSCSourceFolder = '..\DSC'
)

Import-Module Azure -ErrorAction SilentlyContinue

try {
    [Microsoft.Azure.Common.Authentication.AzureSession]::ClientFactory.AddUserAgent("VSAzureTools-$UI$($host.name)".replace(" ","_"), "2.8")
} catch { }

Set-StrictMode -Version 3

$OptionalParameters = New-Object -TypeName Hashtable
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)

if ($UploadArtifacts) {
    # Convert relative paths to absolute paths if needed
    $AzCopyPath = [System.IO.Path]::Combine($PSScriptRoot, $AzCopyPath)
    $ArtifactStagingDirectory = [System.IO.Path]::Combine($PSScriptRoot, $ArtifactStagingDirectory)
    $DSCSourceFolder = [System.IO.Path]::Combine($PSScriptRoot, $DSCSourceFolder)

    Set-Variable ArtifactsLocationName '_artifactsLocation' -Option ReadOnly -Force
    Set-Variable ArtifactsLocationSasTokenName '_artifactsLocationSasToken' -Option ReadOnly -Force

    $OptionalParameters.Add($ArtifactsLocationName, $null)
    $OptionalParameters.Add($ArtifactsLocationSasTokenName, $null)

    # Parse the parameter file and update the values of artifacts location and artifacts location SAS token if they are present
    $JsonContent = Get-Content $TemplateParametersFile -Raw | ConvertFrom-Json
    $JsonParameters = $JsonContent | Get-Member -Type NoteProperty | Where-Object {$_.Name -eq "parameters"}

    if ($JsonParameters -eq $null) {
        $JsonParameters = $JsonContent
    }
    else {
        $JsonParameters = $JsonContent.parameters
    }

    $JsonParameters | Get-Member -Type NoteProperty | ForEach-Object {
        $ParameterValue = $JsonParameters | Select-Object -ExpandProperty $_.Name

        if ($_.Name -eq $ArtifactsLocationName -or $_.Name -eq $ArtifactsLocationSasTokenName) {
            $OptionalParameters[$_.Name] = $ParameterValue.value
        }
    }

    $StorageAccountKey = (Get-AzureRmStorageAccountKey -ResourceGroupName $StorageAccountResourceGroupName -Name $StorageAccountName).Key1

    $StorageAccountContext = (Get-AzureRmStorageAccount -ResourceGroupName $StorageAccountResourceGroupName -Name $StorageAccountName).Context

    # Create DSC configuration archive
    if (Test-Path $DSCSourceFolder) {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $ArchiveFile = Join-Path $ArtifactStagingDirectory "dsc.zip"
        Remove-Item -Path $ArchiveFile -ErrorAction SilentlyContinue
        [System.IO.Compression.ZipFile]::CreateFromDirectory($DSCSourceFolder, $ArchiveFile)
    }

    # Generate the value for artifacts location if it is not provided in the parameter file
    $ArtifactsLocation = $OptionalParameters[$ArtifactsLocationName]
    if ($ArtifactsLocation -eq $null) {
        $ArtifactsLocation = $StorageAccountContext.BlobEndPoint + $StorageContainerName
        $OptionalParameters[$ArtifactsLocationName] = $ArtifactsLocation
    }

    # Use AzCopy to copy files from the local storage drop path to the storage account container
    & $AzCopyPath """$ArtifactStagingDirectory""", $ArtifactsLocation, "/DestKey:$StorageAccountKey", "/S", "/Y", "/Z:$env:LocalAppData\Microsoft\Azure\AzCopy\$ResourceGroupName"
    if ($LASTEXITCODE -ne 0) { return }

    # Generate the value for artifacts location SAS token if it is not provided in the parameter file
    $ArtifactsLocationSasToken = $OptionalParameters[$ArtifactsLocationSasTokenName]
    if ($ArtifactsLocationSasToken -eq $null) {
        # Create a SAS token for the storage container - this gives temporary read-only access to the container
        $ArtifactsLocationSasToken = New-AzureStorageContainerSASToken -Container $StorageContainerName -Context $StorageAccountContext -Permission r -ExpiryTime (Get-Date).AddHours(4)
        $ArtifactsLocationSasToken = ConvertTo-SecureString $ArtifactsLocationSasToken -AsPlainText -Force
        $OptionalParameters[$ArtifactsLocationSasTokenName] = $ArtifactsLocationSasToken
    }
}

# Create or update the resource group using the specified template file and template parameters file
New-AzureRmResourceGroup -Name $ResourceGroupName -Location $ResourceGroupLocation -Verbose -Force -ErrorAction Stop 

New-AzureRmResourceGroupDeployment -Name ((Get-ChildItem $TemplateFile).BaseName + '-' + ((Get-Date).ToUniversalTime()).ToString('MMdd-HHmm')) `
                                   -ResourceGroupName $ResourceGroupName `
                                   -TemplateFile $TemplateFile `
                                   -TemplateParameterFile $TemplateParametersFile `
                                   @OptionalParameters `
                                   -Force -Verbose


#sleep for 30 seconds to allow the GitHub publishing to work
#Write-Output "Sleeping for 3 minutes to allow GitHub publishing to work"
#[System.Threading.Thread]::Sleep(180000)
#Write-Output "Waking back up... nice nap."

$params = (Get-Content $TemplateParametersFile) -join "`n" | ConvertFrom-Json

$webSites = Get-AzureRmWebApp -ResourceGroupName $ResourceGroupName 
$storageAccounts = Get-AzureRmStorageAccount -ResourceGroupName $ResourceGroupName 


#Add a traffic manager profile
$params = (Get-Content $TemplateParametersFile) -join "`n" | ConvertFrom-Json
$tmProfileName = $params.parameters.uniqueDnsName.value

$tmProfile = Get-AzureRmTrafficManagerProfile -ResourceGroupName $ResourceGroupName -Name $tmProfileName -ErrorAction Ignore
if($tmProfile -eq $null)
{
    $tmProfile = New-AzureRmTrafficManagerProfile -ResourceGroupName $ResourceGroupName -Name $tmProfileName -ProfileStatus Enabled -TrafficRoutingMethod Performance -RelativeDnsName $tmProfileName -Ttl 30 -MonitorProtocol HTTPS -MonitorPort 443 -MonitorPath "/" 
}


$added = $false

foreach($site in $webSites)
{
    $siteName = $site.Name   
    Write-Output $siteName 
    #Add each storage account to the existing appSettings    
		
    $props = (Invoke-AzureRmResourceAction -ResourceGroupName $ResourceGroupName `
     -ResourceType Microsoft.Web/Sites/config -Name $siteName/appsettings `
     -Action list -ApiVersion 2015-08-01 -Force).Properties

	Write-Output "All appSettings" 
	Write-Output $props

    $hash = @{}
    $props | Get-Member -MemberType NoteProperty | % { $hash[$_.Name] = $props.($_.Name) }

    foreach($sa in $storageAccounts)
    {
        $hash[$sa.StorageAccountName] = ("DefaultEndpointsProtocol=https;AccountName=" + $sa.StorageAccountName.ToLower() + ";AccountKey=" + (Get-AzureRmStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $sa.StorageAccountName.ToLower()).Key1)        
    }
    
	Set-AzureRMWebApp -ResourceGroupName $ResourceGroupName -Name $siteName -AppSettings $hash

    #Add each web site to the traffic manager endpoint
    $endpoint = Get-AzureRmTrafficManagerEndpoint -Name $site.Location -Type AzureEndpoints -ProfileName $tmProfileName -ResourceGroupName $ResourceGroupName -ErrorAction Ignore
    
    if($endpoint -eq $null)
    {   
        Add-AzureRmTrafficManagerEndpointConfig -TrafficManagerProfile $tmProfile -EndpointName $site.Location -EndpointStatus Enabled -Type AzureEndpoints -TargetResourceId $site.Id
        $added = $true
    }
} 
if($added)
{
    Set-AzureRmTrafficManagerProfile -TrafficManagerProfile $tmProfile
}
