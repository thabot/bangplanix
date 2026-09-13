using System.Collections.Concurrent;
using Bangplanix.Core.Plugins;

namespace Bangplanix.Engine.Plugins;

public class PluginDirectoryWatcher : IDisposable
{
    private readonly DynamicPluginManager _pluginManager;
    private readonly string _pluginsDirectory;
    private readonly byte[]? _verificationKey;
    private readonly FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _lastLoadedTimestamps = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _discoveredPluginIds = new();
    private bool _isDisposed;

    public string PluginsDirectory => _pluginsDirectory;
    public IReadOnlyList<string> DiscoveredPluginIds
    {
        get
        {
            lock (_discoveredPluginIds)
            {
                return _discoveredPluginIds.ToList();
            }
        }
    }

    public event Action<string, PluginDescriptor>? OnPluginLoaded;
    public event Action<string, string>? OnPluginLoadError;

    public PluginDirectoryWatcher(DynamicPluginManager pluginManager, string pluginsDirectory, byte[]? verificationKey = null, bool startImmediately = true)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _pluginsDirectory = Path.GetFullPath(pluginsDirectory);
        _verificationKey = verificationKey;

        if (!Directory.Exists(_pluginsDirectory))
        {
            Directory.CreateDirectory(_pluginsDirectory);
        }

        _watcher = new FileSystemWatcher(_pluginsDirectory, "*.dll")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = false
        };

        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;

        if (startImmediately)
        {
            Start();
            ScanDirectory();
        }
    }

    public void Start()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
        }
    }

    public int ScanDirectory()
    {
        int loadedCount = 0;
        if (!Directory.Exists(_pluginsDirectory)) return 0;

        var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll");
        foreach (var dllPath in dllFiles)
        {
            if (TryLoadPluginFile(dllPath))
            {
                loadedCount++;
            }
        }

        return loadedCount;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        // Simple debounce: avoid duplicate loads within 500ms
        var now = DateTime.UtcNow;
        if (_lastLoadedTimestamps.TryGetValue(e.FullPath, out var lastTime) && (now - lastTime).TotalMilliseconds < 500)
        {
            return;
        }

        _lastLoadedTimestamps[e.FullPath] = now;
        Task.Delay(200).ContinueWith(_ => TryLoadPluginFile(e.FullPath));
    }

    public bool TryLoadPluginFile(string dllPath)
    {
        try
        {
            if (!File.Exists(dllPath)) return false;

            if (_verificationKey != null)
            {
                var sigPath = Path.ChangeExtension(dllPath, ".sig.json");
                if (!File.Exists(sigPath))
                {
                    OnPluginLoadError?.Invoke(dllPath, "Missing signature file (.sig.json) for signed plugin verification.");
                    return false;
                }

                var dllBytes = File.ReadAllBytes(dllPath);
                var sigJson = File.ReadAllText(sigPath);
                var sigInfo = System.Text.Json.JsonSerializer.Deserialize<PluginSignatureInfo>(sigJson);
                if (sigInfo == null || !PluginSignatureValidator.VerifySignature(dllBytes, sigInfo, _verificationKey))
                {
                    OnPluginLoadError?.Invoke(dllPath, "Cryptographic signature validation failed. DLL may be tampered or untrusted.");
                    return false;
                }
            }

            var container = _pluginManager.LoadPluginFromFile(dllPath, validateSecurity: true);
            lock (_discoveredPluginIds)
            {
                if (!_discoveredPluginIds.Contains(container.Descriptor.Id))
                {
                    _discoveredPluginIds.Add(container.Descriptor.Id);
                }
            }

            OnPluginLoaded?.Invoke(dllPath, container.Descriptor);
            return true;
        }
        catch (Exception ex)
        {
            OnPluginLoadError?.Invoke(dllPath, ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _watcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
