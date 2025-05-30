using ProcurementApp.Data.Repositories;
using ProcurementApp.ViewModels.Auth;
using System.Text.RegularExpressions;

namespace ProcurementApp.Views.Auth;

public partial class RegistrationPage : ContentPage
{
    private readonly UsersRepository _usersRepository;

    public RegistrationPage(UsersRepository usersRepository)
    {
        InitializeComponent();
        _usersRepository = usersRepository;
        BindingContext = new RegistrationViewModel();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var viewModel = (RegistrationViewModel)BindingContext;

        if (string.IsNullOrWhiteSpace(viewModel.FirstName))
        {
            await DisplayAlert("Ошибка", "Введите имя", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.Email) || !IsValidEmail(viewModel.Email))
        {
            await DisplayAlert("Ошибка", "Введите корректный email", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.Password) || viewModel.Password.Length < 6)
        {
            await DisplayAlert("Ошибка", "Пароль должен содержать минимум 6 символов", "OK");
            return;
        }

        if (viewModel.Password != viewModel.ConfirmPassword)
        {
            await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
            return;
        }

        if (userTypePicker.SelectedIndex == -1)
        {
            await DisplayAlert("Ошибка", "Выберите тип пользователя", "OK");
            return;
        }

        try
        {
            var userType = userTypePicker.SelectedIndex == 0 ? "buyer" : "seller";
            Console.WriteLine($"Регистрируем пользователя с типом: {userType}"); // Логирование

            var registrationResult = await _usersRepository.RegisterUserAsync(
                viewModel.Email,
                viewModel.Password,
                viewModel.FirstName,
                viewModel.LastName,
                userType);

            if (registrationResult.Success)
            {
                await DisplayAlert("Успех", "Регистрация прошла успешно", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                await DisplayAlert("Ошибка", registrationResult.ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private void OnLoginTapped(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//LoginPage");
    }
}
