using v2rayN.Desktop.Base;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class AddPsiphonServerWindow : WindowBase<AddPsiphonServerViewModel>
{
    public AddPsiphonServerWindow()
    {
        InitializeComponent();

        btnCancel.Click += (s, e) => Close();

        this.WhenActivated(disposables =>
        {
            if (ViewModel != null)
            {
                cmbEgressRegion.ItemsSource = ViewModel.EgressRegions;
            }

            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedEgressRegion, v => v.cmbEgressRegion.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.TunnelPoolSize, v => v.txtPoolSize.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CdnFrontingEnabled, v => v.togCdnFronting.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CdnFrontingEdges, v => v.txtCdnEdges.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PreSocksPort, v => v.txtPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DisplayLog, v => v.togDisplayLog.IsChecked).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveServerCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }
}
