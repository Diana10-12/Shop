using ProcurementApp.Data.Repositories;
using ProcurementApp.ViewModels.Auth;
using ProcurementApp.ViewModels.Buyer;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace ProcurementApp.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly UsersRepository _usersRepository;

    public LoginPage(UsersRepository usersRepository)
    {
        InitializeComponent();
        _usersRepository = usersRepository;
        BindingContext = new LoginViewModel();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var viewModel = (LoginViewModel)BindingContext;

        if (string.IsNullOrWhiteSpace(viewModel.Email))
        {
            await DisplayAlert("Ошибка", "Введите email", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.Password))
        {
            await DisplayAlert("Ошибка", "Введите пароль", "OK");
            return;
        }

        try
        {
            var loginResult = await _usersRepository.LoginUserAsync(viewModel.Email, viewModel.Password);

            if (loginResult.Success)
            {
                // Сохраняем данные аутентификации
                await SecureStorage.Default.SetAsync("auth_token", loginResult.Token);
                await SecureStorage.Default.SetAsync("user_id", loginResult.UserId.ToString());
                await SecureStorage.Default.SetAsync("user_type", loginResult.UserType);

                // Обновляем ViewModel для страницы товаров
                var productsVM = new ProductsViewModel(
                    App.Current.Handler.MauiContext.Services.GetService<ProductsRepository>(),
                    App.Current.Handler.MauiContext.Services.GetService<CartRepository>());

                // Навигация с обновленной ViewModel
                var route = loginResult.UserType == "buyer"
                    ? "//Buyer/ProductsPage"
                    : "//Seller/AddProductPage";

                await Shell.Current.GoToAsync(route);
            }
            else
            {
                await DisplayAlert("Ошибка", loginResult.ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
        }
    }

    // Остальные методы без изменений
    private void OnRegisterTapped(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//RegistrationPage");
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Информация", "Вход через Google будет реализован позже", "OK");
    }

    private async void OnFacebookLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Информация", "Вход через Facebook будет реализован позже", "OK");
    }

    protected async Task CheckSellerAccess()
    {
        var userType = await SecureStorage.Default.GetAsync("user_type");
        if (userType != "seller")
        {
            await DisplayAlert("Ошибка", "Доступ запрещен", "OK");
            await Shell.Current.GoToAsync("//Buyer/ProductsPage");
            return;
        }
    }
}
