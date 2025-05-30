using ProcurementApp.Data.Models;
using ProcurementApp.Data.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace ProcurementApp.ViewModels.Buyer
{
    public class ProductsViewModel : INotifyPropertyChanged
    {
        private readonly ProductsRepository _productsRepository;
        private readonly CartRepository _cartRepository;
        // Кэш для хранения количества товаров в корзине
        private Dictionary<int, int> _productQuantities = new();
        // Публичное свойство для доступа из конвертеров
        public Dictionary<int, int> ProductQuantities => _productQuantities;

        public ObservableCollection<Product> Products { get; } = new();

        private int _userId;

        public ProductsViewModel(ProductsRepository productsRepository, CartRepository cartRepository)
        {
            _productsRepository = productsRepository;
            _cartRepository = cartRepository;

            LoadProductsCommand = new Command(async () => await LoadProductsAsync());
            AddToCartCommand = new Command<Product>(async (product) => await AddToCartAsync(product));
            DecreaseQuantityCommand = new Command<Product>(async (product) => await DecreaseQuantityAsync(product));

            Task.Run(async () => await InitializeUserId());

            // Подписываемся на сообщение об очистке корзины
            MessagingCenter.Subscribe<CheckoutViewModel>(this, "CartCleared", async (sender) =>
            {
                // Очищаем локальный кэш количеств
                _productQuantities.Clear();

                // Обновляем UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadProductsAsync();
                });
            });
        }

        public void RefreshProducts()
        {
            OnPropertyChanged(nameof(Products));
        }

        private async Task InitializeUserId()
        {
            var userIdString = await SecureStorage.Default.GetAsync("user_id");
            if (int.TryParse(userIdString, out int userId))
            {
                UserId = userId;
                await LoadCartQuantities(); // <<< Добавлено: загрузка данных корзины
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Пользователь не авторизован", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        public Command LoadProductsCommand { get; }
        public Command<Product> AddToCartCommand { get; }
        public Command<Product> DecreaseQuantityCommand { get; }

        public int UserId
        {
            get => _userId;
            set
            {
                if (_userId != value)
                {
                    _userId = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadProductsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                var allProducts = await _productsRepository.GetProductsAsync();
                var filteredProducts = allProducts
                    .Where(p => p.StockQuantity > 0)
                    .ToList();
                Products.Clear();
                foreach (var product in filteredProducts)
                {
                    Products.Add(product);
                }
                await LoadCartQuantities(); // <<< Обновление корзины при загрузке товаров
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить товары: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // <<< Новый метод — загружает количество товаров в корзине
        public async Task LoadCartQuantities()
        {
            if (UserId == 0) return;
            try
            {
                var cartItems = await _cartRepository.GetCartItemsAsync(UserId);
                foreach (var item in cartItems)
                {
                    _productQuantities[item.ProductId] = item.Quantity;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить корзину: {ex.Message}", "OK");
            }
        }

        private async Task AddToCartAsync(Product product)
        {
            try
            {
                if (UserId == 0)
                {
                    await InitializeUserId();
                    if (UserId == 0) return;
                }
                var success = await _cartRepository.AddToCartAsync(UserId, product.ProductId, 1);
                if (success)
                {
                    var newQuantity = (_productQuantities.TryGetValue(product.ProductId, out int qty) ? qty : 0) + 1;
                    UpdateQuantityForProduct(product.ProductId, newQuantity);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task DecreaseQuantityAsync(Product product)
        {
            try
            {
                if (UserId == 0)
                {
                    await InitializeUserId();
                    if (UserId == 0) return;
                }
                var currentQuantity = await GetProductQuantityInCart(product.ProductId);
                if (currentQuantity <= 0)
                {
                    await Shell.Current.DisplayAlert("Информация", "В корзине нет товара.", "OK");
                    return;
                }
                var newQuantity = currentQuantity - 1;
                bool success;
                if (newQuantity <= 0)
                {
                    success = await _cartRepository.RemoveFromCartByProductAsync(UserId, product.ProductId);
                }
                else
                {
                    success = await _cartRepository.UpdateCartItemQuantityAsync(UserId, product.ProductId, newQuantity);
                }
                if (success)
                {
                    _productQuantities[product.ProductId] = newQuantity;
                    UpdateQuantityForProduct(product.ProductId, newQuantity);
                    await Shell.Current.DisplayAlert("Успешно", "Количество товара уменьшено", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Не удалось уменьшить количество товара", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось уменьшить количество: {ex.Message}", "OK");
            }
        }

        public async Task<int> GetProductQuantityInCart(int productId)
        {
            if (UserId == 0) return 0;
            if (_productQuantities.TryGetValue(productId, out var quantity))
                return quantity;
            var quantityInCart = await _cartRepository.GetCartItemQuantityAsync(UserId, productId);
            _productQuantities[productId] = quantityInCart ?? 0;
            return _productQuantities[productId];
        }

        public async Task RefreshProductQuantities()
        {
            var updatedProducts = await _productsRepository.GetProductsAsync();
            foreach (var updatedProduct in updatedProducts)
            {
                var existingProduct = Products.FirstOrDefault(p => p.ProductId == updatedProduct.ProductId);
                if (existingProduct != null)
                {
                    if (updatedProduct.StockQuantity <= 0)
                    {
                        Products.Remove(existingProduct); // Удаляем товар с нулевым остатком
                    }
                    else if (existingProduct.StockQuantity != updatedProduct.StockQuantity)
                    {
                        var index = Products.IndexOf(existingProduct);
                        Products[index] = updatedProduct; // Обновляем количество
                    }
                }
                else if (updatedProduct.StockQuantity > 0)
                {
                    Products.Add(updatedProduct); // Добавляем товар при пополнении
                }
            }
        }

        public void UpdateQuantityForProduct(int productId, int newQuantity)
        {
            if (_productQuantities.ContainsKey(productId))
            {
                _productQuantities[productId] = newQuantity;
            }
            else
            {
                _productQuantities.Add(productId, newQuantity);
            }
            var product = Products.FirstOrDefault(p => p.ProductId == productId);
            if (product != null)
            {
                var index = Products.IndexOf(product);
                Products[index] = product.CloneWithQuantity(newQuantity);
            }
        }

        public void RemoveProductQuantity(int productId)
        {
            if (_productQuantities.ContainsKey(productId))
            {
                _productQuantities.Remove(productId);
                var product = Products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    var index = Products.IndexOf(product);
                    Products[index] = product.CloneWithQuantity(0);
                }
            }
        }

        // Метод для сброса всех количеств
        public void ResetProductQuantities()
        {
            _productQuantities.Clear();
            foreach (var product in Products.ToList())
            {
                product.Quantity = 0;
                var index = Products.IndexOf(product);
                Products[index] = product;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
