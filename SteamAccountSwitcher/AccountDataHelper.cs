#region

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SteamAccountSwitcher.Properties;

#endregion

namespace SteamAccountSwitcher
{
    public static class AccountDataHelper
    {
        public static string DefaultData()
        {
            return SettingsHelper.SerializeAccounts(new ObservableCollection<Account>
            {
                new Account
                {
                    DisplayName = "Example",
                    Username = "username",
                    Password = "password"
                }
            });
        }

        public static void ReloadData()
        {
            try
            {
                App.Accounts =
                    new ObservableCollection<Account>(SettingsHelper.DeserializeAccounts(Settings.Default.Accounts));
            }
            catch
            {
                App.Accounts = new ObservableCollection<Account>();
                Popup.Show(
                    "Existing account data is corrupt.\n\nAll accounts have been reset.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            App.Accounts.CollectionChanged += (sender, args) =>
            {
                App.SaveTimer.DelaySave();
                TrayIconHelper.RefreshTrayIconMenu();
            };
            TrayIconHelper.RefreshTrayIconMenu();
            App.SwitchWindow?.ReloadAccountListBinding();
        }

        public static void ImportAccounts()
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = Resources.ImportExportExtension,
                Filter = Resources.ImportExportDialogExtensionFilter,
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(dialog.FileName) || !File.Exists(dialog.FileName))
            {
                Popup.Show("File doesn't exist.", image: MessageBoxImage.Error);
                return;
            }
            var fileContent = File.ReadAllText(dialog.FileName);
            try
            {
                // Test imported data before overwriting existing accounts.
                var testAccounts =
                    new ObservableCollection<Account>(SettingsHelper.DeserializeAccounts(fileContent));
            }
            catch
            {
                Popup.Show(
                    "Import failed. Data may be corrupt.\n\nNo changes have been made.",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Popup.Show(
                "Are you sure you want to overwrite all current accounts?\n\nThis cannot be reversed.",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) != MessageBoxResult.Yes)
            {
                return;
            }
            Settings.Default.Accounts = fileContent;
            ReloadData();
            SettingsHelper.SaveSettings();
        }

        public static void ExportAccounts()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = Resources.ImportExportExtension,
                Filter = Resources.ImportExportDialogExtensionFilter
            };
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, SettingsHelper.SerializeAccounts(App.Accounts));
            }
        }

        public static void Reload(this IEnumerable<Account> accounts)
        {
            App.Accounts = new ObservableCollection<Account>(accounts);
            SettingsHelper.SaveSettings();
            ReloadData();
        }
    }
}