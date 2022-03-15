using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using AzureSign.Core;
using AzureSignToolClickOnce.Utils;
using RSAKeyVaultProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AzureSignToolClickOnce.Services
{
    public class AzureSignToolService
    {
        private string _magetoolPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\mage.exe";
        public void Start(string description, string path, string timeStampUrl, string timeStampUrlRfc3161, string keyVaultUrl, string tenantId, string clientId, string clientSecret, string certName)
        {
            var tokenCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var client = new CertificateClient(vaultUri: new Uri(keyVaultUrl), credential: tokenCredential);
            var cert = client.GetCertificate(certName).Value;
            var certificate = new X509Certificate2(cert.Cer);
            var keyIdentifier = cert.KeyId;
            var rsa = RSAFactory.Create(tokenCredential, keyIdentifier, certificate);

            // We need to be explicit about the order these files are signed in. The data files must be signed first
            // Then the .manifest file
            // Then the nested clickonce/vsto file
            // finally the top-level clickonce/vsto file

            var files = Directory.GetFiles(path, "*.*").ToList();
            files.AddRange(Directory.GetFiles(path + @"\Application Files", "*.*", SearchOption.AllDirectories));
            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                files[i] = file.Replace(@"\\", @"\");
            }

            var filesToSign = new List<string>();
            var setupExe = files.Where(f => ".exe".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase));
            filesToSign.AddRange(setupExe);

            var manifestFile = files.SingleOrDefault(f => ".manifest".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(manifestFile))
            {
                Console.WriteLine("No manifest file found");
                return;
            }

            // sign the exe files
            SignInAzureVault(description, "", timeStampUrlRfc3161, certificate, rsa, filesToSign);

            // look for the manifest file and sign that
            var args = "-a sha256RSA";
            var fileArgs = $@"-update ""{manifestFile}"" {args}";
            if (!RunMageTool(fileArgs, manifestFile, rsa, certificate, timeStampUrl))
                return;

            // Now sign the inner vsto/clickonce file
            // Order by desending length to put the inner one first
            var clickOnceFilesToSign = files
                                            .Where(f => ".vsto".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase) ||
                                                        ".application".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
                                            .Select(f => new { file = f, f.Length })
                                            .OrderByDescending(f => f.Length)
                                            .Select(f => f.file)
                                            .ToList();

            foreach (var f in clickOnceFilesToSign)
            {
                fileArgs = $@"-update ""{f}"" {args} -appm ""{manifestFile}""";
                if (!RunMageTool(fileArgs, f, rsa, certificate, timeStampUrl))
                {
                    throw new Exception($"Could not sign {f}");
                }
            }
        }

        private void SignInAzureVault(string description, string supportUrl, string timeStampUrlRfc3161, X509Certificate2 certificate, RSA rsaPrivateKey, List<string> filesToSign)
        {
            var authenticodeKeyVaultSigner = new AuthenticodeKeyVaultSigner(rsaPrivateKey, certificate, HashAlgorithmName.SHA256,
                new TimeStampConfiguration(timeStampUrlRfc3161, HashAlgorithmName.SHA256, TimeStampType.RFC3161));
            foreach (var f in filesToSign)
            {
                Console.WriteLine($"SignInAzureVault: {f}");
                authenticodeKeyVaultSigner.SignFile(f.AsSpan(), description.AsSpan(), supportUrl.AsSpan(), null);
            }
        }

        private bool RunMageTool(string args, string inputFile, RSA rsa, X509Certificate2 publicCertificate, string timestampUrl)
        {
            var signtool = new Process
            {
                StartInfo =
                {
                    FileName = _magetoolPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = args
                }
            };
            Console.WriteLine($"Signing {signtool.StartInfo.FileName} {args}");
            signtool.Start();
            signtool.WaitForExit();

            if (signtool.ExitCode == 0)
            {
                Console.WriteLine($"Manifest signing {inputFile}");
                ManifestSigner.SignFile(inputFile, rsa, publicCertificate, timestampUrl);
                return true;
            }
            else
            {
                var output = signtool.StandardOutput.ReadToEnd();
                var error = signtool.StandardError.ReadToEnd();
                Debug.WriteLine($"Mage Out {output}");
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"Mage Err {error}");
                }
            }

            Console.WriteLine($"Error: Signtool returned {signtool.ExitCode}");
            return false;
        }
    }
}
