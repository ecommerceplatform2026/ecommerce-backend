using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task SendOrderConfirmationAsync(Order order);
    }
}
