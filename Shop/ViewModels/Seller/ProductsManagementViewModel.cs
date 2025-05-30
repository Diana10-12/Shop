using ProcurementApp.Data.Models;
using ProcurementApp.Data.Repositories;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using System.Diagnostics;

namespace ProcurementApp.ViewModels.Seller;

public class ProductsManagementViewModel : INotifyPropertyChanged
{
    private readonly ProductsRepository _productsRepository;
    private readonly ProductImagesRepository _imagesRepository;
    private string _newProductName;
    private string _newProductDescription;
    private decimal _newProductPrice;
    private int _newProductQuantity;
    private int _sellerId;
    private bool _isBusy;
    private ObservableCollection<string> _newProductImages = new();

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<string> NewProductImages
    {
        get => _newProductImages;
        set
        {
            _newProductImages = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadProductsCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand AddImageCommand { get; }
    public ICommand RemoveImageCommand { get; }
    public ICommand PickImageCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand PickImageFromGalleryCommand { get; }
    public ICommand TakePhotoCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }

    public string NewProductName
    {
        get => _newProductName;
        set
        {
            _newProductName = value;
            OnPropertyChanged();
        }
    }

    public string NewProductDescription
    {
        get => _newProductDescription;
        set
        {
            _newProductDescription = value;
            OnPropertyChanged();
        }
    }

    public decimal NewProductPrice
    {
        get => _newProductPrice;
        set
        {
            _newProductPrice = value;
            OnPropertyChanged();
        }
    }

    public int NewProductQuantity
    {
        get => _newProductQuantity;
        set
        {
            _newProductQuantity = value;
            OnPropertyChanged();
        }
    }

    public int SellerId
    {
        get => _sellerId;
        set
        {
            _sellerId = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public ProductsManagementViewModel(
        ProductsRepository productsRepository,
        ProductImagesRepository imagesRepository)
    {
        _productsRepository = productsRepository;
        _imagesRepository = imagesRepository;

        LoadProductsCommand = new Command(async () => await LoadProductsAsync());
        AddProductCommand = new Command(async () => await AddProductAsync());
        AddImageCommand = new Command<string>(async (url) => await AddImageAsync(url));
        RemoveImageCommand = new Command<string>((url) => RemoveImage(url));
        PickImageCommand = new Command(async () => await PickImageAsync());
        PickImageFromGalleryCommand = new Command(async () => await PickImageFromGalleryAsync());
        TakePhotoCommand = new Command(async () => await TakePhotoAsync());
        DeleteProductCommand = new Command<Product>(async product => await DeleteProductAsync(product));
        IncreaseQuantityCommand = new Command<Product>(async product => await ChangeQuantityAsync(product, 1));
        DecreaseQuantityCommand = new Command<Product>(async product => await ChangeQuantityAsync(product, -1));
    }

    public async Task InitializeSellerId()
    {
        try
        {
            var userIdString = await SecureStorage.Default.GetAsync("user_id");
            if (int.TryParse(userIdString, out int id))
            {
                SellerId = id;
                await LoadProductsAsync(); // Загрузка товаров после инициализации SellerId
                return;
            }
            await Shell.Current.DisplayAlert("Ошибка", "Требуется авторизация", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting user_id: {ex.Message}");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public async Task LoadProductsAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            if (SellerId == 0)
            {
                await InitializeSellerId();
                if (SellerId == 0) return;
            }
            Products.Clear();
            var products = await _productsRepository.GetSellerProductsAsync(SellerId);
            foreach (var product in products)
            {
                if (_imagesRepository != null)
                {
                    product.Images = await _imagesRepository.GetImagesForProductAsync(product.ProductId);
                }
                Products.Add(product);
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить товары: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddImageAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Введите URL изображения", "OK");
            return;
        }
        NewProductImages.Add(imageUrl);
        OnPropertyChanged(nameof(NewProductImages));
    }

    private async Task PickImageAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите изображение товара",
                FileTypes = FilePickerFileType.Images
            });
            if (result != null)
            {
                NewProductImages.Add(result.FullPath);
                OnPropertyChanged(nameof(NewProductImages));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
        }
    }

    private void RemoveImage(string imageUrl)
    {
        NewProductImages.Remove(imageUrl);
        OnPropertyChanged(nameof(NewProductImages));
    }

    private async Task AddProductAsync()
    {
        try
        {
            if (SellerId == 0)
            {
                await InitializeSellerId();
                if (SellerId == 0)
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Не удалось определить продавца", "OK");
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(NewProductName))
            {
                await Shell.Current.DisplayAlert("Ошибка", "Введите название товара", "OK");
                return;
            }
            if (NewProductPrice <= 0)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Цена должна быть больше 0", "OK");
                return;
            }
            if (NewProductQuantity < 0)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Количество не может быть отрицательным", "OK");
                return;
            }
            var product = new Product
            {
                Name = NewProductName,
                Description = NewProductDescription,
                Price = NewProductPrice,
                SellerId = SellerId,
                StockQuantity = NewProductQuantity
            };
            var productId = await _productsRepository.AddProductAsync(product);
            if (productId > 0)
            {
                product.ProductId = productId;
                if (NewProductImages.Any())
                {
                    foreach (var imageUrl in NewProductImages)
                    {
                        await _imagesRepository.AddImageAsync(
                            productId,
                            imageUrl,
                            imageUrl == NewProductImages.FirstOrDefault());
                    }
                }
                await LoadProductsAsync();
                ClearForm();
                await Shell.Current.DisplayAlert("Успех", "Товар успешно добавлен", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при добавлении товара: {ex}");
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось добавить товар: {ex.Message}", "OK");
        }
    }

    private async Task DeleteProductAsync(Product product)
    {
        try
        {
            bool confirm = await Shell.Current.DisplayAlert(
               "Подтверждение",
               $"Вы уверены, что хотите удалить товар {product.Name}?",
               "Да", "Нет");
            if (!confirm) return;
            var success = await _productsRepository.DeleteProductAsync(product.ProductId);
            if (success)
            {
                Products.Remove(product);
                await Shell.Current.DisplayAlert("Успех", "Товар удален", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось удалить товар", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить товар: {ex.Message}", "OK");
        }
    }

    private async Task ChangeQuantityAsync(Product product, int delta)
    {
        if (product == null) return;
        try
        {
            product.StockQuantity += delta;
            if (product.StockQuantity < 0)
            {
                product.StockQuantity -= delta;
                await Shell.Current.DisplayAlert("Ошибка", "Количество не может быть меньше нуля", "OK");
                return;
            }
            await _productsRepository.UpdateProductQuantityAsync(product.ProductId, product.StockQuantity);
            OnPropertyChanged(nameof(Products));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось изменить количество: {ex.Message}", "OK");
        }
    }

    private async Task PickImageFromGalleryAsync()
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Выберите изображение товара"
            });
            if (result != null)
            {
                NewProductImages.Add(result.FullPath);
                OnPropertyChanged(nameof(NewProductImages));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось выбрать изображение: {ex.Message}", "OK");
        }
    }

    private async Task TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.IsCaptureSupported)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Фотосъемка не поддерживается на этом устройстве", "OK");
                return;
            }
            var result = await MediaPicker.CapturePhotoAsync();
            if (result != null)
            {
                NewProductImages.Add(result.FullPath);
                OnPropertyChanged(nameof(NewProductImages));
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось сделать фото: {ex.Message}", "OK");
        }
    }

    private void ClearForm()
    {
        NewProductName = string.Empty;
        NewProductDescription = string.Empty;
        NewProductPrice = 0;
        NewProductQuantity = 0;
        NewProductImages.Clear();
        OnPropertyChanged(nameof(NewProductImages));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
