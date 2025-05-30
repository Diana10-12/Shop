using System.Globalization;

namespace ProcurementApp.Converters;

public class DeliveryDateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime deliveryDate)
        {
            var today = DateTime.Today;

            if (deliveryDate.Date < today)
                return Colors.Red; //  Просрочено
            if (deliveryDate.Date <= today.AddDays(2))
                return Colors.Orange; //  Скоро
            return Colors.Green; //  В срок
        }
        return Colors.Black; //  По умолчанию
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}