using CrimsonStainedLands;

namespace Mapper2
{
    internal class SpellDisplay
    {
        public ItemSpellData spell;

        public SpellDisplay(ItemSpellData spell)
        {
            this.spell = spell;
        }

        public string Display
        {
            get
            {
                return spell.SpellName + " - " + spell.Level.ToString();
            }
        }
    }
}