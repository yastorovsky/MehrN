using System.Windows;
using ReactiveUI;
using ServiceLib.ViewModels;
using v2rayN.Common;

namespace v2rayN.Views;

public partial class AddPsiphonServerWindow
{
    public AddPsiphonServerWindow()
    {
        InitializeComponent();

        Loaded += Window_Loaded;
        btnCancel.Click += (s, e) => Close();

        this.WhenActivated(disposables =>
        {
            if (ViewModel != null)
            {
                cmbEgressRegion.ItemsSource = ViewModel.EgressRegions;
            }

            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedEgressRegion, v => v.cmbEgressRegion.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.TunnelPoolSize, v => v.txtPoolSize.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CdnFrontingEnabled, v => v.togCdnFronting.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CdnFrontingEdges, v => v.txtCdnEdges.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PreSocksPort, v => v.txtPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DisplayLog, v => v.togDisplayLog.IsChecked).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveServerCmd, v => v.btnSave).DisposeWith(disposables);
        });

        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtRemarks.Focus();
    }
}
