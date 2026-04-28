using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MVC.ViewModels;

namespace MVC.Views;

public partial class ErrorWindowView : Window
{
    public ErrorWindowView()
    {
        InitializeComponent();
    }
    public ErrorWindowView(string errMsg, string errDesc)
    {
        InitializeComponent();
        DataContext = new ErrorWindowViewModel(this, errMsg, errDesc);
    }
    private void OnClickCommand(object sender, PointerPressedEventArgs e)
    {
        this.Close();
    }
}