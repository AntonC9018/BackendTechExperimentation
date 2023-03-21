using Microsoft.EntityFrameworkCore;

namespace efcore_transactions;

public class ServiceA
{
    private readonly DbContext _data;
    
    public ServiceA(DbContext data)
    {
        _data = data;
    }
}