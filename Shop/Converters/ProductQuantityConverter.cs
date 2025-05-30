using System.Globalization;
using ProcurementApp.ViewModels.Buyer;
using ProcurementApp.Data.Models;

namespace ProcurementApp.Converters;

public class ProductQuantityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Product product)
        {
            var vm = App.Current?.Handler?.MauiContext?.Services.GetService<ProductsViewModel>();
            if (vm != null && vm.ProductQuantities.TryGetValue(product.ProductId, out int qty))
            {
                return qty.ToString();
            }
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
