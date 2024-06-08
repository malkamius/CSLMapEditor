using CrimsonStainedLands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper2
{
    public class ItemDisplay
    {
        public ItemTemplateData Item;

        public ItemDisplay(ItemTemplateData item)
        { this.Item = item; }

        public string Display { get { return Item.Vnum + " - " + Item.Name; } }
    }
}
