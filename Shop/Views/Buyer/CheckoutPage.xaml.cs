using ProcurementApp.ViewModels.Buyer;
using ProcurementApp.Data.Repositories; // Добавлено для _cartRepository и _productsRepository
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Для SecureStorage
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace ProcurementApp.Views.Buyer;

public partial class CheckoutPage : ContentPage
{
    private readonly CheckoutViewModel _viewModel;
    private readonly CartRepository _cartRepository;
    private readonly ProductsRepository _productsRepository;

    public CheckoutPage(CheckoutViewModel viewModel, CartRepository cartRepository, ProductsRepository productsRepository)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _cartRepository = cartRepository;
        _productsRepository = productsRepository;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Получаем ID пользователя
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (!int.TryParse(userIdString, out int uid))
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }
            _viewModel.UserId = uid;

            // Используем публичный метод для обновления суммы
            await _viewModel.UpdateTotalAmount();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "OK");
        }
    }

    private Dictionary<string, string> ParseQueryParameters(string query)
    {
        return query.Split('&')
            .Select(p => p.Split('='))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => Uri.UnescapeDataString(pair[1]),
                StringComparer.OrdinalIgnoreCase);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // === Метод вызывается при нажатии кнопки "Оформить заказ" ===
    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        try
        {
            // Получаем ID пользователя
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (!int.TryParse(userIdString, out int userId))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Пользователь не авторизован.", "OK");
                return;
            }

            // Получаем товары из корзины
            var cartItems = await _cartRepository.GetCartItemsAsync(userId);
            if (cartItems == null || !cartItems.Any())
            {
                await Shell.Current.DisplayAlert("Ошибка", "Корзина пуста.", "OK");
                return;
            }

            // Проверяем наличие каждого товара на складе
            foreach (var item in cartItems)
            {
                var product = await _productsRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    await Shell.Current.DisplayAlert("Ошибка",
                        $"Недостаточно товара '{product?.Name ?? "неизвестный товар"}'", "OK");
                    return;
                }
            }

            // Здесь можно вызвать метод создания заказа
            await Shell.Current.DisplayAlert("Успех", "Заказ оформлен успешно!", "OK");

            // Очищаем корзину после оформления
            foreach (var item in cartItems)
            {
                await _cartRepository.RemoveFromCartAsync(item.CartItemId);
            }

            // Переход на страницу подтверждения или главную
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось оформить заказ: {ex.Message}", "OK");
        }
    }
}
