using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DATN_AUTO_CREATE_PART
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Attach resolver first
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // 2. Check Tekla connection
            if (!CheckTeklaConnection())
            {
                MessageBox.Show("Không thể kết nối với Tekla Structures. Vui lòng mở Tekla và một Model trước khi sử dụng ứng dụng.\n\nỨng dụng vẫn sẽ mở nhưng các tính năng tạo cấu kiện sẽ không hoạt động.", 
                                "Cảnh báo kết nối Tekla", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
            }

            base.OnStartup(e);
        }

        private bool CheckTeklaConnection()
        {
            try
            {
                return IsModelConnected();
            }
            catch
            {
                return false;
            }
        }

        // Must be in a separate method to ensure AssemblyResolve is called before JIT compiles this
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool IsModelConnected()
        {
            var model = new Tekla.Structures.Model.Model();
            return model.GetConnectionStatus();
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new System.Reflection.AssemblyName(args.Name).Name;
            
            // Resolve Tekla assemblies AND their dependencies (Trimble.Remoting, System.* etc.)
            bool isTeklaAssembly = assemblyName.StartsWith("Tekla.Structures");
            bool isTeklaDependency = assemblyName.StartsWith("Trimble.") 
                                     || assemblyName == "System.Memory"
                                     || assemblyName == "System.Buffers"
                                     || assemblyName == "System.Runtime.CompilerServices.Unsafe"
                                     || assemblyName == "System.Numerics.Vectors"
                                     || assemblyName == "System.Threading.Tasks.Extensions";

            if (isTeklaAssembly || isTeklaDependency)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                
                // 1. Try to detect running Tekla version from process
                string runningVersion = GetRunningTeklaVersion();
                if (!string.IsNullOrEmpty(runningVersion))
                {
                    string path = System.IO.Path.Combine(programFiles, "Tekla Structures", runningVersion, "bin", assemblyName + ".dll");
                    if (System.IO.File.Exists(path)) return System.Reflection.Assembly.LoadFrom(path);
                }

                // 2. Fallback to existing logic if no process found
                string[] versions = { "2025.0", "2024.0", "2022.0", "2026.0" };
                foreach (var version in versions)
                {
                    string path = System.IO.Path.Combine(programFiles, "Tekla Structures", version, "bin", assemblyName + ".dll");
                    if (System.IO.File.Exists(path)) return System.Reflection.Assembly.LoadFrom(path);
                }
            }
            return null;
        }

        private string GetRunningTeklaVersion()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("TeklaStructures");
                if (processes.Length > 0)
                {
                    // Get path e.g. "C:\Program Files\Tekla Structures\2024.0\bin\TeklaStructures.exe"
                    string fullPath = processes[0].MainModule.FileName;
                    var parts = fullPath.Split(System.IO.Path.DirectorySeparatorChar);
                    for (int i = 0; i < parts.Length - 2; i++)
                    {
                        if (parts[i] == "Tekla Structures") return parts[i + 1];
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
