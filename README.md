# Introduction 
Azure ClickOnce Sign Tool

# Compiling to a single EXE for deployment
If you want a single exe for distribution (windows platform), run the powershell script in the root dir. This simply uses ilrepack (the modern ilmerge)
to combine the build exe with the dlls. The script runs from debug, but easy to edit to run from release. It creates a file called AzureKeyVaultSigner.exe

# Usage (If using the compiled exe above, the executable is AzureKeyVaultSigner)

AzureSignToolClickOnce.exe ^\
 -p=bin\Release\app.publish\^\
 -azure-key-vault-url=https://1234-vault.vault.azure.net/^\
 -azure-key-vault-client-id=1234^\
 -azure-key-vault-client-secret=1234^\
 -azure-key-vault-tenant-id=1234^\
 -azure-key-vault-certificate=MyGlobalSignCert^\
 -timestamp-sha2=http://timestamp.globalsign.com/?signature=sha2^\
 -timestamp-rfc3161=http://rfc3161timestamp.globalsign.com/advanced^\
 -description=MyApp^


More info on:
https://www.davici.nl/blog/clickonce-signing-from-azure-devops-via-azure-key-vault
