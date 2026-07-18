<#
.SYNOPSIS
  Enables or disables Windows loopback "large MTU" - the setting that gates localhost
  stream throughput for the RpcBenchmark.

.DESCRIPTION
  Windows exposes a global loopback large-MTU setting (per IPv4/IPv6):

    netsh int ipv4|ipv6 set gl loopbacklargemtu=enable|disable

  Enabling it roughly DOUBLES localhost stream throughput and is required to reproduce the
  documented RpcBenchmark stream numbers (e.g. Stream100 ~43M vs ~19-25M when disabled). On some
  Windows 11 builds the effective default throttles loopback throughput, so this must be enabled
  explicitly for representative stream results.

  IMPORTANT TRADE-OFF: large MTU also breaks the many-short-connection HTTP loopback path - it can
  HANG the plain Benchmark (Benchmark.csproj) HTTP/DB tests. So:
    * RpcBenchmark  -> run with large MTU ENABLED  (streams need it; gRPC/SignalR are fine)
    * Benchmark     -> run with large MTU DISABLED (its HTTP/DB path hangs otherwise)

  The change is system-wide (IPv4 + IPv6) and PERSISTS across reboots. It requires an ELEVATED
  ("Run as administrator") PowerShell; without elevation the change is impossible and this reports
  an error.

.PARAMETER Action
  'enable' (aliases: on/true) or 'disable' (aliases: off/false).
  If omitted, the script prints the current state and asks interactively.

.EXAMPLE
  .\Set-LoopbackMode.ps1 enable      # before RpcBenchmark (streams)

.EXAMPLE
  .\Set-LoopbackMode.ps1 disable     # before Benchmark.csproj, or to restore the default

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
    if (Test-Path Variable:\IsWindows) { return $IsWindows }
    return $true  # Windows PowerShell 5.1 has no $IsWindows
}

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-LargeMtu {
    $out = netsh int ipv4 show global
    (($out | Select-String 'Loopback Large Mtu') -split ':')[-1].Trim()
}

if (-not (Test-IsWindows)) {
    Write-Error "Loopback large MTU is a Windows-only setting; nothing to do on this OS."
    exit 2
}

# Resolve the desired action (prompt if not provided).
if ([string]::IsNullOrWhiteSpace($Action)) {
    Write-Host "Current loopback large MTU: $(Get-LargeMtu)"
    $ans = Read-Host "ENABLE large MTU (throughput; for RpcBenchmark streams)? [Y]es / [N]o (disable) / [C]ancel"
    switch -Regex ($ans.Trim()) {
        '^(y|yes|e|enable|on)$'   { $Action = 'enable' }
        '^(n|no|d|disable|off)$'  { $Action = 'disable' }
        default { Write-Host "Cancelled - no changes made."; exit 0 }
    }
}

switch ($Action.Trim().ToLowerInvariant()) {
    { $_ -in 'enable','on','true' }   { $enable = $true }
    { $_ -in 'disable','off','false' } { $enable = $false }
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

$value = if ($enable) { 'enable' } else { 'disable' }
Write-Host "Applying loopbacklargemtu=$value (IPv4 + IPv6)..."
foreach ($fam in 'ipv4', 'ipv6') {
    & netsh int $fam set gl loopbacklargemtu=$value | Out-Null
}

# Verify the change actually took effect.
Start-Sleep -Milliseconds 500
$after = Get-LargeMtu
$expected = if ($enable) { 'enabled' } else { 'disabled' }
if ($after -ieq $expected) {
    Write-Host ("SUCCESS: loopback large MTU is now {0}." -f $after.ToUpper()) -ForegroundColor Green
    if ($enable) {
        Write-Host "Reminder: run Benchmark.csproj with large MTU DISABLED (its HTTP/DB tests hang otherwise)." -ForegroundColor Yellow
    }
    exit 0
}
else {
    Write-Error "Verification FAILED - large MTU is '$after', expected '$expected'. (Group Policy or another agent may be overriding it.)"
    exit 3
}
