using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Estimates;
using Nop.Core.Events;
using Nop.Data;
using Nop.Services.Events;
using Nop.Services.Estimates;
using Nop.Core.Caching;
using Nop.Core.Domain.Orders;
using Nop.Services.Orders;

namespace Nop.Services.Estimates
{
    public partial class EstimateService: IEstimateService
    {
        #region Fields
        private readonly IRepository<Estimate> _estimateRepository;
        private readonly IRepository<EstimateItem> _estimateItemRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        private readonly IRepository<Product> _productRepository;

        private readonly IRepository<Customer> _customerRepository;
        private readonly IEventPublisher _eventPublisher;
        #endregion

        #region Ctor
        public EstimateService(IRepository<Estimate> estimateRepository,
                                 IRepository<EstimateItem> estimateItemRepository,
                                 IRepository<Product> productRepository,
                                 IRepository<Customer> customerRepository,
                                 IEventPublisher eventPublisher,
                                 IStaticCacheManager staticCacheManager)
        {
            _estimateRepository = estimateRepository;
            _estimateItemRepository = estimateItemRepository;
            _productRepository = productRepository;
            _customerRepository = customerRepository;
            _eventPublisher = eventPublisher;
            _staticCacheManager = staticCacheManager;
        }
        #endregion

        #region Methods
        public virtual async Task<Estimate> GetEstimateById(int estimateID)
        {
            if (estimateID == 0)
                return null;

            return await _estimateRepository.GetByIdAsync(estimateID);
        }

        public virtual IList<Estimate> GetEstimateByCustomerNumber(int CustId = 0)
        {
            if (CustId == 0)
                return null;
            var query = _estimateRepository.Table;
            query = query.Where(a => a.CustomerId == CustId && a.Delete != true);
            query = query.OrderByDescending(a => a.CreatedOnUtc);
            return query.ToList();
        }

        public virtual async Task DeleteEstimate(Estimate estimate)
        {
            if (estimate == null)
                throw new ArgumentNullException("estimate");

            estimate.Delete = true;
            await UpdateEstimate(estimate);

            //event notification
            await _eventPublisher.EntityDeletedAsync(estimate);
        }

        public virtual async Task UpdateEstimate(Estimate estimate)
        {
            if (estimate == null)
                throw new ArgumentNullException("estimate");

            await _estimateRepository.UpdateAsync(estimate);

            //event notification
            await _eventPublisher.EntityUpdatedAsync(estimate);
        }


        //public virtual IPagedList<Estimate> SearchEstimate(int storeId = 0,
        //int customerId = 0,
        //int productId = 0,
        //DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        // int pageIndex = 0, int pageSize = int.MaxValue)
        //{
        //    var query = _estimateRepository.Table;
        //    if (storeId > 0)
        //        query = query.Where(o => o.StoreId == storeId);
        //    if (customerId > 0)
        //        query = query.Where(o => o.CustomerId == customerId);
        //    if (productId > 0)
        //    {
        //        query = query
        //            .Where(o => o.EstimateItems
        //            .Any(orderItem => orderItem.Product.Id == productId));
        //    }
        //    if (createdFromUtc.HasValue)
        //        query = query.Where(o => createdFromUtc.Value <= o.CreatedOnUtc);
        //    if (createdToUtc.HasValue)
        //        query = query.Where(o => createdToUtc.Value >= o.CreatedOnUtc);

        //    query = query.Where(o => !o.Delete);
        //    query = query.OrderByDescending(o => o.CreatedOnUtc);
        //    return new PagedList<Estimate>(query, pageIndex, pageSize);
        //}

        public virtual async Task InsertEstimate(Estimate estimate)
        {
            if (estimate == null)
                throw new ArgumentNullException("order");

            await _estimateRepository.InsertAsync(estimate);

            //event notification
            await _eventPublisher.EntityInsertedAsync(estimate);
        }
        public virtual async Task InsertEstimateItem(EstimateItem estimateItem)
        {
            if (estimateItem == null)
                throw new ArgumentNullException("order");

            await _estimateItemRepository.InsertAsync(estimateItem);

            //event notification
            await _eventPublisher.EntityInsertedAsync(estimateItem);
        }


        public virtual async Task UpdateEstimateItem(EstimateItem etimate)
        {
            if (etimate == null)
                throw new ArgumentNullException("order");

            await _estimateItemRepository.UpdateAsync(etimate);

            //event notification
            await _eventPublisher.EntityUpdatedAsync(etimate);
        }
        public virtual async Task<EstimateItem> GetEstimateItemById(int estimateItemId)
        {
            if (estimateItemId == 0)
                return null;

            return await _estimateItemRepository.GetByIdAsync(estimateItemId, cache => default);
        }
        public virtual EstimateItem GetEstimateItemByEstimateId(int estimateId)
        {
            if (estimateId == 0)
                return null;
            var query = _estimateItemRepository.Table;
            query = query.Where(a => a.EstimateId == estimateId);

            var item = query.FirstOrDefault();
            return item;
        }

        public virtual IList<EstimateItem> ProductExistOrNot(int estimateId, int productid = 0)
        {
            if (estimateId == 0 || productid == 0)
                return null;

            var query = _estimateItemRepository.Table;
            query = query.Where(a => a.EstimateId == estimateId);
            query = query.Where(a => a.ProductId == productid);

            return query.ToList();
        }

        public virtual async Task DeleteEstimateItem(EstimateItem estimateItem)
        {
            if (estimateItem == null)
                throw new ArgumentNullException("orderItem");
            await _estimateItemRepository.DeleteAsync(estimateItem);
            //event notification
            await _eventPublisher.EntityDeletedAsync(estimateItem);
        }

        public virtual ICollection<EstimateItem> ForUpdateEstimatesItems(int EstimateId)
        {
            return _estimateItemRepository.Table.Where(a => a.EstimateId == EstimateId).ToList();
        }

        public virtual async Task UpdateEstimateItemUsingFor(int itemId = 0, int Quntity = 0)
        {
            var shoppingCartItem = GetEstimateItemById(itemId).Result;
            if (shoppingCartItem != null)
            {
                if (Quntity > 0)
                {
                    shoppingCartItem.Quantity = Quntity;
                    await UpdateEstimateItem(shoppingCartItem);
                }
                else
                {
                    //delete a shopping cart item
                    await DeleteEstimateItem(shoppingCartItem);
                }
            }
        }
        // Added by Yogesh Kumbhar on Dt: 12/10/2024
        public virtual async Task<IList<EstimateItem>> GetEstimateItemsAsync(Customer customer, int EstimateId, int storeId = 0, int? productId = null)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var items = _estimateItemRepository.Table.Where(sci => sci.CustomerId == customer.Id);

            //filter shopping cart items by store
            if (storeId > 0)
                items = items.Where(item => item.StoreId == storeId);

            return  await items.ToListAsync();
        }
        public virtual async Task<IList<EstimateItem>> GetEstimateItemsByEstimateIdAsync(int EstimateId)
        {
            var items = _estimateItemRepository.Table.Where(sci => sci.EstimateId == EstimateId);

            return await items.ToListAsync();
        }
        #endregion
    }
}
