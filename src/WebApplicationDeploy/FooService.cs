using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;

namespace WebApplicationDeploy
{
    public class FooService : IFooService
    {
        private readonly ILogger<FooService> logger;

        public FooService(ILogger<FooService> logger)
        {
            this.logger = logger;
        }

        public void DoWork()
        {
            logger.LogInformation($"{nameof(DoWork)} started.");
            Debug.WriteLine("Test 2");

            string solutionFile = @"C:\Users\manhn\Downloads\net6-auth\src\WebApplication.sln";
            string solutionName = Path.GetFileNameWithoutExtension(solutionFile);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string solutionFolder = Path.GetDirectoryName(solutionFile);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            Print($"Solution Name: { solutionName }");
            Print($"Solution Folder: { solutionFolder }");
            string projectFile = @"C:\Users\manhn\Downloads\net6-auth\src\WebApplication\WebApplication.csproj";
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string projectFolder = Path.GetDirectoryName(projectFile);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            Print($"Project Name: { projectName }");
            Print($"Project Folder: { projectFolder }");
            GitUtil.Pull(solutionFolder);
            string hostName = projectName.PrettyName();
            Print($"Host Name: { hostName }");
            string siteName = hostName;
            Print($"Site Name: { siteName }");
            string physicalPath = $@"{Environment.ExpandEnvironmentVariables("%SystemDrive%")}\inetpub\wwwroot\{siteName}";
            Print($"Physical Path: { physicalPath }");
            Directory.CreateDirectory(physicalPath);
            IISManagerUtil.StopSite(siteName);
            string publishProfile = $@"{projectFolder}\Properties\PublishProfiles\FolderProfile.pubxml";
            DotNetCoreUtil.Publish(projectFile, publishProfile, physicalPath);
            HostsFileUtil.SaveHosting(hostName);
            string appPool = siteName;
            Print($"Application Pool: { appPool }");
            IISManagerUtil.AddAppPool(appPool, true);
            IISManagerUtil.AddSite(siteName, appPool, physicalPath, true, 443);
            IISManagerUtil.StartSite(siteName);
            IISManagerUtil.RecycleAppPool(appPool);
            IISManagerUtil.OpenSite(siteName, true, 443);
            logger.LogInformation($"{nameof(DoWork)} finished.");
        }

        public static void Print(object obj, string prefix = null)
        {
            if (prefix != null) Debug.Write(prefix);
            Debug.WriteLine(obj);

            if (prefix != null) Console.Write(prefix);
            Console.WriteLine(obj);
        }
    }

    public static class GitUtil
    {
        public static void Pull(string solutionFolder)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.WorkingDirectory = solutionFolder;
            startInfo.FileName = "CMD";
            var args = $@"/c git pull origin master";
            startInfo.Arguments = args;
            FooService.Print(args.Replace(@"/c", ""));
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            FooService.Print(output);
            process.WaitForExit();
        }
    }

    public static class DotNetCoreUtil
    {
        public static void Publish(string projectFile, string publishProfile, string outputFolder)
        {
            string programFiles = IISManagerUtil.ProgramFilesx86().Replace(" (x86)", "");
            string dotNet = $@"{programFiles}\dotnet\dotnet";
            string args = $" publish \"{projectFile}\" -c Release /p:PublishProfile=\"{publishProfile}\" -o \"{outputFolder}\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(dotNet));
            ProcessUtil.RunCommand(dotNet, args);
        }
    }

    public static class IISManagerUtil
    {
        public static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
#pragma warning disable CS8603 // Possible null reference return.
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
#pragma warning restore CS8603 // Possible null reference return.
            }

