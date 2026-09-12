using System;
using UnityEngine;

namespace CMS.Core.Data.Base
{
    public abstract class ModelBase
    {
        public string userId;
        public int stage;
    }

    public abstract class CollectionDataBase : ICloneable
    {
        public virtual int Stage { get; set; }
        public virtual Texture2D Thumbnail { get; set; }
        public virtual bool IsSelected { get; set; }
        public virtual object Clone() => null;
    }
}

namespace CMS.Template.UI.Popup.Collection.Base
{
    // Intentionally empty to avoid ambiguity when both namespaces are imported
}
