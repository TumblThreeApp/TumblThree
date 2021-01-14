param ([Parameter(Mandatory=$true)][string]$versionInfoFile)

$RegularExpression = [regex] 'AssemblyFileVersion\(\"(.*)\"\)'
$fileContent = Get-Content $versionInfoFile
foreach($content in $fileContent)
{
    $match = [System.Text.RegularExpressions.Regex]::Match($content, $RegularExpression)
    if($match.Success) {
        $match.groups[1].value
    }
}