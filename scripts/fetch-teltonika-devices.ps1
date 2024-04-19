
$ErrorActionPreference = 'SilentlyContinue'

function ConvertFrom-HTMLTable {
    <#
    .SYNOPSIS
    Function for converting ComObject HTML object to common PowerShell object.

    .DESCRIPTION
    Function for converting ComObject HTML object to common PowerShell object.
    ComObject can be retrieved by (Invoke-WebRequest).parsedHtml or IHTMLDocument2_write methods.

    In case table is missing column names and number of columns is:
    - 2
        - Value in the first column will be used as object property 'Name'. Value in the second column will be therefore 'Value' of such property.
    - more than 2
        - Column names will be numbers starting from 1.

    .PARAMETER table
    ComObject representing HTML table.

    .PARAMETER tableName
    (optional) Name of the table.
    Will be added as TableName property to new PowerShell object.

    .EXAMPLE
    $pageContent = Invoke-WebRequest -Method GET -Headers $Headers -Uri "https://docs.microsoft.com/en-us/mem/configmgr/core/plan-design/hierarchy/log-files"
    $table = $pageContent.ParsedHtml.getElementsByTagName('table')[0]
    $tableContent = @(ConvertFrom-HTMLTable $table)

    Will receive web page content >> filter out first table on that page >> convert it to PSObject

    .EXAMPLE
    $Source = Get-Content "C:\Users\Public\Documents\MDMDiagnostics\MDMDiagReport.html" -Raw
    $HTML = New-Object -Com "HTMLFile"
    $HTML.IHTMLDocument2_write($Source)
    $HTML.body.getElementsByTagName('table') | % {
        ConvertFrom-HTMLTable $_
    }

    Will get web page content from stored html file >> filter out all html tables from that page >> convert them to PSObjects
    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [System.__ComObject] $table,

        [string] $tableName
    )

    $twoColumnsWithoutName = 0

    if ($tableName) { $tableNameTxt = "'$tableName'" }

    $columnName = $table.getElementsByTagName("th") | % { $_.innerText -replace "^\s*|\s*$" }

    if (!$columnName) {
        $numberOfColumns = @($table.getElementsByTagName("tr")[0].getElementsByTagName("td")).count
        if ($numberOfColumns -eq 2) {
            ++$twoColumnsWithoutName
            Write-Verbose "Table $tableNameTxt has two columns without column names. Resultant object will use first column as objects property 'Name' and second as 'Value'"
        }
        elseif ($numberOfColumns) {
            Write-Warning "Table $tableNameTxt doesn't contain column names, numbers will be used instead"
            $columnName = 1..$numberOfColumns
        }
        else {
            throw "Table $tableNameTxt doesn't contain column names and summarization of columns failed"
        }
    }

    if ($twoColumnsWithoutName) {
        # table has two columns without names
        $property = [ordered]@{ }

        $table.getElementsByTagName("tr") | % {
            # read table per row and return object
            $columnValue = $_.getElementsByTagName("td") | % { $_.innerText -replace "^\s*|\s*$" }
            if ($columnValue) {
                # use first column value as object property 'Name' and second as a 'Value'
                $property.($columnValue[0]) = $columnValue[1]
            }
            else {
                # row doesn't contain <td>
            }
        }
        if ($tableName) {
            $property.TableName = $tableName
        }

        New-Object -TypeName PSObject -Property $property
    }
    else {
        # table doesn't have two columns or they are named
        $table.getElementsByTagName("tr") | % {
            # read table per row and return object
            $columnValue = $_.getElementsByTagName("td") | % { $_.innerText -replace "^\s*|\s*$" }
            if ($columnValue) {
                $property = [ordered]@{ }
                $i = 0
                $columnName | % {
                    $property.$_ = $columnValue[$i]
                    ++$i
                }
                if ($tableName) {
                    $property.TableName = $tableName
                }

                New-Object -TypeName PSObject -Property $property
            }
            else {
                # row doesn't contain <td>, its probably row with column names
            }
        }
    }
}

