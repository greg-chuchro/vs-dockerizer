using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VSDockerizer {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Debug {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4129;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d596c879-6a47-4f13-9071-d0167946c1b4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="Debug"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Debug(AsyncPackage package, OleMenuCommandService commandService, DTE dte) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            this.dte = dte;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Debug Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in Debug's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE ?? throw new ArgumentNullException("dte is null");
            dte.ExecuteCommand("DebugAdapterHost.Logging", "/On /OutputWindow");
            Instance = new Debug(package, commandService, dte);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VSDockerizer.Resources.launch.json"))
            using (var reader = new StreamReader(stream)) {
                var launchJson = reader.ReadToEnd();
                dynamic launch = JObject.Parse(launchJson);
                var project = (dte.ActiveSolutionProjects as object[])[0] as Project;
                var dllName = project.Properties.Item("AssemblyName").Value;
                launch.adapterArgs = string.Format((string)launch.adapterArgs, project.Name);
                launch.configurations[0].program = string.Format((string)launch.configurations[0].program, dllName);

                var launchFileName = "launch.json";
                var launchFilePath = Path.Combine(Path.GetDirectoryName(dte.Solution.FullName), ".vs", project.Name, launchFileName);
                using (var file = File.CreateText(launchFilePath)) {
                    file.Write(JsonConvert.SerializeObject(launch));
                }

                dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:\"{launchFilePath}\"");
            }
        }
    }
}
