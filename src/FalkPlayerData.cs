using System;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SlugBaseFalk
{
    public class FalkPlayerData
    {
        public readonly bool IsFalk;

        public WeakReference<Player> playerRef;

        public Color BodyColor;
        public Color EyesColor;
        public Color GillsColor;
        public Color DiamondColor;

        public FAtlas TailAtlas;

        public FalkAura falkAura;

        public FalkPlayerData(Player player)
        {
            IsFalk = player.slugcatStats.name == FalkEnums.Falk;

            playerRef = new WeakReference<Player>(player);

            if (!IsFalk)
            {
                return;
            }
        }

        ~FalkPlayerData()
        {
            try
            {
                TailAtlas.Unload();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LoadTailAtlas()
        {
            var tailTexture = new Texture2D(Plugin.TailAtlasTemplate.texture.width, Plugin.TailAtlasTemplate.texture.height, TextureFormat.ARGB32, false);
            Graphics.CopyTexture(Plugin.TailAtlasTemplate.texture, tailTexture);

            Utils.MapTextureColor(tailTexture, 255, DiamondColor, false);
            Utils.MapTextureColor(tailTexture, 0, BodyColor);

            if (playerRef.TryGetTarget(out var player))
            {
                TailAtlas = Futile.atlasManager.LoadAtlasFromTexture("falktailtexture_" + player.playerState.playerNumber + Time.time + Random.value, tailTexture, false);
            }
        }

        public void SetupColors(PlayerGraphics pg)
        {
            BodyColor = pg.GetColor(FalkEnums.Color.Body) ?? Custom.hexToColor("466B78");
            EyesColor = pg.GetColor(FalkEnums.Color.Eyes) ?? Custom.hexToColor("FFF8A3");
            GillsColor = pg.GetColor(FalkEnums.Color.Gills) ?? EyesColor;
            DiamondColor = pg.GetColor(FalkEnums.Color.Diamonds) ?? EyesColor;
        }
    }
}