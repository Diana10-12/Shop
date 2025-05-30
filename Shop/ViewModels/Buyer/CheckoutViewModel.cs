using ProcurementApp.Data.Models;
using ProcurementApp.Data.Repositories;
using ProcurementApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;

namespace ProcurementApp.ViewModels.Buyer
{
    public partial class CheckoutViewModel : INotifyPropertyChanged
    {
        // 🛠️ Сервисы
        private readonly DeliveryCalculator _deliveryCalculator;
        private readonly CartRepository _cartRepository;
        private readonly PurchaseOrderRepository _orderRepository;
        private readonly DeliveryRepository _deliveryRepository;
        private readonly ProductsRepository _productsRepository;
        private readonly YandexMapService _mapService;

        // 📍 Поля
        private string _address;
        private decimal _deliveryCost;
        private int _estimatedDays;
        private string _weatherImpact;
        private decimal _totalAmount;
        private double _latitude;
        private double _longitude;
        private bool _isCalculating;

        // 🗺️ HTML-карта
        private HtmlWebViewSource _mapHtmlSource;
        public HtmlWebViewSource MapHtmlSource
        {
            get => _mapHtmlSource;
            set { SetProperty(ref _mapHtmlSource, value); }
        }

        // 📦 Свойства
        public string Address
        {
            get => _address;
            set { SetProperty(ref _address, value); }
        }

