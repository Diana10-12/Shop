using ProcurementApp.Data.Models;
using ProcurementApp.Data.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using System.Globalization;

namespace ProcurementApp.ViewModels.Buyer;

public class CartViewModel : INotifyPropertyChanged
{
    private readonly CartRepository _cartRepository;
    private readonly ProductsRepository _productsRepository; // Добавлено

    public ObservableCollection<CartItem> CartItems { get; } = new();
    public decimal TotalPrice => CartItems.Sum(item => item.Product.Price * item.Quantity);

    public ICommand CheckoutCommand { get; }

    private int _userId;

    public CartViewModel(CartRepository cartRepository, ProductsRepository productsRepository)
    {
        _cartRepository = cartRepository;
        _productsRepository = productsRepository;

        LoadCartItemsCommand = new Command(async () => await LoadCartItemsAsync());
        RemoveItemCommand = new Command<CartItem>(async (item) => await RemoveItemAsync(item));
        IncreaseQuantityCommand = new Command<CartItem>(async (item) => await ChangeQuantityAsync(item, 1));
        DecreaseQuantityCommand = new Command<CartItem>(async (item) => await ChangeQuantityAsync(item, -1));
        CheckoutCommand = new Command(async () => await GoToCheckoutAsync());

        // Подписываемся на событие очистки корзины
        MessagingCenter.Subscribe<CheckoutViewModel>(this, "CartCleared", (sender) =>
        {
            ClearCart();
        });
    }

    public Command LoadCartItemsCommand { get; }
    public Command<CartItem> RemoveItemCommand { get; }
    public Command<CartItem> IncreaseQuantityCommand { get; }
    public Command<CartItem> DecreaseQuantityCommand { get; }

    public int UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            OnPropertyChanged();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadCartItemsAsync()
    {
        if (IsBusy || UserId == 0)
            return;

        try
        {
            IsBusy = true;
            CartItems.Clear();

            var items = await _cartRepository.GetCartItemsAsync(UserId);
            foreach (var item in items)
            {
                CartItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalPrice));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить корзину: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // === Метод для перехода на страницу оформления заказа ===
    public async Task GoToCheckoutAsync()
    {
        if (CartItems.Count == 0) return;

        var totalPrice = CartItems.Sum(item => item.Product.Price * item.Quantity);
        await Shell.Current.GoToAsync($"CheckoutPage?TotalAmount={totalPrice}&UserId={UserId}");
    }

    // === Метод изменения количества товара в корзине ===
    private async Task ChangeQuantityAsync(CartItem item, int delta)
    {
        try
        {
            var newQuantity = item.Quantity + delta;

            if (newQuantity <= 0)
            {
                await RemoveItemAsync(item);
                return;
            }

            var product = await _productsRepository.GetProductByIdAsync(item.ProductId);
            if (product == null || product.StockQuantity < newQuantity)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Недостаточно товара на складе", "OK");
                return;
            }

            var success = await _cartRepository.UpdateCartItemQuantityAsync(item.CartItemId, newQuantity);

            if (success)
            {
                await _productsRepository.UpdateProductQuantityAsync(item.ProductId, delta); // Обновляем количество на складе

                var newItem = new CartItem
                {
                    CartItemId = item.CartItemId,
                    UserId = item.UserId,
                    ProductId = item.ProductId,
                    Quantity = newQuantity,
                    Product = item.Product,
                    AddedAt = item.AddedAt
                };

                var index = CartItems.IndexOf(item);
                if (index != -1)
                {
                    CartItems[index] = newItem;
                }

                // Уведомление об изменении общей суммы и цены конкретного товара
                OnPropertyChanged(nameof(TotalPrice));
                item.OnPropertyChanged(nameof(item.TotalItemPrice)); // <<== Вызов уведомления для TotalItemPrice
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Ошибка при изменении количества: {ex.Message}", "OK");
        }

        OnPropertyChanged(nameof(TotalPrice));
    }

    private async Task RemoveItemAsync(CartItem item)
    {
        try
        {
            var success = await _cartRepository.RemoveFromCartAsync(item.CartItemId);
            if (success)
            {
                CartItems.Remove(item);
                OnPropertyChanged(nameof(TotalPrice));

                var productsVm = App.Current?.Handler?.MauiContext?.Services.GetService<ProductsViewModel>();
                productsVm?.RemoveProductQuantity(item.ProductId);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить товар: {ex.Message}", "OK");
        }
    }

    // <<< Новый метод — очищает корзину
    public void ClearCart()
    {
        CartItems.Clear();
        OnPropertyChanged(nameof(TotalPrice));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
