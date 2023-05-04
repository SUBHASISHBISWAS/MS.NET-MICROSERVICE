using Shopping.Aggregator.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    public interface ICatalogService
    {
        Task<IEnumerable<CatalogModel>> GetCatalog();

        Task<IEnumerable<CatalogModel>> GetCatelogByCategory(string categoty);

        Task<CatalogModel> GetCatelog(string id);

    }
}
