using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AutoKeyPresser.Models;

namespace AutoKeyPresser.Services
{
    /// <summary>
    /// Service for saving and loading configuration
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath;

        public ConfigService()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoKeyPresser", "config.json");
        }

        public void Save(SavedConfig config)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_configPath, JsonSerializer.Serialize(config, options));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Save Error: " + ex.Message);
                throw;
            }
        }

        public SavedConfig? Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return null;
                }

                string json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<SavedConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Load Error: " + ex.Message);
                return null;
            }
        }
    }
}
