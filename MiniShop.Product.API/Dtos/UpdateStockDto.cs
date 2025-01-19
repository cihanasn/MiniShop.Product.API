namespace MiniShop.Product.API.Dtos;

public record UpdateStockDto(Guid ProductId, int Quantity);
