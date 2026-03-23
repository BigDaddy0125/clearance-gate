[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("authorize", "acknowledge")]
    [string]$Mode,

    [string]$Profile = "itops_deployment_v1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$adapterRoot = $PSScriptRoot

switch ($Mode) {
    "authorize" {
        $inputPath = Join-Path $adapterRoot "change-control-request.json"
        $input = Get-Content -Raw -Path $inputPath | ConvertFrom-Json

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
        $inputPath = Join-Path $adapterRoot "change-control-ack.json"
        $input = Get-Content -Raw -Path $inputPath | ConvertFrom-Json

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
