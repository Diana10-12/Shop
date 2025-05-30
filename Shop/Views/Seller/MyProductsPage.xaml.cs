using ProcurementApp.ViewModels.Seller;

namespace ProcurementApp.Views.Seller;

public partial class MyProductsPage : ContentPage
{
    private readonly ProductsManagementViewModel _viewModel;

    public MyProductsPage(ProductsManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Products.Count == 0)
        {
            await _viewModel.LoadProductsAsync();
        }
    }
}
