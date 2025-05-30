using ProcurementApp.ViewModels.Seller;

namespace ProcurementApp.Views.Seller;

public partial class AddProductPage : ContentPage
{
    private readonly ProductsManagementViewModel _viewModel;

    public AddProductPage(ProductsManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeSellerId();
    }
}
