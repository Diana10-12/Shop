using ProcurementApp.ViewModels.Buyer;
using Microsoft.Maui.Controls;

namespace ProcurementApp.Views.Buyer
{
    public partial class OrderTrackingPage : ContentPage
    {
        private readonly OrderTrackingViewModel _viewModel;

        public OrderTrackingPage(OrderTrackingViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // «агружаем список заказов пользовател€
            if (!_viewModel.IsBusy)
            {
                await _viewModel.LoadOrders();
            }
        }
    }
}