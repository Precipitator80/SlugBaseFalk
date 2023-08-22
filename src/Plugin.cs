using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using IL.ScavengerCosmetic;

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
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
        }

        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
            Debug.Log("Loading atlas.");
            FAtlas atlas = Futile.atlasManager.LoadAtlas(string.Concat("atlases/", TAIL_SPRITE_NAME));
            Debug.Log("Adding atlas.");
            Futile.atlasManager.AddAtlas(atlas);
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

        private static bool _lockApplyPalette = false;
        private static bool _loggedMissingSprite = false;
        private static readonly string TAIL_SPRITE_NAME = "falkTail";
        public void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            // Work-around to avoid calling this hook multiple times
            if (_lockApplyPalette)
            {
                orig(self, sLeaser, rCam, palette);
                return;
            }

            _lockApplyPalette = true;
            orig(self, sLeaser, rCam, palette);
            _lockApplyPalette = false;

            // Edit the tail sprite
            // Note that, while it is a mesh, meshes inherit from FSprite, so it's still a sprite :)
            TriangleMesh tail = null;
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (sLeaser.sprites[i] is TriangleMesh tm)
                {
                    tail = tm;
                    break;
                }
            }

            if (tail != null)
            {
                // Set the tail's element to a custom sprite
                try
                {
                    Debug.Log("Getting element from atlas.");
                    tail.element = Futile.atlasManager.GetElementWithName(TAIL_SPRITE_NAME);
                    Debug.Log(TAIL_SPRITE_NAME + " loaded successfully.");
                }
                catch (FutileException e)
                {
                    if (!_loggedMissingSprite)
                    {
                        _loggedMissingSprite = true;
                        Debug.Log(TAIL_SPRITE_NAME + " failed to load!");
                        Debug.LogError(new Exception($"Tail sprite \"{TAIL_SPRITE_NAME}\" not found. Defaulting to \"Futile_White\". Further errors will not be logged.", e));
                    }
                    tail.element = Futile.atlasManager.GetElementWithName("Futile_White");
                }

                // Register that the tail must have custom colors
                if (tail.verticeColors == null || tail.verticeColors.Length != tail.vertices.Length)
                {
                    tail.verticeColors = new Color[tail.vertices.Length];
                }

                tail.customColor = true;

                // Use the player's color when the given color is exactly black
                Color fromColor = Color.white;
                Color toColor = Color.white;

                // Calculate UVs and colors
                for (int i = tail.verticeColors.Length - 1; i >= 0; i--)
                {
                    float perc = i / 2 / (float)(tail.verticeColors.Length / 2);
                    tail.verticeColors[i] = Color.Lerp(fromColor, toColor, perc);
                    Vector2 uv;
                    if (i % 2 == 0)
                        uv = new Vector2(perc, 0f);
                    else if (i < tail.verticeColors.Length - 1)
                        uv = new Vector2(perc, 1f);
                    else
                        uv = new Vector2(1f, 0f);

                    // Map UV values to the element
                    uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
                    uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

                    tail.UVvertices[i] = uv;
                }
                tail.Refresh();
            }
        }
    }
}
