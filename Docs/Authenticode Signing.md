# Authenticode Signing for `setup.exe`

Official Project Hope Lynden release installers are intended to be signed by the verified publisher **Vyper Industries** through Microsoft Azure Artifact Signing.

Azure Artifact Signing is a managed signing service. Microsoft manages the certificate lifecycle and protects the private key in managed hardware security modules. The repository does not store an exportable PFX certificate or certificate password.

## Publisher metadata is not a signature

The Inno Setup values `AppPublisher`, `VersionInfoCompany`, and related metadata describe an installer, but they do not prove who produced it. Windows can still show **Publisher: Unknown** for an unsigned installer.

The release workflow sets the installer publisher metadata to `Vyper Industries`. After Azure identity validation and configuration are complete, the Authenticode certificate must also show the approved Vyper Industries identity so the metadata and verified publisher are aligned.

A valid signature gives Windows a verifiable publisher identity, but it does not guarantee that SmartScreen or Smart App Control will never display a warning. Reputation can still take time to develop.

## Step 0: Confirm the Vyper Industries DBA records

Microsoft supports public identity validation for an organization or DBA, but it validates that identity against current business records.

Before creating the Azure signing identity, confirm that:

- `Vyper Industries` is registered as the active Washington trade name/DBA.
- The associated legal owner or business entity is correct.
- The Washington Unified Business Identifier (UBI) and business address are current.
- A website for Vyper Industries is available and identifies the business.
- Two different email addresses on the website's domain can receive external verification links.
- The person completing validation has current government-issued identification and is authorized to represent the business.

Washington requires a business license when doing business under a name other than a person's full legal name. The Washington Business License Application can register or change a trade name and provides the business with a UBI number.

Do not proceed past Azure's **Certificate subject preview** unless it displays the intended verified Vyper Industries identity. If the preview is wrong, correct the business or billing records before creating the certificate profile.

## Step 1: Create the Azure foundation

You need:

- A Microsoft Entra tenant.
- An Azure subscription whose billing-account type supports organization/DBA validation.
- Permission to register Azure resource providers and assign roles.

In the Azure portal:

1. Open **Subscriptions**.
2. Select the subscription that will own the signing service.
3. Select **Resource providers**.
4. Find `Microsoft.CodeSigning`.
5. Select **Register**.

## Step 2: Create the Artifact Signing account

In the Azure portal:

1. Search for **Artifact Signing Accounts**.
2. Select **Create**.
3. Select the Azure subscription.
4. Create or select a resource group.
5. Enter a globally unique account name, such as `vyperindustries-signing` if available.
6. Select a supported US region.
7. Select the **Basic** pricing tier unless requirements change.
8. Review the displayed price before creating the billable resource.
9. Select **Review + Create**, then **Create**.

Record these values for later:

- Azure subscription ID
- Artifact Signing account name
- Artifact Signing endpoint for the selected region

## Step 3: Validate the Vyper Industries DBA identity

Identity validation is completed in the Azure portal and cannot be completed by the GitHub workflow.

1. Open the Artifact Signing account.
2. Confirm that your Azure identity has the **Artifact Signing Identity Verifier** role.
3. Open **Identity validations**.
4. Select **Organization**, then **New Identity**, then **Public**.
5. Enter the requested organization/DBA information.

Microsoft currently requests information including:

- Legal business entity associated with the DBA
- Website URL
- Primary and secondary business email addresses
- Business identifier, such as the applicable UBI or other accepted identifier
- Business address
- The legal first and last name of the representative completing identity verification

Use the **Certificate subject preview** to confirm the exact identity that Windows will see. The desired result is a subject that clearly identifies **Vyper Industries** as the validated publisher.

Validation can require additional current business documents. Keep the public registration, website, domain ownership, email domain, and submitted business details consistent.

## Step 4: Create a Public Trust certificate profile

After identity validation is completed:

1. Open the Artifact Signing account.
2. Open **Certificate profiles**.
3. Select **Create**.
4. Select **Public Trust**.
5. Enter a profile name such as `VyperIndustriesPublicTrust`.
6. Select the completed Vyper Industries identity under **Verified CN and O**.
7. Review the generated certificate-subject preview again.
8. Create the profile.

Record the certificate profile name.

## Step 5: Create the GitHub workload identity

The workflow uses GitHub OpenID Connect (OIDC). It does not use an Azure client secret.

In Microsoft Entra ID:

1. Create an app registration for Project Hope Lynden GitHub releases.
2. Record its **Application (client) ID** and the tenant ID.
3. Add a **Federated credential** for GitHub Actions.
4. Restrict the credential to this repository and release-tag workflow context.
5. Assign the workload identity the **Artifact Signing Certificate Profile Signer** role for the selected certificate profile or the narrowest practical Artifact Signing scope.

The federated credential must match the GitHub subject used by tag releases. Review the generated subject carefully before saving it.

## Step 6: Configure GitHub Actions

Open the GitHub repository and go to:

**Settings → Secrets and variables → Actions**

Create these repository secrets:

| Secret | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | Application/client ID of the Entra app registration. |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID. |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID that owns the signing account. |

Create these repository variables:

| Variable | Value |
| --- | --- |
| `ARTIFACT_SIGNING_ENDPOINT` | Regional endpoint such as `https://wus2.codesigning.azure.net/`. |
| `ARTIFACT_SIGNING_ACCOUNT_NAME` | Artifact Signing account name. |
| `ARTIFACT_SIGNING_CERTIFICATE_PROFILE_NAME` | Public Trust certificate profile name. |

Do not create the old PFX secrets:

- `AUTHENTICODE_CERTIFICATE_BASE64`
- `AUTHENTICODE_CERTIFICATE_PASSWORD`

They are not used by the Azure Artifact Signing workflow and should be deleted if they were previously created.

## Release behavior

For a `vX.X.X.X` tag:

1. The shared workflow builds the application and unsigned `setup.exe`.
2. The signing job checks all three Azure secrets and all three Artifact Signing variables.
3. With no Azure signing configuration, signing is skipped and the unsigned release remains available.
4. With partial configuration, the signing job fails and reports the missing setting names.
5. With complete configuration, GitHub authenticates to Azure through OIDC.
6. `azure/artifact-signing-action` signs `setup.exe` with SHA-256 and Microsoft's RFC 3161 timestamp service.
7. PowerShell verifies that the resulting Authenticode signature is valid.
8. The signed installer is uploaded as a workflow artifact.
9. The existing GitHub Release `setup.exe` asset is replaced with the verified signed installer.

## First signed release

After the Azure resources and GitHub configuration are complete, merge the signing workflow and create a new release tag. Do not reuse an existing tag.

Example:

```powershell
git switch main
git pull --ff-only
git tag -a v0.0.9.3 -m "Release v0.0.9.3"
git push origin v0.0.9.3
```

Use the next version that is actually available at release time.

## Local verification

After downloading the installer from the GitHub Release:

```powershell
Get-AuthenticodeSignature .\setup.exe |
    Format-List Status, StatusMessage, SignerCertificate
```

The expected result is `Status : Valid`, and the signer certificate should show the approved Vyper Industries identity.

Windows Explorer also displays the signature under **Properties → Digital Signatures**.

## Official references

- Microsoft Learn: `https://learn.microsoft.com/azure/artifact-signing/overview`
- Microsoft setup quickstart: `https://learn.microsoft.com/azure/artifact-signing/quickstart`
- Microsoft GitHub action: `https://github.com/Azure/artifact-signing-action`
- Washington business licensing: `https://dor.wa.gov/open-business/apply-business-license`
