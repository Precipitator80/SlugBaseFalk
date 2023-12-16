using System;
using BepInEx;
using UnityEngine;
using MonoMod.RuntimeDetour;

namespace SlugBaseFalk
{
    [BepInPlugin(MOD_ID, "SlugBase Falk", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "precipitator.slugbasefalk";

        /// <summary>
        /// Add hooks.
        /// </summary>
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Update += HookPlayerUpdate;
            On.PlayerGraphics.AddToContainer += HookPlayerGraphicsAddToContainer;
            On.PlayerGraphics.DrawSprites += HookPlayerGraphicsDrawSprites;
            On.PlayerGraphics.InitiateSprites += HookPlayerGraphicsInitiateSprites;
            On.PlayerGraphics.Update += HookPlayerGraphicsUpdate;
            On.WaterNut.Update += HookWaterNutUpdate;
            On.Player.UpdateMSC += HookPlayerUpdateMSC;
            On.PlayerGraphics.AxolotlGills.SetGillColors += HookPlayerGraphicsAxolotlGillsSetGillColors;

            On.PlayerGraphics.ctor += PlayerGraphics_ctor;

            new Hook(typeof(Player).GetProperty(nameof(Player.isRivulet)).GetGetMethod(), HookPlayerget_isRivulet);
        }

        /// <summary>
        /// Load any resources such as sprites and sounds.
        /// </summary>
        /// <param name="rainWorld">The game instance.</param>
        private void LoadResources(RainWorld rainWorld)
        {
            FalkEnums.RegisterValues();

            TailAtlasTemplate = Futile.atlasManager.LoadAtlas(string.Concat("atlases/", TAIL_SPRITE_NAME));
            Debug.Log("Loaded tail successfully.");
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PlayerGraphics, FalkSpriteStartIndices> _falkSprites = new();
        public static FAtlas TailAtlasTemplate;
        private static readonly string TAIL_SPRITE_NAME = "falkTail";

        private class FalkSpriteStartIndices
        {
            public int shield;
        }

        private void HookPlayerUpdate(On.Player.orig_Update orig, Player player, bool eu)
        {
            // Run the original code and check whether the player is a Falk slugcat.
            orig.Invoke(player, eu);
            if (!player.IsFalk(out var falk))
            {
                return;
            }

            // Create a new aura if required and observe input from the player(s), followed by an update.
            falk.falkAura ??= new FalkAura(player, falk.AuraColor);
            if (player.input[0].mp && !player.input[1].mp)
            {
                for (int i = 2; i < player.input.Length; i++)
                {
                    if (player.input[i].mp)
                    {
                        falk.falkAura.SwitchAuraState();
                        break;
                    }
                }
            }
            falk.falkAura.Update();

            // Create a new shield if required and run the update function.
            falk.falkShield ??= new FalkShield(player);
            falk.falkShield.Update();
        }

        private void HookPlayerGraphicsInitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam);

            if (!playerGraphics.player.IsFalk(out FalkPlayerData falk))
            {
                return;
            }

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

            if (sLeaser.sprites[2] is TriangleMesh tail && falk.TailAtlas.elements != null && falk.TailAtlas.elements.Count > 0)
            {
                tail.element = falk.TailAtlas.elements[0];
                for (var i = tail.vertices.Length - 1; i >= 0; i--)
                {
                    var perc = i / 2 / (float)(tail.vertices.Length / 2);
                    //tail.verticeColors[i] = Color.Lerp(fromColor, toColor, perc);
                    Vector2 uv;
                    if (i % 2 == 0)
                        uv = new Vector2(perc, 0f);
                    else if (i < tail.vertices.Length - 1)
                        uv = new Vector2(perc, 1f);
                    else
                        uv = new Vector2(1f, 0f);

                    // Map UV values to the element
                    uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
                    uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

                    tail.UVvertices[i] = uv;
                }
            }

            playerGraphics.AddToContainer(sLeaser, rCam, null);
        }

        private void HookPlayerGraphicsAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, newContatiner);

            if (!playerGraphics.player.IsFalk())
            {
                return;
            }

            if (_falkSprites.TryGetValue(playerGraphics, out var newSprites) && newSprites.shield < sLeaser.sprites.Length)
            {
                playerGraphics.gills.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
                rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[newSprites.shield]);
            }
        }

        public void HookPlayerGraphicsDrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, timeStacker, camPos);

            if (!playerGraphics.player.IsFalk(out var falk))
            {
                return;
            }

            if (!rCam.room.game.DEBUGMODE && playerGraphics.player.room != null)
            {
                sLeaser.sprites[2].color = Color.white;

                playerGraphics.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);

                falk.falkShield?.DisruptorDrawSprites(sLeaser);
            }
        }

        public void HookPlayerGraphicsUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics playerGraphics)
        {
            orig.Invoke(playerGraphics);

            if (!playerGraphics.player.IsFalk())
            {
                return;
            }

            if (playerGraphics.player.room != null)
            {
                playerGraphics.gills.Update();
            }
        }

        private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics playerGraphics, PhysicalObject ow)
        {
            orig(playerGraphics, ow);

            if (!playerGraphics.player.IsFalk(out var falk))
            {
                return;
            }

            falk.SetupColors(playerGraphics);
            falk.LoadTailAtlas();
        }

        private bool HookPlayerget_isRivulet(Func<Player, bool> orig, Player player)
        {
            return orig(player) || player.IsFalk();
        }

        private void HookWaterNutUpdate(On.WaterNut.orig_Update orig, WaterNut waterNut, bool eu)
        {
            orig.Invoke(waterNut, eu);
            if (waterNut.grabbedBy.Count > 0)
            {
                for (int i = 0; i < waterNut.grabbedBy.Count; i++)
                {
                    if (waterNut.grabbedBy[i].grabber is Player)
                    {
                        if (((Player)waterNut.grabbedBy[i].grabber).IsFalk())
                        {
                            waterNut.swellCounter--;
                            if (waterNut.swellCounter < 1)
                            {
                                waterNut.Swell();
                            }
                            return;
                        }
                    }
                }
            }
        }

        private static void HookPlayerUpdateMSC(On.Player.orig_UpdateMSC orig, Player player)
        {
            orig.Invoke(player);

            if (!player.IsFalk())
            {
                return;
            }

            if (!player.monkAscension)
            {
                player.buoyancy = 0.9f;
            }
        }

        private void HookPlayerGraphicsAxolotlGillsSetGillColors(On.PlayerGraphics.AxolotlGills.orig_SetGillColors orig, PlayerGraphics.AxolotlGills playerGraphicsAxolotlGills, Color baseCol, Color effectCol)
        {
            if (playerGraphicsAxolotlGills.pGraphics.player.IsFalk())
            {
                effectCol = SlugBase.DataTypes.PlayerColor.GetCustomColor(playerGraphicsAxolotlGills.pGraphics, "Gills");
            }
            orig.Invoke(playerGraphicsAxolotlGills, baseCol, effectCol);
        }
    }
}
