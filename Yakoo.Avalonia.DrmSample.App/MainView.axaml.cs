using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Yakoo.Avalonia.DrmSample.App.ViewModels;

namespace Yakoo.Avalonia.DrmSample.App;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel();
    }
}