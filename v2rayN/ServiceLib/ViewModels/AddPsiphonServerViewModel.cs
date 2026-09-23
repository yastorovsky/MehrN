namespace ServiceLib.ViewModels;

public partial class AddPsiphonServerViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    [Reactive]
    public partial ProfileItem SelectedSource { get; set; }

    [Reactive]
    public partial PsiphonRegionItem? SelectedRegion { get; set; }

    [Reactive]
    public partial int SelectedTunnelPoolSize { get; set; }

    [Reactive]
    public partial string PreSocksPort { get; set; }

    [Reactive]
    public partial bool DisplayLog { get; set; }

    public List<PsiphonRegionItem> Regions { get; }

    public List<int> TunnelPoolSizes { get; } = [1, 2, 3, 4, 5, 6, 8, 10];

    public ReactiveCommand<RxVoid, RxVoid> SaveServerCmd { get; }

    public AddPsiphonServerViewModel(ProfileItem profileItem)
    {
        _config = AppManager.Instance.Config;
        SelectedSource = profileItem.IndexId.IsNullOrEmpty() ? profileItem : JsonUtils.DeepCopy(profileItem);

        Regions = InitRegions();

        var extra = SelectedSource.GetProtocolExtra();
        var regionCode = extra?.PsiphonEgressRegion ?? string.Empty;
        SelectedRegion = Regions.FirstOrDefault(r => r.Code.Equals(regionCode, StringComparison.OrdinalIgnoreCase)) ?? Regions[0];

        SelectedTunnelPoolSize = extra?.PsiphonTunnelPoolSize is > 0 and <= 10 ? extra.PsiphonTunnelPoolSize.Value : 2;
        PreSocksPort = (SelectedSource.PreSocksPort is > 0 and <= 65535 ? SelectedSource.PreSocksPort.Value : 20808).ToString();
        DisplayLog = SelectedSource.DisplayLog;

        if (SelectedSource.Remarks.IsNullOrEmpty())
        {
            SelectedSource.Remarks = string.IsNullOrEmpty(SelectedRegion.Code) ? "Psiphon" : $"Psiphon [{SelectedRegion.Code}]";
        }

        SaveServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveServerAsync();
        });
    }

    private static List<PsiphonRegionItem> InitRegions()
    {
        return
        [
            new PsiphonRegionItem("", "Auto (Best / Fastest)"),
            new PsiphonRegionItem("US", "United States (US)"),
            new PsiphonRegionItem("DE", "Germany (DE)"),
            new PsiphonRegionItem("GB", "United Kingdom (GB)"),
            new PsiphonRegionItem("NL", "Netherlands (NL)"),
            new PsiphonRegionItem("CA", "Canada (CA)"),
            new PsiphonRegionItem("FR", "France (FR)"),
            new PsiphonRegionItem("CH", "Switzerland (CH)"),
            new PsiphonRegionItem("SG", "Singapore (SG)"),
            new PsiphonRegionItem("JP", "Japan (JP)"),
            new PsiphonRegionItem("AU", "Australia (AU)"),
            new PsiphonRegionItem("AT", "Austria (AT)"),
            new PsiphonRegionItem("BE", "Belgium (BE)"),
            new PsiphonRegionItem("BG", "Bulgaria (BG)"),
            new PsiphonRegionItem("CZ", "Czech Republic (CZ)"),
            new PsiphonRegionItem("DK", "Denmark (DK)"),
            new PsiphonRegionItem("ES", "Spain (ES)"),
            new PsiphonRegionItem("FI", "Finland (FI)"),
            new PsiphonRegionItem("HU", "Hungary (HU)"),
            new PsiphonRegionItem("IN", "India (IN)"),
            new PsiphonRegionItem("IE", "Ireland (IE)"),
            new PsiphonRegionItem("IT", "Italy (IT)"),
            new PsiphonRegionItem("NO", "Norway (NO)"),
            new PsiphonRegionItem("PL", "Poland (PL)"),
            new PsiphonRegionItem("RO", "Romania (RO)"),
            new PsiphonRegionItem("SE", "Sweden (SE)"),
            new PsiphonRegionItem("SK", "Slovakia (SK)")
        ];
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
            port = 20808;
        }

        SelectedSource.ConfigType = EConfigType.Custom;
        SelectedSource.CoreType = ECoreType.psiphon;
        SelectedSource.PreSocksPort = port;
        SelectedSource.DisplayLog = DisplayLog;
        SelectedSource.Port = port;
        SelectedSource.Address = Global.Loopback;

        var extra = (SelectedSource.GetProtocolExtra() ?? new ProtocolExtraItem()) with
        {
            PsiphonEgressRegion = SelectedRegion?.Code ?? string.Empty,
            PsiphonTunnelPoolSize = SelectedTunnelPoolSize
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

public class PsiphonRegionItem
{
    public string Code { get; set; }
    public string Name { get; set; }

    public PsiphonRegionItem(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public override string ToString() => Name;
}
