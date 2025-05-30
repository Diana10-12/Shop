using ProcurementApp;
using Microsoft.EntityFrameworkCore;
using ProcurementApp.Data;
using ProcurementApp.Data.Repositories;
using ProcurementApp.ViewModels.Auth;
using ProcurementApp.Views.Auth;
using ProcurementApp.Services;
using ProcurementApp.ViewModels.Buyer;
using ProcurementApp.Views.Buyer;
using ProcurementApp.ViewModels.Seller;
using ProcurementApp.Views.Seller;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Добавляем HttpClient с именем "WeatherClient" и таймаутом 15 секунд
        builder.Services.AddHttpClient("WeatherClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Регистрация сервисов базы данных
        var connectionString = "Host=192.168.237.233;Database=Shop;Username=postgres;Password=12345";
        builder.Services.AddDbContext<DatabaseContext>(options =>
            options.UseNpgsql(connectionString));

        // Регистрация репозиториев
        builder.Services.AddSingleton<UsersRepository>(sp =>
            new UsersRepository(connectionString));

        builder.Services.AddSingleton<ProductImagesRepository>(sp =>
            new ProductImagesRepository(connectionString));

        builder.Services.AddSingleton<ProductsRepository>(sp =>
            new ProductsRepository(
                connectionString,
                sp.GetRequiredService<ProductImagesRepository>()));

        builder.Services.AddSingleton<CartRepository>(sp =>
            new CartRepository(connectionString));

        // Регистрация сервисов
        builder.Services.AddSingleton<WeatherService>(sp =>
            new WeatherService(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("WeatherClient")));

        builder.Services.AddSingleton<DeliveryCalculator>(sp =>
            new DeliveryCalculator(sp.GetRequiredService<WeatherService>()));

        builder.Services.AddSingleton<YandexMapService>();

        // Регистрация репозиториев для CheckoutViewModel
        builder.Services.AddSingleton<PurchaseOrderRepository>(sp =>
            new PurchaseOrderRepository(connectionString));
        builder.Services.AddSingleton<DeliveryRepository>(sp =>
            new DeliveryRepository(connectionString));

        // Регистрация ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddSingleton<ProductsViewModel>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<CheckoutViewModel>(sp =>
            new CheckoutViewModel(
                sp.GetRequiredService<DeliveryCalculator>(),
                sp.GetRequiredService<CartRepository>(),
                sp.GetRequiredService<PurchaseOrderRepository>(),
                sp.GetRequiredService<DeliveryRepository>(),
                sp.GetRequiredService<ProductsRepository>(),
                sp.GetRequiredService<YandexMapService>()
            ));
        builder.Services.AddTransient<ProductsManagementViewModel>();
        builder.Services.AddTransient<OrderTrackingViewModel>(sp =>
            new OrderTrackingViewModel(
                sp.GetRequiredService<PurchaseOrderRepository>()
            )
        );

        // Регистрация страниц
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<ProductsPage>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CheckoutPage>();
        builder.Services.AddTransient<OrderTrackingPage>();

        // Регистрация страниц продавца
        builder.Services.AddTransient<AddProductPage>();
        builder.Services.AddTransient<MyProductsPage>();

        return builder.Build();
    }
}
