using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;

namespace Nop.Core.Domain.VideoGallarys
{
    public partial class VideoGallary : BaseEntity, ILocalizedEntity, ISlugSupported
    {
        public string VideoTitle { get; set; }
        public string VideoUrl { get; set; }
        public string VideoTumbnailImage { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool Publish { get; set; }

        public bool VideoDelete { get; set; }
    }
}
