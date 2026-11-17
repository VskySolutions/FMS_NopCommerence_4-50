using Nop.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial interface IHomePageArrivals
    {
        //void InsertHomePageArrival(HomePageArrivals product);

        /// <summary>
        /// Updates the product
        /// </summary>
        /// <param name="product">Product</param>
        //void UpdateHomePageArrival(HomePageArrivals product);


        /// <summary>

        /// Updates the products
        /// </summary>
        /// <param name="products">Product</param>
        //void UpdateHomePageArrival(IList<HomePageArrivals> products);

        //HomePageArrivals GetArrivalById(int Id);


        //void DeleteHomePageArrival(HomePageArrivals product);

        /// <summary>
        /// Delete products
        /// </summary>
        /// <param name="products">Products</param>

        //HomePageArrivals Edit(int Id);
        IList<HomePageArrivals> HomePageList();


        //IPagedList<HomePageArrivals> SearchArrival(
        // int pageIndex = 0,
        // int pageSize = int.MaxValue,
        // bool showHidden = false,
        // bool? overridePublished = null);
    }
}
