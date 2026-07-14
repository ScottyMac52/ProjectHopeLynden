# Authenticode Signing for `setup.exe`

Project Hope release tags can optionally Authenticode-sign the generated Inno Setup installer.

## Publisher metadata is not a signature

The Inno Setup values `AppPublisher`, `VersionInfoCompany`, and related metadata describe the installer, but they do not prove who produced it. Windows can still show **Publisher: Unknown** for an unsigned installer even when those values are present.

Authenticode signing adds a cryptographic signature backed by a code-signing certificate. Windows uses the certificate subject as the verified publisher identity when the certificate chain is trusted.

A valid signature gives Smart App Control and SmartScreen the strongest identity information available from the project, but signing alone does not guarantee immediate reputation or prevent every warning.

## Required GitHub Actions secrets

Configure both repository secrets under **Settings → Secrets and variables → Actions**:

| Secret | Purpose |
| --- | --- |
| `AUTHENTICODE_CERTIFICATE_BASE64` | Base64-encoded contents of the `.pfx` code-signing certificate file. |
| `AUTHENTICODE_CERTIFICATE_PASSWORD` | Password protecting the `.pfx` private key. |

The certificate must include its private key and should be issued by a certificate authority trusted by the target Windows machines.

Never commit the `.pfx`, its Base64 value, its password, or any private-key material to the repository.

## Encoding the certificate

From PowerShell on the secure machine that holds the certificate:

```powershell
$pfxPath = "C:\Secure\ProjectHope-CodeSigning.pfx"
$base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
$base64 | Set-Clipboard
```

Paste the clipboard contents into the `AUTHENTICODE_CERTIFICATE_BASE64` repository secret. Store the `.pfx` password separately in `AUTHENTICODE_CERTIFICATE_PASSWORD`.

## Optional timestamp variable

The workflow uses `http://timestamp.digicert.com` by default. A different RFC 3161 timestamp service can be configured as the repository variable:

```text
AUTHENTICODE_TIMESTAMP_URL
```

Timestamping allows Windows to validate that the installer was signed while the certificate was valid, even after the certificate later expires.

## Release behavior

For a `vX.X.X.X` tag:

1. The shared workflow builds the application and unsigned `setup.exe`.
2. The Project Hope signing job checks whether both signing secrets exist.
3. When both exist, it downloads the installer artifact, signs with SHA-256, applies an RFC 3161 timestamp, and verifies the signature twice.
4. The verified signed installer is uploaded as a workflow artifact.
5. The existing `setup.exe` asset on the GitHub Release is replaced with the signed file.

When neither signing secret exists, the signing job reports that signing was skipped and leaves the unsigned installer produced by the shared workflow in place. This preserves local, development, and test release capability without a certificate.

If only one of the two required secrets is configured, the signing job fails with a clear configuration error rather than silently publishing an incorrectly configured signed release.

## Local verification

After downloading an official signed installer:

```powershell
Get-AuthenticodeSignature .\setup.exe | Format-List Status,StatusMessage,SignerCertificate
```

A successful official release should report `Status : Valid` and show the expected certificate subject. Windows Explorer also exposes the signature under **Properties → Digital Signatures**.
