[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("authorize", "acknowledge")]
    [string]$Mode,

    [string]$Profile = "itops_deployment_v1",

    [string]$AuthorizeInputPath = "",

    [string]$AcknowledgeInputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$adapterRoot = $PSScriptRoot

function Resolve-CallerInputPath {
    param(
        [string]$OverridePath,
        [string]$DefaultFileName
    )

    if ([string]::IsNullOrWhiteSpace($OverridePath)) {
        return Join-Path $adapterRoot $DefaultFileName
    }

    return $OverridePath
}

function Require-PropertyValue {
    param(
        [pscustomobject]$InputObject,
        [string]$Path
    )

    $current = $InputObject
    foreach ($segment in $Path.Split(".")) {
        if ($null -eq $current) {
            throw "Caller payload is missing required property '$Path'."
        }

        $property = $current.PSObject.Properties[$segment]
        if ($null -eq $property) {
            throw "Caller payload is missing required property '$Path'."
        }

        $current = $property.Value
    }

    if ($null -eq $current) {
        throw "Caller payload property '$Path' must not be null."
    }

    if ($current -is [string] -and [string]::IsNullOrWhiteSpace($current)) {
        throw "Caller payload property '$Path' must not be empty."
    }

    if ($current -is [System.Array] -and $current.Count -eq 0) {
        throw "Caller payload property '$Path' must not be empty."
    }

    return $current
}

switch ($Mode) {
    "authorize" {
        $inputPath = Resolve-CallerInputPath -OverridePath $AuthorizeInputPath -DefaultFileName "change-control-request.json"
        $input = Get-Content -Raw -Path $inputPath | ConvertFrom-Json

        Require-PropertyValue -InputObject $input -Path "ticketId" | Out-Null
        Require-PropertyValue -InputObject $input -Path "executionId" | Out-Null
        Require-PropertyValue -InputObject $input -Path "operation.kind" | Out-Null
        Require-PropertyValue -InputObject $input -Path "operation.summary" | Out-Null
        Require-PropertyValue -InputObject $input -Path "changeWindow" | Out-Null
        Require-PropertyValue -InputObject $input -Path "riskIndicators" | Out-Null
        Require-PropertyValue -InputObject $input -Path "requester.id" | Out-Null
        Require-PropertyValue -InputObject $input -Path "source.system" | Out-Null
        Require-PropertyValue -InputObject $input -Path "source.recordedAt" | Out-Null

        $mapped = [ordered]@{
            requestId = $input.ticketId
            decisionId = $input.executionId
            profile = $Profile
            action = [ordered]@{
                type = $input.operation.kind
                description = $input.operation.summary
            }
            context = [ordered]@{
                attributes = [ordered]@{
                    changeWindow = $input.changeWindow
                }
            }
            riskFlags = @($input.riskIndicators)
            responsibility = [ordered]@{
                owner = $input.requester.id
                role = "decision_owner"
            }
            metadata = [ordered]@{
                sourceSystem = $input.source.system
                timestamp = $input.source.recordedAt
            }
        }

        $mapped | ConvertTo-Json -Depth 10
        break
    }

    "acknowledge" {
        $inputPath = Resolve-CallerInputPath -OverridePath $AcknowledgeInputPath -DefaultFileName "change-control-ack.json"
        $input = Get-Content -Raw -Path $inputPath | ConvertFrom-Json

        Require-PropertyValue -InputObject $input -Path "executionId" | Out-Null
        Require-PropertyValue -InputObject $input -Path "authority.id" | Out-Null
        Require-PropertyValue -InputObject $input -Path "authority.recordedAt" | Out-Null

        $mapped = [ordered]@{
            decisionId = $input.executionId
            acknowledger = [ordered]@{
                id = $input.authority.id
                role = "acknowledging_authority"
            }
            acknowledgment = [ordered]@{
                type = "risk_acceptance"
                timestamp = $input.authority.recordedAt
            }
        }

        $mapped | ConvertTo-Json -Depth 10
        break
    }
}
