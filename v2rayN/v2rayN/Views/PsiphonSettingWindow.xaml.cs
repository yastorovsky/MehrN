using ServiceLib.ViewModels;

namespace v2rayN.Views;

public partial class PsiphonSettingWindow
{
    public PsiphonSettingWindow()
    {
        InitializeComponent();

        btnCancel.Click += (s, e) => Close();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }
}
