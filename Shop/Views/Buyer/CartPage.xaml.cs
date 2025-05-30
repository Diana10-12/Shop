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
            // Получаем ID пользователя из SecureStorage
            var userIdString = await SecureStorage.Default.GetAsync("user_id");
            if (int.TryParse(userIdString, out int userId))
            {
                _viewModel.UserId = userId;
                await _viewModel.LoadCartItemsAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Пользователь не авторизован", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить корзину: {ex.Message}", "OK");
        }

        // Подписываемся на сообщение об очистке корзины
        MessagingCenter.Subscribe<CheckoutViewModel>(this, "CartCleared", (sender) =>
        {
            _viewModel.ClearCart();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Отписываемся от сообщения при уходе со страницы
        MessagingCenter.Unsubscribe<CheckoutViewModel>(this, "CartCleared");
    }

    private async void OnCheckoutClicked(object sender, EventArgs e)
    {
        if (_viewModel.CartItems.Count == 0)
        {
            await DisplayAlert("Ошибка", "Корзина пуста", "OK");
            return;
        }

        // Используем актуальную сумму из ViewModel
        var parameters = new Dictionary<string, object>
        {
            { "TotalAmount", _viewModel.TotalPrice },
            { "UserId", _viewModel.UserId }
        };

        await Shell.Current.GoToAsync("CheckoutPage", parameters);
    }
}
