using Avalonia.ReactiveUI;
using ReactiveUI;
using ServiceLib.ViewModels;
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
                cmbRegion.ItemsSource = ViewModel.Regions;
                cmbTunnelPool.ItemsSource = ViewModel.TunnelPoolSizes;
            }

            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRegion, v => v.cmbRegion.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedTunnelPoolSize, v => v.cmbTunnelPool.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PreSocksPort, v => v.txtPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DisplayLog, v => v.togDisplayLog.IsChecked).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveServerCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }
}
