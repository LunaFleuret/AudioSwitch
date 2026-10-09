using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;

namespace AudioSwitch
{
    public partial class App : Application
    {
        public const string AppUserModelId = "Luna.AudioSwitch";

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set explicit AppUserModelID so Windows associates JumpList with pinned shortcut
            try
            {
                SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
            }
            catch { }

            // Ensure JumpList is registered
            UpdateJumpList();

            // Handle arguments
            string[] args = e.Args;

            if (args.Length >= 2 && args[0].Equals("--set", StringComparison.OrdinalIgnoreCase))
            {
                string targetKey = args[1].ToLowerInvariant();
                var target = AudioManager.TargetDevices.FirstOrDefault(t => t.Key.Equals(targetKey, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    var result = AudioManager.SetDefaultDeviceByTarget(target);
                    ShowNotificationAndExit(result.Message, result.Success);
                    return;
                }
                else
                {
                    ShowNotificationAndExit($"不明なデバイス指定: {targetKey}", false);
                    return;
                }
            }
            else if (args.Length >= 1 && (args[0].Equals("--shortcut", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--create-shortcut", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    string exePath = Environment.ProcessPath ?? "";
                    string dir = Directory.GetCurrentDirectory();
                    string shortcutPath = Path.Combine(dir, "AudioSwitch.lnk");
                    ShortcutHelper.CreateShortcut(exePath, shortcutPath, AppUserModelId, "Audio Output Switcher");
                    ShowNotificationAndExit($"ショートカットを作成しました:\n{Path.GetFileName(shortcutPath)}", true);
                }
                catch (Exception ex)
                {
                    ShowNotificationAndExit($"ショートカット作成失敗: {ex.Message}", false);
                }
                return;
            }
            else if (args.Length >= 1 && args[0].Equals("--register", StringComparison.OrdinalIgnoreCase))
            {
                ShowNotificationAndExit("ジャンプリストを更新しました。", true);
                return;
            }
            else
            {
                // Default: Toggle to next available target device
                var result = AudioManager.ToggleNextDevice();
                ShowNotificationAndExit(result.Message, result.Success);
                return;
            }
        }

        private void UpdateJumpList()
        {
            try
            {
                string exePath = Environment.ProcessPath ?? "";
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return;
                }

                var jumpList = new JumpList();
                JumpList.SetJumpList(this, jumpList);

                // Add tasks for each target device
                foreach (var target in AudioManager.TargetDevices)
                {
                    var task = new JumpTask
                    {
                        Title = target.DisplayName,
                        Description = $"{target.DisplayName} にオーディオ出力を切り替え",
                        ApplicationPath = exePath,
                        Arguments = $"--set {target.Key}",
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    };
                    if (!string.IsNullOrEmpty(target.IconResourcePath))
                    {
                        task.IconResourcePath = target.IconResourcePath;
                        task.IconResourceIndex = target.IconResourceIndex;
                    }
                    jumpList.JumpItems.Add(task);
                }

                jumpList.Apply();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update jump list: {ex.Message}");
            }
        }

        private void ShowNotificationAndExit(string message, bool isSuccess)
        {
            var win = new NotificationWindow(message, isSuccess);
            win.Closed += (s, e) =>
            {
                Shutdown();
            };
            win.Show();
        }
    }
}
