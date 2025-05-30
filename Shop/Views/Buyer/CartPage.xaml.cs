using ProcurementApp.ViewModels.Buyer;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace ProcurementApp.Views.Buyer;

public partial class CartPage : ContentPage
{
    private readonly CartViewModel _viewModel;

    public CartPage(CartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // �������� ID ������������ �� SecureStorage
            var userIdString = await SecureStorage.Default.GetAsync("user_id");
            if (int.TryParse(userIdString, out int userId))
            {
                _viewModel.UserId = userId;
                await _viewModel.LoadCartItemsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("������", "������������ �� �����������", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("������", $"�� ������� ��������� �������: {ex.Message}", "OK");
        }

        // ������������� �� ��������� �� ������� �������
        MessagingCenter.Subscribe<CheckoutViewModel>(this, "CartCleared", (sender) =>
        {
            _viewModel.ClearCart();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // ������������ �� ��������� ��� ����� �� ��������
        MessagingCenter.Unsubscribe<CheckoutViewModel>(this, "CartCleared");
    }

    private async void OnCheckoutClicked(object sender, EventArgs e)
    {
        if (_viewModel.CartItems.Count == 0)
        {
            await DisplayAlert("������", "������� �����", "OK");
            return;
        }

        // ���������� ���������� ����� �� ViewModel
        var parameters = new Dictionary<string, object>
        {
            { "TotalAmount", _viewModel.TotalPrice },
            { "UserId", _viewModel.UserId }
        };

        await Shell.Current.GoToAsync("CheckoutPage", parameters);
    }
}
