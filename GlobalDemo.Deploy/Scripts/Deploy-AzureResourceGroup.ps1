#Requires -Version 3.0

Param(
  [string] [Parameter(Mandatory=$true)] $ResourceGroupLocation,
  [string] $ResourceGroupName = 'GlobalScaleDemo',  
  [switch] $UploadArtifacts,
  [string] $StorageAccountName,
  [string] $StorageAccountResourceGroupName, 
  [string] $StorageContainerName = $ResourceGroupName.ToLowerInvariant() + '-stageartifacts',
  [string] $TemplateFile = '..\Templates\DeploymentTemplate.json',
  [string] $TemplateParametersFile = '..\Templates\DeploymentTemplate.param.dev.json',
  [string] $ArtifactStagingDirectory = '..\bin\Debug\staging',
  [string] $AzCopyPath = '..\Tools\AzCopy.exe',
  [string] $DSCSourceFolder = '..\DSC'
)

if (Get-Module -ListAvailable | Where-Object { $_.Name -eq 'AzureResourceManager' -and $_.Version -ge '0.9.9' }) {
    Throw "The version of the Azure PowerShell cmdlets installed on this machine are not compatible with this script.  For help updating this script visit: http://go.microsoft.com/fwlink/?LinkID=623011"
}

Import-Module Azure -ErrorAction SilentlyContinue

try {
  [Microsoft.Azure.Common.Authentication.AzureSession]::ClientFactory.AddUserAgent("VSAzureTools-$UI$($host.name)".replace(" ","_"), "2.7.1")
} catch { }

Set-StrictMode -Version 3

$OptionalParameters = New-Object -TypeName Hashtable
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)

if ($UploadArtifacts)
{
    # Convert relative paths to absolute paths if needed
    $AzCopyPath = [System.IO.Path]::Combine($PSScriptRoot, $AzCopyPath)
    $ArtifactStagingDirectory = [System.IO.Path]::Combine($PSScriptRoot, $ArtifactStagingDirectory)
    $DSCSourceFolder = [System.IO.Path]::Combine($PSScriptRoot, $DSCSourceFolder)

    Set-Variable ArtifactsLocationName '_artifactsLocation' -Option ReadOnly
    Set-Variable ArtifactsLocationSasTokenName '_artifactsLocationSasToken' -Option ReadOnly

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

    if ($StorageAccountResourceGroupName) {
        Switch-AzureMode AzureResourceManager
        $StorageAccountKey = (Get-AzureStorageAccountKey -ResourceGroupName $StorageAccountResourceGroupName -Name $StorageAccountName).Key1
    }
    else {
        Switch-AzureMode AzureServiceManagement
        $StorageAccountKey = (Get-AzureStorageKey -StorageAccountName $StorageAccountName).Primary 
    }
    
    $StorageAccountContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $StorageAccountKey

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

    # Generate the value for artifacts location SAS token if it is not provided in the parameter file
    $ArtifactsLocationSasToken = $OptionalParameters[$ArtifactsLocationSasTokenName]
    if ($ArtifactsLocationSasToken -eq $null) {
       # Create a SAS token for the storage container - this gives temporary read-only access to the container (defaults to 1 hour).
       $ArtifactsLocationSasToken = New-AzureStorageContainerSASToken -Container $StorageContainerName -Context $StorageAccountContext -Permission r
       $ArtifactsLocationSasToken = ConvertTo-SecureString $ArtifactsLocationSasToken -AsPlainText -Force
       $OptionalParameters[$ArtifactsLocationSasTokenName] = $ArtifactsLocationSasToken
    }
}

# Create or update the resource group using the specified template file and template parameters file
Switch-AzureMode AzureResourceManager
New-AzureResourceGroup -Name $ResourceGroupName `
                       -Location $ResourceGroupLocation `
                       -TemplateFile $TemplateFile `
                       -TemplateParameterFile $TemplateParametersFile `
                        @OptionalParameters `
                        -Force -Verbose

						#sleep for 30 seconds to allow the GitHub publishing to work
Write-Output "Sleeping for 3 minutes to allow GitHub publishing to work"
[System.Threading.Thread]::Sleep(180000)
Write-Output "Waking back up... nice nap."

$webSites = Get-AzureResource -ResourceGroupName $ResourceGroupName -ResourceType "Microsoft.Web/sites"
#Add traffic manager
#$inputParameters = (Get-Content $TemplateParametersFile) -join "`n" | ConvertFrom-Json
#$inputParameters.parameters.uniqueDnsName

#Set the storage accounts in appSettings for the web applications

$storageAccounts = Get-AzureResource -ResourceGroupName $ResourceGroupName -ResourceType "Microsoft.Storage/storageAccounts"

foreach($site in $webSites)
{            
	$newAppSettings = New-Object Hashtable
	foreach($sa in $storageAccounts)
	{		
		$newAppSettings.Add($sa.ResourceName,("DefaultEndpointsProtocol=https;AccountName=" + $sa.ResourceName.ToLower() + ";AccountKey=" + (Get-AzureStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $sa.ResourceName.ToLower()).Key1));
	}
	Switch-AzureMode AzureServiceManagement

	$keepGoing = $true;
	do
	{
		$s = Get-AzureWebsite -Name $site.ResourceName
		Write-Output ("Getting appSettings for site " + $site.ResourceName)
        $oldAppSettings = $s.AppSettings
		if($oldAppSettings.Keys.Length -ge 6)
		{
			$keepGoing = $false;
			foreach($key in $oldAppSettings.Keys)
			{
                Write-Output ($key + " - " + $oldAppSettings[$key])

                Write-Output ("Contains key? " + $newAppSettings.ContainsKey($key))

				if($newAppSettings.ContainsKey($key) -ne $true)        
				{
					Write-Output ("Adding key " + $key + " - " + $oldAppSettings[$key])
					$newAppSettings.Add($key, $oldAppSettings[$key])
				}
			}
		}
		else
		{
			#For some reason, not all appSettings are being returned.  Try again
			Write-Output "Not all keys were returned.  Trying again."
			$keepGoing = $true;
		}

	}
	while($keepGoing)
        
    Set-AzureWebsite -Name $site.ResourceName -AppSettings $newAppSettings                

	Switch-AzureMode AzureResourceManager
}