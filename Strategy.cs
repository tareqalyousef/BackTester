using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTester
{
    public interface IStrategy
    {
        void Update(Account account);
        void ProcessOrder(Account account, Order order, double price);
        void ProcessBoxOrder(Account account, BoxOrder order, double price);
    }
}