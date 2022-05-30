using Microsoft.EntityFrameworkCore;
using Ordering.Application.Contracts.Persistance;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Repository
{
    public  class OrderRepository : RepositoryBase<Order>,IOrderRepository
    {
        public OrderRepository(OrderContext context):base(context)
        {

        }

        public async Task<IEnumerable<Order>> GetOrderByUserName(string userName)
        {
            var orderList = await _dbContext.Orders.Where(o => o.UserName == userName).ToListAsync();
            return orderList;
        }
    }
}