        public decimal DeliveryCost
        {
            get => _deliveryCost;
            private set
            {
                if (_deliveryCost != value)
                {
                    _deliveryCost = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalWithDelivery)); // Обновляем TotalWithDelivery
                }
            }
        }

        public int EstimatedDays
        {
            get => _estimatedDays;
            private set
            {
                if (_estimatedDays != value)
                {
                    _estimatedDays = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WeatherImpact
        {
            get => _weatherImpact;
            private set
            {
                if (_weatherImpact != value)
                {
                    _weatherImpact = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set { SetProperty(ref _totalAmount, value); OnPropertyChanged(nameof(TotalWithDelivery)); }
        }

        public decimal TotalWithDelivery => TotalAmount + DeliveryCost;

        public bool IsCalculating
        {
            get => _isCalculating;
            set
            {
                SetProperty(ref _isCalculating, value);
                OnPropertyChanged(nameof(IsNotCalculating));
                OnPropertyChanged(nameof(DeliveryButtonText));
            }
        }

        public bool IsNotCalculating => !IsCalculating;

        public string DeliveryButtonText => IsCalculating ? "Расчет..." : "Рассчитать доставку";

        // 🧾 Команды
        public ICommand CalculateDeliveryCommand { get; }
        public ICommand PlaceOrderCommand { get; }
        public ICommand GetLocationCommand { get; }
        public ICommand SearchAddressCommand { get; }

        public int UserId { get; set; }

        // 🛠️ Конструктор
        public CheckoutViewModel(
            DeliveryCalculator deliveryCalculator,
            CartRepository cartRepository,
            PurchaseOrderRepository orderRepository,
            DeliveryRepository deliveryRepository,
            ProductsRepository productsRepository,
            YandexMapService mapService)
        {
            _deliveryCalculator = deliveryCalculator;
            _cartRepository = cartRepository;
            _orderRepository = orderRepository;
            _deliveryRepository = deliveryRepository;
            _productsRepository = productsRepository;
            _mapService = mapService;
            TotalAmount = 0;

            // Инициализация команд
            CalculateDeliveryCommand = new Command(async () => await CalculateDeliveryAsync());
            PlaceOrderCommand = new Command(async () => await PlaceOrderAsync());
            GetLocationCommand = new Command(async () => await GetCurrentLocation());
            SearchAddressCommand = new Command(async () => await SearchAddress());

            // Установка начального значения
            IsCalculating = false;
        }

        // 📦 Основные методы
        public void SetOrderParameters(decimal totalAmount, int userId)
        {
            TotalAmount = totalAmount;
            UserId = userId;
        }

        /// <summary>
        /// Обновляет общую сумму заказа на основе данных корзины.
        /// </summary>
        public async Task UpdateTotalAmount()
        {
            var cartItems = await _cartRepository.GetCartItemsAsync(UserId);
            TotalAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);
        }

        // 🌍 Геолокация и карта
        public async Task GetCurrentLocation()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await Shell.Current.DisplayAlert("Ошибка", "Необходимо разрешение на геолокацию", "OK");
                        return;
                    }
                }
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(30)
                });
                if (location == null)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Не удалось получить положение", "OK");
                    return;
                }
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                Address = await _mapService.GetAddressAsync(_latitude, _longitude);
                UpdateMap();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось получить местоположение: {ex.Message}", "OK");
            }
        }

        private void UpdateMap()
        {
            try
            {
                if (_latitude != 0 && _longitude != 0)
                {
                    var html = _mapService.GenerateMapHtml(_latitude, _longitude);
                    // Обновление UI должно быть в главном потоке
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MapHtmlSource = new HtmlWebViewSource { Html = html };
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления карты: {ex}");
            }
        }

        private async Task SearchAddress()
        {
            if (string.IsNullOrWhiteSpace(Address))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите адрес для поиска", "OK");
                return;
            }
            try
            {
                var (lat, lon) = await _mapService.GetCoordinatesAsync(Address);
                _latitude = lat;
                _longitude = lon;
                UpdateMap();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Адрес не найден: {ex.Message}", "OK");
            }
        }

        // 🚚 Расчёт доставки
        private async Task CalculateDeliveryAsync()
        {
            if (IsCalculating) return;
            IsCalculating = true;

            try
            {
                if (string.IsNullOrWhiteSpace(Address))
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Введите адрес доставки", "OK");
                    return;
                }

                // Получаем координаты по адресу
                var coords = await _mapService.GetCoordinatesAsync(Address);
                _latitude = coords.Latitude;
                _longitude = coords.Longitude;

                UpdateMap(); // Обновляем карту на экране

                // Вызываем расчет доставки
                var (cost, days, impact) = await _deliveryCalculator.CalculateDeliveryAsync(
                    _latitude,
                    _longitude,
                    basePrice: 300,
                    baseDays: 2);

                // Обновляем UI
                DeliveryCost = cost;
                EstimatedDays = days;
                WeatherImpact = impact;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расчёта доставки: {ex.Message}");
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось рассчитать доставку: {ex.Message}", "OK");
            }
            finally
            {
                IsCalculating = false;
            }
        }

        // 📦 Оформление заказа
        private async Task PlaceOrderAsync()
        {
            try
            {
                if (UserId <= 0)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Ошибка пользователя. Попробуйте перезайти.", "OK");
                    return;
                }

                // Получаем актуальные данные корзины
                var cartItems = await _cartRepository.GetCartItemsAsync(UserId);
                if (!cartItems.Any())
                {
                    await Shell.Current.DisplayAlert("Внимание", "Ваша корзина пуста.", "OK");
                    return;
                }

                foreach (var item in cartItems)
                {
                    var product = await _productsRepository.GetProductByIdAsync(item.ProductId);
                    if (product == null)
                    {
                        await Shell.Current.DisplayAlert("Ошибка", $"Товар с ID {item.ProductId} больше не доступен.", "OK");
                        return;
                    }
                    if (product.StockQuantity < item.Quantity)
                    {
                        await Shell.Current.DisplayAlert(
                            "Ошибка",
                            $"Недостаточно товара '{product.Name}'.\nЗапрошено: {item.Quantity}\nДоступно: {product.StockQuantity}",
                            "OK");
                        return;
                    }
                }

                // Пересчитываем сумму на основе актуальных данных корзины
                await UpdateTotalAmount();

                // Проверка рассчитанной доставки
                if (EstimatedDays <= 0)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Пожалуйста, рассчитайте доставку перед оформлением заказа.", "OK");
                    return;
                }

                // Рассчитываем примерную дату доставки
                var estimatedDeliveryDate = DateTime.UtcNow.AddDays(EstimatedDays);

                // Передаем дату доставки при создании заказа
                var orderId = await _orderRepository.CreateOrderAsync(
                    UserId,
                    TotalWithDelivery,
                    estimatedDeliveryDate);

                // Обновляем остатки товаров
                foreach (var item in cartItems)
                {
                    await _productsRepository.UpdateProductQuantityAsync(
                        item.ProductId,
                        -item.Quantity);
                }

                // Очищаем корзину
                await _cartRepository.ClearCartAsync(UserId);

                // Уведомляем об очистке корзины
                MessagingCenter.Send(this, "CartCleared");

                await Shell.Current.DisplayAlert("Успех", $"Заказ #{orderId} оформлен.", "OK");
                await Shell.Current.GoToAsync("//Buyer/OrderTrackingPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось оформить заказ: {ex.Message}", "OK");
            }
        }

        // 📡 INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
