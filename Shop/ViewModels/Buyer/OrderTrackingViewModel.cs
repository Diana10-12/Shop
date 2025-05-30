using ProcurementApp.Data.Models;
using ProcurementApp.Data.Repositories;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using System.Globalization;
using ProcurementApp.Converters;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Windows.Input;

namespace ProcurementApp.ViewModels.Buyer
{
    public class OrderTrackingViewModel : INotifyPropertyChanged
    {
        private readonly PurchaseOrderRepository _orderRepo;

        private ObservableCollection<PurchaseOrder> _orders = new();
        private bool _isBusy;
        private int _selectedOrderId;
        private PurchaseOrder _selectedOrder;

        public ObservableCollection<PurchaseOrder> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public int SelectedOrderId
        {
            get => _selectedOrderId;
            set
            {
                if (SetProperty(ref _selectedOrderId, value))
                {
                    LoadOrderDetails();
                }
            }
        }

        public PurchaseOrder SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        // Вычисляемые свойства для форматированного отображения
        public string FormattedOrderDate =>
            SelectedOrder?.CreatedAt == default
                ? "не установлена"
                : SelectedOrder.CreatedAt.ToString("dd.MM.yyyy HH:mm");

        public string FormattedDeliveryDate =>
            SelectedOrder?.EstimatedDeliveryDate.HasValue == true
                ? SelectedOrder.EstimatedDeliveryDate.Value.ToString("dd.MM.yyyy")
                : "не установлена";

        public Color DeliveryDateColor
        {
            get
            {
                if (SelectedOrder?.EstimatedDeliveryDate == null)
                    return Colors.Black;

                var converter = new DeliveryDateColorConverter();
                return (Color)converter.Convert(
                    SelectedOrder.EstimatedDeliveryDate.Value,
                    typeof(Color),
                    null,
                    CultureInfo.CurrentCulture
                );
            }
        }

        public OrderTrackingViewModel(PurchaseOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
            LoadOrdersCommand = new Command(async () => await LoadOrders());
        }

        public ICommand LoadOrdersCommand { get; }

        public async Task LoadOrders()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var userIdString = await SecureStorage.Default.GetAsync("user_id");
                if (!int.TryParse(userIdString, out int userId)) return;

                Orders.Clear();
                var orders = await _orderRepo.GetOrdersByUserIdAsync(userId);
                foreach (var order in orders)
                {
                    Orders.Add(order);
                }

                // Автовыбор первого заказа, если есть
                if (Orders.Count > 0)
                {
                    SelectedOrder = Orders[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadOrderDetails()
        {
            if (SelectedOrderId <= 0) return;

            try
            {
                var order = await _orderRepo.GetOrderByIdAsync(SelectedOrderId);
                if (order != null)
                {
                    SelectedOrder = order;

                    // Обновляем вычисляемые свойства
                    OnPropertyChanged(nameof(FormattedOrderDate));
                    OnPropertyChanged(nameof(FormattedDeliveryDate));
                    OnPropertyChanged(nameof(DeliveryDateColor));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading order details: {ex.Message}");
            }
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
