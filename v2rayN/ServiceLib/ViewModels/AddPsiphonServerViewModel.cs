namespace ServiceLib.ViewModels;

public partial class AddPsiphonServerViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    [Reactive]
    public partial ProfileItem SelectedSource { get; set; }

    [Reactive]
    public partial string SelectedEgressRegion { get; set; }

    [Reactive]
    public partial bool CdnFrontingEnabled { get; set; }

    [Reactive]
    public partial string CdnFrontingEdges { get; set; }

    [Reactive]
    public partial string TunnelPoolSize { get; set; }

    [Reactive]
    public partial string PreSocksPort { get; set; }

    [Reactive]
    public partial bool DisplayLog { get; set; }

    public List<string> EgressRegions { get; } = Global.PsiphonEgressRegions;

    public ReactiveCommand<RxVoid, RxVoid> SaveServerCmd { get; }

    public AddPsiphonServerViewModel(ProfileItem profileItem)
    {
        _config = AppManager.Instance.Config;
        SelectedSource = profileItem.IndexId.IsNullOrEmpty() ? profileItem : JsonUtils.DeepCopy(profileItem);

        var extra = SelectedSource.GetProtocolExtra();
        SelectedEgressRegion = extra?.PsiphonEgressRegion ?? "";
        CdnFrontingEnabled = extra?.PsiphonCdnFronting ?? false;
        CdnFrontingEdges = extra?.PsiphonCdnFrontingEdges ?? "";
        TunnelPoolSize = (extra?.PsiphonTunnelPoolSize ?? 1).ToString();
        PreSocksPort = (SelectedSource.PreSocksPort is > 0 and <= 65535 ? SelectedSource.PreSocksPort.Value : 1080).ToString();
        DisplayLog = SelectedSource.DisplayLog;

        if (SelectedSource.Remarks.IsNullOrEmpty())
        {
            SelectedSource.Remarks = "Psiphon Shirokhorshid";
        }

        SaveServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveServerAsync();
        });
    }

    private async Task SaveServerAsync()
    {
        if (SelectedSource.Remarks.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        if (!int.TryParse(PreSocksPort, out var port) || port <= 0 || port > 65535)
        {
            port = 1080;
        }
        if (!int.TryParse(TunnelPoolSize, out var poolSize) || poolSize < 1)
        {
            poolSize = 1;
        }

        SelectedSource.ConfigType = EConfigType.Custom;
        SelectedSource.CoreType = ECoreType.psiphon;
        SelectedSource.PreSocksPort = port;
        SelectedSource.Port = port;
        SelectedSource.DisplayLog = DisplayLog;
        SelectedSource.Address = Global.Loopback;

        var extra = (SelectedSource.GetProtocolExtra() ?? new ProtocolExtraItem()) with
        {
            PsiphonEgressRegion = SelectedEgressRegion,
            PsiphonCdnFronting = CdnFrontingEnabled,
            PsiphonCdnFrontingEdges = CdnFrontingEnabled ? CdnFrontingEdges.TrimEx() : "",
            PsiphonTunnelPoolSize = poolSize,
        };
        SelectedSource.SetProtocolExtra(extra);

        var result = await ConfigHandler.AddPsiphonServer(_config, SelectedSource);

        if (result == 0)
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
