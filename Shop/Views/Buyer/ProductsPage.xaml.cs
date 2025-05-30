using ProcurementApp.ViewModels.Buyer;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace ProcurementApp.Views.Buyer
{
    public partial class ProductsPage : ContentPage
    {
        private readonly ProductsViewModel _viewModel;

        public ProductsPage(ProductsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                // Загружаем данные корзины
                await _viewModel.LoadCartQuantities();

                // Обновляем список товаров
                await _viewModel.LoadProductsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить товары: {ex.Message}", "OK");
            }

            // Подписываемся на сообщение об очистке корзины
            MessagingCenter.Subscribe<CheckoutViewModel>(this, "CartCleared", async (sender) =>
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _viewModel.ResetProductQuantities();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Отписываемся от события при уходе со страницы
            MessagingCenter.Unsubscribe<CheckoutViewModel>(this, "CartCleared");
        }
    }
}
