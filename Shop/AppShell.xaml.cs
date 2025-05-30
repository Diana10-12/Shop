using ProcurementApp.Views.Auth;
using ProcurementApp.Views.Buyer;
using ProcurementApp.Views.Seller;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using ProcurementApp.ViewModels.Buyer;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ProcurementApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Явная регистрация всех маршрутов
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegistrationPage), typeof(RegistrationPage));
            Routing.RegisterRoute(nameof(ProductsPage), typeof(ProductsPage));
            Routing.RegisterRoute(nameof(CartPage), typeof(CartPage));
            Routing.RegisterRoute("CheckoutPage", typeof(CheckoutPage));
            Routing.RegisterRoute("OrderTrackingPage", typeof(OrderTrackingPage));

            // Подписка на события навигации
            this.Navigating += OnNavigating;
            this.Navigated += OnNavigated;
        }

        private async void OnNavigating(object sender, ShellNavigatingEventArgs e)
        {
            var targetRoute = e.Target.Location.OriginalString;

            // Проверка доступа к защищенным разделам
            if (targetRoute.Contains("Buyer") || targetRoute.Contains("Seller"))
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    e.Cancel();
                    await GoToAsync($"//{nameof(LoginPage)}");
                    return;
                }
            }

            var routes = new[] { "CheckoutPage" };

            if (routes.Any(r => e.Target.Location.OriginalString.Contains(r)))
            {
                var userId = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrEmpty(userId))
                {
                    e.Cancel();
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }

            // Дополнительная проверка для CheckoutPage
            if (e.Target.Location.OriginalString.Contains("CheckoutPage"))
            {
                var auth = await SecureStorage.GetAsync("auth_token");
                if (string.IsNullOrEmpty(auth))
                {
                    e.Cancel();
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // Обработка параметров для CheckoutPage
            if (args.Target.Location.OriginalString.StartsWith("//CheckoutPage"))
            {
                var parameters = args.Target.Location.OriginalString.Split('?').Skip(1).FirstOrDefault();
                if (!string.IsNullOrEmpty(parameters))
                {
                    var queryParams = ParseQueryParameters(parameters);
                    var checkoutVM = App.Current?.Handler?.MauiContext?.Services.GetService<CheckoutViewModel>();
                    if (checkoutVM != null)
                    {
                        if (queryParams.TryGetValue("TotalAmount", out var totalAmount))
                            checkoutVM.TotalAmount = decimal.Parse(totalAmount, CultureInfo.InvariantCulture);

                        if (queryParams.TryGetValue("UserId", out var userId))
                            checkoutVM.UserId = int.Parse(userId);
                    }
                }
            }

            // УДАЛЕНО: обработка параметров для OrderTrackingPage больше не нужна
        }

        private Dictionary<string, string> ParseQueryParameters(string query)
        {
            return query.Split('&')
                .Select(p => p.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    pair => Uri.UnescapeDataString(pair[0]),
                    pair => Uri.UnescapeDataString(pair[1]),
                    StringComparer.OrdinalIgnoreCase);
        }

        private async void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            // Проверяем, если текущая страница содержит CartPage
            if (e.Current?.Location.ToString().Contains("CartPage") == true)
            {
                // Получаем CartViewModel из контейнера зависимостей
                var cartVM = App.Current.Handler?.MauiContext?.Services.GetService<CartViewModel>();
                if (cartVM != null)
                {
                    await cartVM.LoadCartItemsAsync(); // Загружаем элементы корзины
                }
            }

            // Проверяем, если текущая страница — ProductsPage
            if (e.Current?.Location.ToString().Contains("ProductsPage") == true)
            {
                var productsVM = App.Current.Handler?.MauiContext?.Services.GetService<ProductsViewModel>();
                if (productsVM != null)
                {
                    await productsVM.LoadCartQuantities();
                }
            }

            // ИЗМЕНЕНО: обработка для OrderTrackingPage
            if (e.Current?.Location.ToString().Contains("OrderTrackingPage") == true)
            {
                // Получаем ViewModel для отслеживания заказов
                var trackingVM = Handler.MauiContext.Services.GetService<OrderTrackingViewModel>();
                if (trackingVM != null)
                {
                    // Просто вызываем загрузку заказов
                    await trackingVM.LoadOrders();
                }
            }
        }
    }
}