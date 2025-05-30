using ProcurementApp.ViewModels.Buyer;
using ProcurementApp.Data.Repositories; // ��������� ��� _cartRepository � _productsRepository
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // ��� SecureStorage
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
            // �������� ID ������������
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (!int.TryParse(userIdString, out int uid))
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }
            _viewModel.UserId = uid;

            // ���������� ��������� ����� ��� ���������� �����
            await _viewModel.UpdateTotalAmount();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("������", $"��������� ������: {ex.Message}", "OK");
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

    // === ����� ���������� ��� ������� ������ "�������� �����" ===
    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        try
        {
            // �������� ID ������������
            var userIdString = await SecureStorage.GetAsync("user_id");
            if (!int.TryParse(userIdString, out int userId))
            {
                await Shell.Current.DisplayAlert("������", "������������ �� �����������.", "OK");
                return;
            }

            // �������� ������ �� �������
            var cartItems = await _cartRepository.GetCartItemsAsync(userId);
            if (cartItems == null || !cartItems.Any())
            {
                await Shell.Current.DisplayAlert("������", "������� �����.", "OK");
                return;
            }

            // ��������� ������� ������� ������ �� ������
            foreach (var item in cartItems)
            {
                var product = await _productsRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    await Shell.Current.DisplayAlert("������",
                        $"������������ ������ '{product?.Name ?? "����������� �����"}'", "OK");
                    return;
                }
            }

            // ����� ����� ������� ����� �������� ������
            await Shell.Current.DisplayAlert("�����", "����� �������� �������!", "OK");

            // ������� ������� ����� ����������
            foreach (var item in cartItems)
            {
                await _cartRepository.RemoveFromCartAsync(item.CartItemId);
            }

            // ������� �� �������� ������������� ��� �������
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("������", $"�� ������� �������� �����: {ex.Message}", "OK");
        }
    }
}
