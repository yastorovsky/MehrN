using v2rayN.Desktop.Base;

namespace v2rayN.Desktop.Views;

public partial class PsiphonSettingWindow : WindowBase<PsiphonSettingViewModel>
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
