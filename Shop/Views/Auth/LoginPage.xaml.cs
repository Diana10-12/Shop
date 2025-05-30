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
            await DisplayAlert("������", "������� email", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewModel.Password))
        {
            await DisplayAlert("������", "������� ������", "OK");
            return;
        }

        try
        {
            var loginResult = await _usersRepository.LoginUserAsync(viewModel.Email, viewModel.Password);

            if (loginResult.Success)
            {
                // ��������� ������ ��������������
                await SecureStorage.Default.SetAsync("auth_token", loginResult.Token);
                await SecureStorage.Default.SetAsync("user_id", loginResult.UserId.ToString());
                await SecureStorage.Default.SetAsync("user_type", loginResult.UserType);

                // ��������� ViewModel ��� �������� �������
                var productsVM = new ProductsViewModel(
                    App.Current.Handler.MauiContext.Services.GetService<ProductsRepository>(),
                    App.Current.Handler.MauiContext.Services.GetService<CartRepository>());

                // ��������� � ����������� ViewModel
                var route = loginResult.UserType == "buyer"
                    ? "//Buyer/ProductsPage"
                    : "//Seller/AddProductPage";

                await Shell.Current.GoToAsync(route);
            }
            else
            {
                await DisplayAlert("������", loginResult.ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("������", $"��������� ������: {ex.Message}", "OK");
        }
    }

    // ��������� ������ ��� ���������
    private void OnRegisterTapped(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("//RegistrationPage");
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlert("����������", "���� ����� Google ����� ���������� �����", "OK");
    }

    private async void OnFacebookLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlert("����������", "���� ����� Facebook ����� ���������� �����", "OK");
    }

    protected async Task CheckSellerAccess()
    {
        var userType = await SecureStorage.Default.GetAsync("user_type");
        if (userType != "seller")
        {
            await DisplayAlert("������", "������ ��������", "OK");
            await Shell.Current.GoToAsync("//Buyer/ProductsPage");
            return;
        }
    }
}
