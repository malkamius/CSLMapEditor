using CrimsonStainedLands;

namespace CLSMapper
{
    internal class LearnedDisplay
    {
        public SkillSpell spell;
        public int learned;
        public LearnedDisplay(SkillSpell spell, int learned)
        {
            this.spell = spell;
            this.learned = learned;
        }

        public string Display
        {
            get
            {
                return spell.name + " - " + learned;
            }
        }
    }
}