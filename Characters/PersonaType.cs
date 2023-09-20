namespace RainMeadow
{
    internal class PersonaType : ExtEnum<PersonaType>
    {
        public PersonaType(string value, bool register = false) : base(value, register) { }

        public static PersonaType Slugcat = new("Slugcat", true);
        public static PersonaType Squidcicada = new("Squidcicada", true);
    }
}