#pragma warning disable CS8603 // Possible null reference return.
            return Environment.GetEnvironmentVariable("ProgramFiles");
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static void AddAppPool(string siteName, bool isNetCore)
        {
            string adminCmd = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD";
            string appPool = siteName;
            string runtimeVersion = isNetCore ? string.Empty : "v4.0";
            string args = $" add apppool /name:\"{appPool}\" /managedRuntimeVersion:\"{runtimeVersion}\" /managedPipelineMode:\"Integrated\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);
        }

        public static void AddSite(string siteName, string appPool, string physicalPath, bool isUseSsl, int port)
        {
            string adminCmd = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD";
            string programFilesx86 = ProgramFilesx86();

            // Assign Site ID
            int siteId = GetMaxSiteId() + 1;

            // Assign Bindings
            string bindings = $"http/*:{port}:{appPool}";
            if (isUseSsl)
            {
                bindings = $"https/*:{port}:{appPool}";
            }

            // Add Site
            string args = $" add site /name:\"{siteName}\" /id:{siteId} /physicalPath:\"{physicalPath}\" /bindings:{bindings}";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);

            // Edit site binding ssl certificate
            if (isUseSsl)
            {
                string iisExpressAdminCmd = $@"{programFilesx86}\IIS Express\IISExpressAdminCmd";
                args = $" setupsslUrl -url:https://{siteName}:{port} -UseSelfSigned";
                FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
                ProcessUtil.RunCommand(iisExpressAdminCmd, args);
            }

            // Set full permission to folders during WebDeploy
            SetFullPermission(physicalPath);

            // Set Application Pool to App
            args = $" set app \"{siteName}/\" /applicationPool:\"{appPool}\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);
        }

        public static void RecycleAppPool(string appPool)
        {
            string adminCmd = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD";
            string args = $" recycle apppool \"{appPool}\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);
        }

        public static void ResetIIS()
        {
            string iisreset = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\System32\iisreset";
            ProcessUtil.RunCommand(iisreset, string.Empty);
        }

        public static void OpenSite(string siteName, bool isUseSsl, int port)
        {
            string programFilesx86 = ProgramFilesx86();
            string msedge = $@"{programFilesx86}\Microsoft\Edge\Application\msedge";
            string swaggerIndex = $@"http://{siteName}:{port}/swagger/index.html";
            if (isUseSsl)
            {
                swaggerIndex = $@"https://{siteName}:{port}/swagger/index.html";
            }
            ProcessUtil.RunCommand(msedge, swaggerIndex);
        }

        public static string PrettyName(this string projectName)
        {
            string hostName = projectName;
            if (!projectName.EndsWith(".vn", StringComparison.OrdinalIgnoreCase))
            {
                hostName = $"{projectName}.vn";
            }
            return hostName;
        }

        private static void SetFullPermission(string folderPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();

            var groupNames = new[]
            {
                @".\IIS_IUSRS",
            };

            foreach (var groupName in groupNames)
            {
                directorySecurity.AddAccessRule(
                    new FileSystemAccessRule(groupName,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow));
                directoryInfo.SetAccessControl(directorySecurity);
            }
        }

        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = string.Empty;
            var i = 0;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    FooService.Print($"IP Address: {ip}");
                    i++; if (i == 2) ipAddress = ip.ToString();
                }
            }
            return ipAddress;
        }

        /// <summary>
        /// Get Max Id of Site in IIS Manager
        /// </summary>
        /// <returns></returns>
        private static int GetMaxSiteId()
        {
            int maxId = 0;
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = " list sites",
                    CreateNoWindow = true,
                    FileName = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string output = proc.StandardOutput.ReadLine();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                if (!string.IsNullOrWhiteSpace(output))
                {
                    string str = output.Split(new[] { "id:" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    str = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (int.TryParse(str, out int id) && id > maxId)
                    {
                        maxId = id;
                    }
                }
            }
            return maxId;
        }

        public static void StartSite(string siteName)
        {
            string adminCmd = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD";
            string args = $" start sites \"{ siteName }\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);
        }

        public static void StopSite(string siteName)
        {
            string adminCmd = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\system32\inetsrv\APPCMD";
            string args = $" stop sites \"{ siteName }\"";
            FooService.Print(args, Path.GetFileNameWithoutExtension(adminCmd));
            ProcessUtil.RunCommand(adminCmd, args);
        }
    }

    public static class ProcessUtil
    {
        /// <summary>
        /// Run a specific program from command prompt
        /// Cannot mix synchronous and asynchronous operation on process stream.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="args"></param>
        public static string RunCommand(string filePath, string args, string workingDirectory = null)
        {
            StringBuilder sb = new StringBuilder();
            args = !string.IsNullOrEmpty(args) ? $" {args.Trim()}" : string.Empty;
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = args,
                    CreateNoWindow = true,
                    FileName = filePath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            if (workingDirectory != null)
            {
                proc.StartInfo.WorkingDirectory = workingDirectory;
            }
            FooService.Print(proc.StartInfo.Arguments, Path.GetFileNameWithoutExtension(filePath));
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                sb.AppendLine(proc.StandardOutput.ReadLine());
            }
            string output = sb.ToString();
            FooService.Print(output);
            return output;
        }
    }

    /// <summary>
    /// The class utility work with file C:\Windows\System32\drivers\etc\hosts
    /// </summary>
    public static class HostsFileUtil
    {
        /// <summary>
        /// Create new host name in C:\Windows\System32\drivers\etc\hosts
        /// --------------------------------------------------------------
        /// Open the Command Prompt with Administrative Privileges
        /// notepad C:\Windows\System32\drivers\etc\hosts
        /// 127.0.0.1       mysite.vn
        /// ::1             mysite.vn
        /// </summary>
        /// <param name="hostName"></param>
        public static void SaveHosting(string hostName)
        {
            string[] lines = new[]
            {
                "127.0.0.1       {0}",
                "::1             {0}"
            };
            string hostsFilePath = $@"{Environment.ExpandEnvironmentVariables("%WinDir%")}\System32\drivers\etc\hosts";
            string hostsFileContent = File.ReadAllText(hostsFilePath, Encoding.UTF8);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Environment.NewLine);
            foreach (var line in lines)
            {
                string s = string.Format(line, hostName);
                if (!hostsFileContent.Contains(s))
                {
                    sb.AppendLine(s);
                }
            }
            string strAddress = $"{IISManagerUtil.GetLocalIpAddress()}             {hostName}";
            if (!hostsFileContent.Contains(strAddress))
            {
                sb.AppendLine(strAddress);
            }
            var str = sb.ToString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                FileUtil.AppendText(hostsFilePath, str);
            }
        }
    }

    /// <summary>
    /// The class utility extend of System.IO.File
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Append text to an existing file
        /// </summary>
        /// <param name="filePath"></param>
        public static void AppendText(string filePath, string line)
        {
            using (StreamWriter file = new StreamWriter(filePath, append: true))
            {
                file.WriteLine(line);
            }
        }
    }
}