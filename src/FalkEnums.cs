using SlugBase.DataTypes;

namespace SlugBaseFalk
{
    public static class FalkEnums
    {
        public static SlugcatStats.Name Falk = new("Falk");

        public static class Color
        {
            public static PlayerColor Body;
            public static PlayerColor Eyes;
            public static PlayerColor Gills;
            public static PlayerColor Diamonds;
            public static PlayerColor Aura;
        }

        public static void RegisterValues()
        {
            Color.Body = new PlayerColor("Body");
            Color.Eyes = new PlayerColor("Eyes");
            Color.Gills = new PlayerColor("Gills");
            Color.Diamonds = new PlayerColor("Diamonds");
            Color.Aura = new PlayerColor("Aura");
        }
    }
}