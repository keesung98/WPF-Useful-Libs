using System;
using System.Collections.Generic;
using System.IO;

namespace KeesInIManager
{
    public class InIManager
    {
        private readonly string _path;
        private readonly bool _autoSave;
        private readonly Dictionary<string, Dictionary<string, string>> _data;
        private readonly FileSystemWatcher _watcher;

        public InIManager(string path, bool autoSave = true)
        {
            _path = Path.GetFullPath(path); // 항상 절대 경로로 변환
            _autoSave = autoSave;
            _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(_path))
                Load();
            else
                Save(); // 없으면 새로 생성

            string dir = Path.GetDirectoryName(_path)!;
            string file = Path.GetFileName(_path);

            _watcher = new FileSystemWatcher(dir, file)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watcher.Changed += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }


        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                _watcher.EnableRaisingEvents = false; // 루프 방지
                Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IniManager] 파일 변경 감지 중 오류: {ex.Message}");
            }
            finally
            {
                _watcher.EnableRaisingEvents = true;
            }
        }

        public void Load()
        {
            _data.Clear();

            string currentSection = "";
            foreach (var line in File.ReadAllLines(_path))
            {
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    if (!_data.ContainsKey(currentSection))
                        _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var parts = trimmed.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();

                        if (!_data.ContainsKey(currentSection))
                            _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        _data[currentSection][key] = value;
                    }
                }
            }
        }

        public void Save()
        {
            using (var writer = new StreamWriter(_path))
            {
                foreach (var section in _data)
                {
                    if (!string.IsNullOrEmpty(section.Key))
                        writer.WriteLine($"[{section.Key}]");

                    foreach (var kv in section.Value)
                        writer.WriteLine($"{kv.Key}={kv.Value}");
                    writer.WriteLine();
                }
            }
        }

        public string GetValue(string section, string key, string defaultValue = "")
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
                return _data[section][key];
            return defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var value = GetValue(section, key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            var value = GetValue(section, key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!_data.ContainsKey(section))
                _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_data[section].ContainsKey(key) && _data[section][key] == value)
                return; // 값이 같으면 저장 안 함

            _data[section][key] = value;

            if (_autoSave) Save();
        }
    }
}
