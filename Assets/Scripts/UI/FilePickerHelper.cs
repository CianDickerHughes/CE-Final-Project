using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper class for selecting image files.
/// In the editor, uses EditorUtility.OpenFilePanel.
/// In Windows builds, opens native Windows file dialog using PowerShell.
/// </summary>
public static class FilePickerHelper
{
    /// <summary>
    /// Opens a native file picker dialog.
    /// </summary>
    public static void PickImageFile(Action<string> onFileSelected)
    {
        #if UNITY_EDITOR
        // Editor: use EditorUtility file picker
        string path = EditorUtility.OpenFilePanel("Choose Image", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            onFileSelected?.Invoke(path);
        }
        #elif UNITY_STANDALONE_WIN
        // Windows build: open native file dialog using PowerShell
        try
        {
            string psScript = @"
[System.Reflection.Assembly]::LoadWithPartialName('System.windows.forms') | Out-Null

$OpenFileDialog = New-Object System.Windows.Forms.OpenFileDialog
$OpenFileDialog.initialDirectory = [Environment]::GetFolderPath('MyDocuments')
$OpenFileDialog.filter = 'Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*'
$OpenFileDialog.Title = 'Select an Image'
$result = $OpenFileDialog.ShowDialog()

if($result -eq [System.Windows.Forms.DialogResult]::OK) {
    Write-Host $OpenFileDialog.filename
}
";

            // Create temp PowerShell script file
            string tempDir = System.IO.Path.GetTempPath();
            string scriptPath = System.IO.Path.Combine(tempDir, "FilePickerScript_" + System.Guid.NewGuid() + ".ps1");
            System.IO.File.WriteAllText(scriptPath, psScript);

            // Run PowerShell script and capture output
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output) && System.IO.File.Exists(output))
                {
                    onFileSelected?.Invoke(output);
                }

                // Clean up temp file
                try { System.IO.File.Delete(scriptPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error opening file dialog: {ex.Message}");
        }
        #else
        UnityEngine.Debug.LogWarning("File picker not supported on this platform");
        #endif
    }
}
