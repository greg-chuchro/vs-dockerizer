using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSDockerizer {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Container {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int GetCommandId = 0x0101;
        public const int SelectCommandId = 0x0102;   

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d596c879-6a47-4f13-9071-d0167946c1b4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private string[] optionsList = { "a", "b" };
        private string selectedOption = "a";

        /// <summary>
        /// Initializes a new instance of the <see cref="Container"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Container(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID getContainerListCommandId = new CommandID(CommandSet, GetCommandId);
            MenuCommand menuMyDropDownComboGetListCommand = new OleMenuCommand(new EventHandler(OnGetContainerList), getContainerListCommandId);
            commandService.AddCommand(menuMyDropDownComboGetListCommand);

            CommandID containerSelectCommandId = new CommandID(CommandSet, SelectCommandId);
            OleMenuCommand menuMyDropDownComboCommand = new OleMenuCommand(new EventHandler(OnContainerSelect), containerSelectCommandId);
            commandService.AddCommand(menuMyDropDownComboCommand);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Container Instance {
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
            Instance = new Container(package, commandService);
        }

        private void OnGetContainerList(object sender, EventArgs e) {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null) {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null) {
                    throw (new ArgumentException("inParam not null"));
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(optionsList, vOut);
                } else {
                    throw (new ArgumentException("outParam is null"));
                }
            }

        }

        private void OnContainerSelect(object sender, EventArgs e) {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null) {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero) {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(selectedOption, vOut);
                } else if (newChoice != null) {
                    // new value was selected or typed in
                    // see if it is one of our items
                    bool validInput = false;
                    int indexInput = -1;
                    for (indexInput = 0; indexInput < optionsList.Length; indexInput++) {
                        if (string.Compare(optionsList[indexInput], newChoice, StringComparison.CurrentCultureIgnoreCase) == 0) {
                            validInput = true;
                            break;
                        }
                    }

                    if (validInput) {
                        selectedOption = optionsList[indexInput];
                        string message = string.Format(CultureInfo.CurrentCulture, "{0}", this.selectedOption);
                        string title = "Container";

                        // Show a message box to prove we were here
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            message,
                            title,
                            OLEMSGICON.OLEMSGICON_INFO,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    } else {
                        throw (new ArgumentException("invalid input"));
                    }
                }
            } else {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException("eventArgs is null"));
            }
        }
    }
}
