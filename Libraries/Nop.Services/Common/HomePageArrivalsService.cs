using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Events;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial class HomePageArrivalsService : IHomePageArrivals
    {
        #region Fields
        private readonly IRepository<HomePageArrivals> _HomePageArrivalsRepository;
        private readonly IEventPublisher _eventPublisher;
        //private readonly DistributedCacheManager _cacheManager;
        #endregion

        #region Ctor
        public HomePageArrivalsService(
            IRepository<HomePageArrivals> productRepository,
              IEventPublisher eventPublisher
         )
        {
            _HomePageArrivalsRepository = productRepository;
            _eventPublisher = eventPublisher;
            //_cacheManager = cacheManager;
        }
        #endregion


        #region Methods
        public IList<HomePageArrivals> HomePageList()
        {
            var query = _HomePageArrivalsRepository.Table;
            query = query.Where(p => !p.Delete && p.HomeDisplayFlag == true);
            query = query.OrderBy(p => p.DisplayOrder);

            return query.ToList();
        }
        #endregion
    }
}
