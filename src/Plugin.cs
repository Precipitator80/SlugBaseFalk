using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;

namespace SlugBaseFalk
{
    [BepInPlugin(MOD_ID, "SlugBase Falk", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "precipitator.slugbasefalk";

        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Update += HookPlayerUpdate;
            On.PlayerGraphics.AddToContainer += HookPlayerGraphicsAddToContainer;
            On.PlayerGraphics.DrawSprites += HookPlayerGraphicsDrawSprites;
            On.PlayerGraphics.InitiateSprites += HookPlayerGraphicsInitiateSprites;
            On.PlayerGraphics.Update += HookPlayerGraphicsUpdate;
        }

        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PlayerGraphics, FalkSpriteStartIndices> _falkSprites = new();
        FalkAura falkAura;
        private static readonly Color baseColor = new Color(0.27451f, 0.41961f, 0.47059f);
        private static readonly Color effectColor = new Color(1f, 0.97255f, 0.63922f);

        private class FalkSpriteStartIndices
        {
            public int shield;
        }

        private void HookPlayerUpdate(On.Player.orig_Update orig, Player player, bool eu)
        {
            if (falkAura == null)
            {
                falkAura = new FalkAura(player);
            }
            if (player.input[0].mp && !player.input[1].mp)
            {
                for (int i = 2; i < player.input.Length; i++)
                {
                    if (player.input[i].mp)
                    {
                        falkAura.SwitchAuraState();
                        break;
                    }
                }
            }
            falkAura.Update();
            orig.Invoke(player, eu);
        }

        private void HookPlayerGraphicsInitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam);

            var newSprites = _falkSprites.GetValue(playerGraphics, _ => new());

            playerGraphics.gills = new PlayerGraphics.AxolotlGills(playerGraphics, sLeaser.sprites.Length);

            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + playerGraphics.gills.numberOfSprites); // 1 for shield.

            playerGraphics.gills.InitiateSprites(sLeaser, rCam);

            int shieldIndex = sLeaser.sprites.Length - 1;
            newSprites.shield = shieldIndex;
            sLeaser.sprites[shieldIndex] = new FSprite("Futile_White", true)
            {
                shader = rCam.game.rainWorld.Shaders["GhostDistortion"],
                alpha = 0.2f
            };

            playerGraphics.AddToContainer(sLeaser, rCam, null);
        }

        private void HookPlayerGraphicsAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, newContatiner);

            if (_falkSprites.TryGetValue(playerGraphics, out var newSprites) && newSprites.shield < sLeaser.sprites.Length)
            {
                playerGraphics.gills.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
                rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[newSprites.shield]);
            }
        }

        public void HookPlayerGraphicsDrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, timeStacker, camPos);
            if (!rCam.room.game.DEBUGMODE)
            {
                playerGraphics.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                playerGraphics.gills.SetGillColors(baseColor, effectColor);
            }
            falkAura?.DisruptorDrawSprites(sLeaser);
        }

        public void HookPlayerGraphicsUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics playerGraphics)
        {
            orig.Invoke(playerGraphics);
            if (playerGraphics.player.room != null)
            {
                playerGraphics.gills.Update();
            }
        }
    }
}