namespace ServiceLib.ViewModels;

public partial class PsiphonSettingViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    private readonly PsiphonItem _settings;

    [Reactive] public partial bool CdnFrontingEnabled { get; set; }
    [Reactive] public partial string CdnFrontingEdges { get; set; }
    [Reactive] public partial string DefaultEgressRegion { get; set; }
    [Reactive] public partial int DefaultTunnelPoolSize { get; set; }

    public List<string> EgressRegions { get; } = Global.PsiphonEgressRegions;

    public ReactiveCommand<RxVoid, RxVoid> SaveCmd { get; }

    public PsiphonSettingViewModel()
    {
        _config = AppManager.Instance.Config;
        _settings = JsonUtils.DeepCopy(_config.PsiphonItem ?? new());
        CdnFrontingEnabled = _settings.CdnFrontingEnabled;
        CdnFrontingEdges = _settings.CdnFrontingEdges ?? string.Empty;
        DefaultEgressRegion = _settings.DefaultEgressRegion ?? string.Empty;
        DefaultTunnelPoolSize = _settings.DefaultTunnelPoolSize < 1 ? 1 : _settings.DefaultTunnelPoolSize;

        SaveCmd = ReactiveCommand.CreateFromTask(SaveAsync);
    }

    private async Task SaveAsync()
    {
        _settings.CdnFrontingEnabled = CdnFrontingEnabled;
        _settings.CdnFrontingEdges = CdnFrontingEdges?.Trim() ?? string.Empty;
        _settings.DefaultEgressRegion = DefaultEgressRegion ?? string.Empty;
        _settings.DefaultTunnelPoolSize = DefaultTunnelPoolSize < 1 ? 1 : DefaultTunnelPoolSize;

        _config.PsiphonItem = _settings;

        if (await ConfigHandler.SaveConfig(_config) == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }
}
