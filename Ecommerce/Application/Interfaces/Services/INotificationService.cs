using Domain.Entities;
using System.Threading;

namespace Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task SendOrderConfirmationAsync(Order order);
        Task SendPointsExpiryWarningAsync(Guid userId, int points, DateTime expiryDate);
    }
}
