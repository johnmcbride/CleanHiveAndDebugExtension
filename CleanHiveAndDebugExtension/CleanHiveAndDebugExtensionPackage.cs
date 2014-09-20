using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Company.CleanHiveAndDebugExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 500)]
    [Guid(GuidList.guidCleanHiveAndDebugExtensionPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class CleanHiveAndDebugExtensionPackage : Package
    {
        ErrorListProvider _errProvider = null;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public CleanHiveAndDebugExtensionPackage()
        {
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            //create the menu service
            OleMenuCommandService _menuService = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            //get the menu item command based on the GUID/ID
            CommandID _cleanHiveCmd = new CommandID(Guid.Parse("{1467AD39-B4C7-47EC-8075-09AB259EB847}"), int.Parse("7A121",System.Globalization.NumberStyles.HexNumber));

            MenuCommand _clientHiveMenuItem = new MenuCommand(ExecuteCleanHiveAndDebug, _cleanHiveCmd);
            _menuService.AddCommand(_clientHiveMenuItem);

            CommandID _enableVSIPLoggingCmd = new CommandID(Guid.Parse("{1467AD39-B4C7-47EC-8075-09AB259EB847}"), int.Parse("7A122", System.Globalization.NumberStyles.HexNumber));
            MenuCommand _enableVSIPMenuItem = new MenuCommand(ExecuteEnableVSIPLogging,_enableVSIPLoggingCmd);
            _menuService.AddCommand(_enableVSIPMenuItem);

            CommandID _disbleVSIPLoggingCmd = new CommandID(Guid.Parse("{1467AD39-B4C7-47EC-8075-09AB259EB847}"), int.Parse("7A123", System.Globalization.NumberStyles.HexNumber));
            MenuCommand _disableVSIPMenuItem = new MenuCommand(ExecuteDisableVSIPLogging, _disbleVSIPLoggingCmd);
            _menuService.AddCommand(_disableVSIPMenuItem);

            //create the errorlist provider
            _errProvider = new ErrorListProvider(this);

           
        }

        private void ExecuteCleanHiveAndDebug(object sender, EventArgs e)
        {
            //clean the registry
            //vs2013
            try
            {
                Microsoft.Win32.RegistryKey _key = Microsoft.Win32.Registry.CurrentUser;
                Microsoft.Win32.RegistryKey _vsKey = _key.OpenSubKey(@"Software\Microsoft\VisualStudio", true);
                foreach (string _keyname in _vsKey.GetSubKeyNames())
                {
                    if (_keyname.ToLower().IndexOf("12.0") != -1)
                    {
                        //check for the exp
                        if (_keyname.ToLower().IndexOf("exp") != -1)
                        {
                            //delete the subkey
                            _vsKey.DeleteSubKeyTree(_keyname);
                            //Console.WriteLine("Found exp key to delete " + _keyname);
                        }
                    }
                }
            }
            catch ( System.Exception _registryException)
            {
                ErrorTask _registryCleanErrorTask = new ErrorTask();
                _registryCleanErrorTask.Category = TaskCategory.BuildCompile;
                _registryCleanErrorTask.ErrorCategory = TaskErrorCategory.Warning;
                _registryCleanErrorTask.Text = "Unable to clean/delete the registry hive for the experimental instance";
                _errProvider.Tasks.Add(_registryCleanErrorTask);
            }
            //delete the exp user directory
            string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if ( _appDataPath.EndsWith(@"\"))
            {
                _appDataPath += @"Microsoft\VisualStudio\";
            }
            else
            {
                _appDataPath += @"\Microsoft\VisualStudio\";
            }

            try
            {
                foreach (string directory in System.IO.Directory.GetDirectories(_appDataPath))
                {
                    if (directory.ToLower().IndexOf("12.0") != -1)
                    {
                        if (directory.ToLower().IndexOf("exp") != -1)
                        {
                            //delete the directory
                            CleanDirectoryAndFiles(directory);
                        }
                    }
                }
            }
            catch ( System.Exception _userDirectoryException)
            {
                ErrorTask _userDirCleanErrorTask = new ErrorTask();
                _userDirCleanErrorTask.Category = TaskCategory.BuildCompile;
                _userDirCleanErrorTask.ErrorCategory = TaskErrorCategory.Warning;
                _userDirCleanErrorTask.Text = "Unable to clean/delete the user directory for the experimental instance";
                _errProvider.Tasks.Add(_userDirCleanErrorTask);
            }
            EnvDTE.DTE _dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            _dte.ExecuteCommand("Debug.Start");
        }

        private void ExecuteEnableVSIPLogging(object sender, EventArgs e)
        {
            try
            {
                Microsoft.Win32.RegistryKey _key = Microsoft.Win32.Registry.CurrentUser;
                Microsoft.Win32.RegistryKey _vsKey = _key.OpenSubKey(@"Software\Microsoft\VisualStudio\12.0\General", true);

                _vsKey.SetValue("EnableVSIPLogging", 1, RegistryValueKind.DWord);
                _vsKey.Close();

                //restart VS instance.
                if ( System.Windows.Forms.MessageBox.Show("Would you like to restart Visual Studio to enable VSIP logging?","Enable VSIP Logging",System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes )
                {
                    //prompt the user to restart.
                    //we need to add the same restart method that the extension manager employs when it has an update and
                    //needs to restart
                    System.Windows.Forms.MessageBox.Show("Please start Visual Studio to apply the VSIP changes.");
                }
            }
            catch ( Exception registryError)
            {
                ErrorTask _registryEnableVSIPErrorTask = new ErrorTask();
                _registryEnableVSIPErrorTask.Category = TaskCategory.BuildCompile;
                _registryEnableVSIPErrorTask.ErrorCategory = TaskErrorCategory.Warning;
                _registryEnableVSIPErrorTask.Text = "Unable to enable VSIP logging by setting the EnableVSIPLogging registry entry";
                _errProvider.Tasks.Add(_registryEnableVSIPErrorTask);

            }
        }
        private void ExecuteDisableVSIPLogging(object sender, EventArgs e)
        {
            try
            {
                Microsoft.Win32.RegistryKey _key = Microsoft.Win32.Registry.CurrentUser;
                Microsoft.Win32.RegistryKey _vsKey = _key.OpenSubKey(@"Software\Microsoft\VisualStudio\12.0\General", true);

                _vsKey.SetValue("EnableVSIPLogging", 0, RegistryValueKind.DWord);
                _vsKey.Close();

                //restart VS instance.
                if (System.Windows.Forms.MessageBox.Show("Would you like to restart Visual Studio to disable VSIP logging?", "Enable VSIP Logging", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    //we need to add the same restart method that the extension manager employs when it has an update and
                    //needs to restart
                    System.Windows.Forms.MessageBox.Show("Please start Visual Studio to apply the VSIP changes.");
                }
            }
            catch ( Exception regError )
            {
                ErrorTask _registryEnableVSIPErrorTask = new ErrorTask();
                _registryEnableVSIPErrorTask.Category = TaskCategory.BuildCompile;
                _registryEnableVSIPErrorTask.ErrorCategory = TaskErrorCategory.Warning;
                _registryEnableVSIPErrorTask.Text = "Unable to enable VSIP logging by setting the EnableVSIPLogging registry entry";
                _errProvider.Tasks.Add(_registryEnableVSIPErrorTask);
            }
        }

        internal void CleanDirectoryAndFiles(string Path)
        {
            System.IO.DirectoryInfo _directory = new System.IO.DirectoryInfo(Path);
            foreach ( System.IO.DirectoryInfo _childDirectory in _directory.GetDirectories())
            {
                CleanDirectoryAndFiles(_childDirectory.FullName);
            }
            System.IO.FileInfo[] _files = _directory.GetFiles();
            foreach (System.IO.FileInfo _file in _files)
            {
                _file.Delete();
            }
            _directory.Delete();
        }
        #endregion

    }
}