$Devices = @(
    #"FM1100"
    #"FM2200"
    #"FM3200"
    #"FM3400"
    #"FMB010"
    #"FMB003"
    #"FMB130"
    #"FMB140"
    #"FMB204"
    #"FMB930"
    #"FMT100"
    "TFT100"
)

$Properties = @()
$Failed = @()

$ErrorActionPreference = 'Continue'

foreach ($device in $Devices) {
    try {
        if ($device -ne "TFT100") {
            $PageContent = Invoke-WebRequest -Method GET -Uri "https://wiki.teltonika-gps.com/view/$($Device)_Teltonika_Data_Sending_Parameters_ID"
        }
        else {
            $PageContent = Invoke-WebRequest -Method GET -Uri "https://wiki.teltonika-gps.com/view/TFT100_AVL_ID_List"
        }
    }
    catch {
        $Failed += $device
        continue
    }

    if ($null -eq $PageContent) {
        $Failed += $device
        continue
    }

    $Tables = $PageContent.ParsedHtml.getElementsByTagName("table")

    $Tables | ForEach-Object { Write-Host $_.ClassName }

    if ($null -eq $Tables) {
        $Failed += $device
        continue
    }

    $Objects = $Tables | ForEach-Object { ConvertFrom-HTMLTable -table $_ }

    if ($null -eq $Objects) {
        $Failed += $device
        continue
    }

    foreach ($row in $Objects) {
        $rowType = 'sbyte'
        $multiplier = $null
        $unit = $null

        switch ($row.'Bytes') {
            '1' { $rowType = 'sbyte' }
            '2' { $rowType = 'short' }
            '4' { $rowType = 'int' }
            '8' { $rowType = 'long' }
        }

        if ($row.'Type' -eq 'Unsigned') {
            if ($rowType -eq 'sbyte') {
                $rowType = 'byte'
            }
            else {
                $rowType = "u$($rowType)"
            }
        }
        
        if ($row.'Units' -ne '-') {
            $multiplier = $row.'Units'
        }

        if ($row.'Description' -ne '-') {
            $unit = $row.'Description'
        }

        if ($null -eq $multiplier) {
            $multiplier = "default"
        }
        else {
            $multiplier = "$($multiplier)d"
        }

        if ($null -eq $unit) {
            $unit = "default"
        }
        else {
            $unit = "`"$unit`""
        }

        $prop = @{
            "Id"         = $row.'Property ID In AVL Packet'
            "Name"       = $row.'Property Name'
            "Multiplier" = $multiplier
            "Unit"       = $unit
            "Type"       = $rowType
        }

        if ($device -eq "TFT100") {
            Add-Content -Path "output-tft100.txt" -Value "Register<$($prop.Type)>($($prop.Id), `"$($prop.Name)`", $($prop.Multiplier), $($prop.Unit));"
        }
        else {
            $Properties += New-Object psobject -Property $prop
        }
    }
}

foreach ($fail in $Failed) {
    Write-Host "Failed: $fail"
}

$dict = [ordered]@{}
$handled = @()

foreach ($entry in $Properties.GetEnumerator()) {
    if ($null -eq $dict[$entry.Id]) {
        $dict[$entry.Id] = $entry
    }
    else {
        $cur = $dict[$entry.Id]

        if ($cur.Name -ne $entry.Name) {
            if (!$handled.Contains($entry.Name)) {
                Write-Host "$($cur.Id): $($cur.Name) != $($entry.Name)"
                $handled += $entry.Name
            }
        }
    }
}

Write-Host "Got $($dict.Count) properties"

foreach ($entry in $dict.GetEnumerator() | Sort-Object { [int]$_.Key }) {
    Add-Content -Path "output.txt" -Value "Register<$($entry.Value.Type)>($($entry.Value.Id), `"$($entry.Value.Name)`", $($entry.Value.Multiplier), $($entry.Value.Unit));"
}