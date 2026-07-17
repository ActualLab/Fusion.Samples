<#
.SYNOPSIS
  Enables or disables Windows TCP "loopback throughput mode" for localhost benchmarking.

.DESCRIPTION
  Windows exposes a global loopback processing setting (per IPv4/IPv6):

    loopbackexecutionmode = worker | inline | adaptive
    loopbacklargemtu      = enable | disable

  The 'inline' mode favors LATENCY over THROUGHPUT and, combined with large-MTU disabled,
  can cap localhost TCP bandwidth by ~20x (e.g. 0.7 GB/s vs 15 GB/s). On some Windows 11
  builds this is the default, which severely skews localhost benchmarks (RpcBenchmark,
  Benchmark) even though nothing changed in the app.

  This script toggles between:
    enable   -> loopbackexecutionmode=worker,  loopbacklargemtu=enable   (throughput)
    disable  -> loopbackexecutionmode=inline,   loopbacklargemtu=disable  (Windows default)

  The change is system-wide (IPv4 + IPv6) and PERSISTS across reboots.

  IMPORTANT: it requires an ELEVATED PowerShell ("Run as administrator"). Without elevation
  the change is impossible and the script reports an error.

.PARAMETER Action
  'enable' (aliases: on/worker/true) or 'disable' (aliases: off/inline/false).
  If omitted, the script prints the current state and asks interactively.

.EXAMPLE
  .\Set-LoopbackMode.ps1 enable      # throughput mode (for benchmarking)

.EXAMPLE
  .\Set-LoopbackMode.ps1 disable     # back to Windows default

.EXAMPLE
  .\Set-LoopbackMode.ps1             # prompt
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $Action
)

$ErrorActionPreference = 'Stop'

function Test-IsWindows {
    # $IsWindows exists on PowerShell 6+; on Windows PowerShell 5.1 assume Windows.
    if (Test-Path Variable:\IsWindows) { return $IsWindows }
    return $true
}

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-LoopbackState {
    $out = netsh int ipv4 show global
    $mode = (($out | Select-String 'Loopback Execution Mode') -split ':')[-1].Trim()
    $mtu  = (($out | Select-String 'Loopback Large Mtu')       -split ':')[-1].Trim()
    [pscustomobject]@{ ExecutionMode = $mode; LargeMtu = $mtu }
}

if (-not (Test-IsWindows)) {
    Write-Error "Loopback execution mode is a Windows-only setting; nothing to do on this OS."
    exit 2
}

# Resolve the desired action (prompt if not provided).
if ([string]::IsNullOrWhiteSpace($Action)) {
    $cur = Get-LoopbackState
    Write-Host "Current loopback state: ExecutionMode=$($cur.ExecutionMode), LargeMtu=$($cur.LargeMtu)"
    $ans = Read-Host "Set THROUGHPUT (worker) mode? [Y]es / [N]o (restore default) / [C]ancel"
    switch -Regex ($ans.Trim()) {
        '^(y|yes|e|enable|worker|on)$'   { $Action = 'enable' }
        '^(n|no|d|disable|inline|off)$'  { $Action = 'disable' }
        default { Write-Host "Cancelled - no changes made."; exit 0 }
    }
}

switch ($Action.Trim().ToLowerInvariant()) {
    { $_ -in 'enable','on','worker','true','throughput' }  { $enable = $true }
    { $_ -in 'disable','off','inline','false','default' }  { $enable = $false }
    default {
        Write-Error "Unknown action '$Action'. Use 'enable' or 'disable'."
        exit 2
    }
}

if (-not (Test-Admin)) {
    $verb = if ($enable) { 'enable' } else { 'disable' }
    Write-Error @"
This change requires an ELEVATED PowerShell (it edits system-wide TCP/IP settings).
Open PowerShell via 'Run as administrator', then re-run:

    .\Set-LoopbackMode.ps1 $verb
"@
    exit 1
}

$mode = if ($enable) { 'worker' } else { 'inline' }
$mtu  = if ($enable) { 'enable' } else { 'disable' }

Write-Host "Applying loopbackexecutionmode=$mode, loopbacklargemtu=$mtu (IPv4 + IPv6)..."
foreach ($fam in 'ipv4', 'ipv6') {
    & netsh int $fam set gl loopbackexecutionmode=$mode | Out-Null
    & netsh int $fam set gl loopbacklargemtu=$mtu       | Out-Null
}

# Verify the change actually took effect.
Start-Sleep -Milliseconds 500
$after = Get-LoopbackState
$expectedMtu = if ($enable) { 'enabled' } else { 'disabled' }
$ok = ($after.ExecutionMode -ieq $mode) -and ($after.LargeMtu -ieq $expectedMtu)

Write-Host "Now: ExecutionMode=$($after.ExecutionMode), LargeMtu=$($after.LargeMtu)"
if ($ok) {
    Write-Host ("SUCCESS: loopback throughput mode is now {0}." -f ($(if ($enable) { 'ENABLED (worker)' } else { 'DISABLED (inline / default)' }))) -ForegroundColor Green
    exit 0
}
else {
    Write-Error "Verification FAILED - the setting did not change to the expected value. (Group Policy or another agent may be overriding it.)"
    exit 3
}
