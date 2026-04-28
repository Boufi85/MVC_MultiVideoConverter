using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVC.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVC.ViewModels
{
    public partial class ErrorWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _errorMessage = "Erreur";

        [ObservableProperty]
        private string _errorDescription = "Description de l'erreur";

        private ErrorWindowView ErrorWindowView;


        public ErrorWindowViewModel(ErrorWindowView parent, string errMsg, string errDesc)
        {
            ErrorWindowView = parent;
            ErrorMessage = errMsg;
            ErrorDescription = errDesc;
        }

        [RelayCommand]
        private void CloseWindow()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                this.ErrorWindowView.Close();
            }));
        }
    }

}