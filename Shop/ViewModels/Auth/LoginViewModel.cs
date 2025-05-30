using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProcurementApp.ViewModels.Auth;

public class LoginViewModel : INotifyPropertyChanged
{
    private string _email;
    private string _password;

    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
