using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class AlertDeliveryRepository(AppDbContext context) : Repository<AlertDelivery>(context), IAlertDeliveryRepository
{
}
