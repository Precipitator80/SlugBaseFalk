using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MoreSlugcats;
using System.Collections.Generic;

namespace SlugBaseFalk
{
    [BepInPlugin(MOD_ID, "SlugBase Falk", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "precipitator.slugbasefalk";

        //public static readonly PlayerFeature<float> SuperJump = PlayerFloat("slugbasefalk/super_jump");
        //public static readonly PlayerFeature<bool> ExplodeOnDeath = PlayerBool("slugbasefalk/explode_on_death");
        //public static readonly GameFeature<float> MeanLizards = GameFloat("slugbasefalk/mean_lizards");


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

            //On.Player.Jump += Player_Jump;
            //On.Player.Die += Player_Die;
            //On.Lizard.ctor += Lizard_ctor;
        }

        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PlayerGraphics, FalkSpriteStartIndices> _falkSprites = new();
        FalkAura falkAura;

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
            playerGraphics.gills.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));

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
                rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[newSprites.shield]);
            }
        }

        public void HookPlayerGraphicsDrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, timeStacker, camPos);
            if (!rCam.room.game.DEBUGMODE)
            {
                playerGraphics.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
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

        internal class FalkAura
        {
            public FalkAura(Player player)
            {
                this.player = player;
                flakes = new List<PlayerFlake>();
                auraRoom = player.room;
                SwitchAuraState();
            }

            public void SwitchAuraState()
            {
                auraActive = !auraActive;
                player.glowing = auraActive;
                PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
                if (playerGraphics.lightSource != null)
                {
                    if (!player.room.lightSources.Contains(playerGraphics.lightSource))
                    {
                        playerGraphics.player.room.AddObject(playerGraphics.lightSource);
                    }
                    playerGraphics.lightSource.color = auraActive ? new Color(0.258823544f, 0.5137255f, 0.796078444f) : Color.black;
                }

            }

            public void Update()
            {
                if (auraActive)
                {
                    if (!auraRoom.BeingViewed || this.flakes.Count == 0)
                    {
                        for (int i = 0; i < this.flakes.Count; i++)
                        {
                            this.flakes[i].Destroy();
                        }
                        this.flakes = new List<PlayerFlake>();
                        for (int i = 0; i < 10; i++)
                        {
                            PlayerFlake playerFlake = new PlayerFlake(player, this);
                            this.flakes.Add(playerFlake);
                            player.room.AddObject(playerFlake);
                            playerFlake.active = true;
                            playerFlake.PlaceRandomlyAroundPlayer();
                            playerFlake.savedCamPos = player.room.game.cameras[0].currentCameraPosition;
                            playerFlake.reset = false;
                        }
                        auraRoom = player.room;
                    }
                    PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
                    if (playerGraphics.lightSource != null)
                    {
                        playerGraphics.lightSource.color = auraActive ? new Color(0.258823544f, 0.5137255f, 0.796078444f) : Color.black;
                    }
                }
            }

            public void DisruptorDrawSprites(RoomCamera.SpriteLeaser sLeaser)
            {
                int shieldInt = sLeaser.sprites.Length - 1;
                sLeaser.sprites[shieldInt].x = sLeaser.sprites[9].x;
                sLeaser.sprites[shieldInt].y = sLeaser.sprites[9].y;
                sLeaser.sprites[shieldInt].rotation = sLeaser.sprites[9].rotation;
                sLeaser.sprites[shieldInt].scale = (this.auraActive && this.disruptorActive) ? 8f : 0.01f;
            }

            private List<PlayerFlake> flakes;
            private Player player;
            private Room auraRoom;
            public bool auraActive;
            private bool disruptorActive;
        }

        internal class PlayerFlake : GoldFlakes.GoldFlake
        {
            private FalkAura falkAura;
            public PlayerFlake(Player player, FalkAura falkAura)
            {
                this.player = player;
                this.falkAura = falkAura;
            }

            public override void Update(bool eu)
            {
                if (!this.active && !falkAura.auraActive)
                {
                    this.savedCamPos = -1;
                    return;
                }
                ((Action)Activator.CreateInstance(typeof(Action), this, typeof(CosmeticSprite).GetMethod("Update").MethodHandle.GetFunctionPointer()/*, eu*/))();
                this.vel *= 0.82f;
                this.vel.y = this.vel.y - 0.25f;
                this.vel += RWCustom.Custom.DegToVec(180f + Mathf.Lerp(-45f, 45f, UnityEngine.Random.value)) * 0.1f;
                this.vel += RWCustom.Custom.DegToVec(this.rot + this.velRotAdd + this.yRot) * Mathf.Lerp(0.1f, 0.25f, UnityEngine.Random.value);
                if (this.room.GetTile(this.pos).Solid && this.room.GetTile(this.lastPos).Solid)
                {
                    this.reset = true;
                }
                if (this.reset)
                {
                    float radius = 75f;
                    this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-radius, radius, UnityEngine.Random.value), Mathf.Lerp(-radius, radius, UnityEngine.Random.value));
                    this.lastPos = this.pos;
                    this.ResetMe();
                    this.reset = false;
                    this.vel *= 0f;
                    this.active = falkAura.auraActive && !player.inShortcut;
                    return;
                }
                if (this.pos.x < this.room.game.cameras[0].pos.x - 20f)
                {
                    this.reset = true;
                }
                if (this.pos.x > this.room.game.cameras[0].pos.x + 1366f + 20f)
                {
                    this.reset = true;
                }
                if (this.pos.y < this.room.game.cameras[0].pos.y - 200f)
                {
                    this.reset = true;
                }
                if (this.pos.y > this.room.game.cameras[0].pos.y + 768f + 200f)
                {
                    this.reset = true;
                }
                if (this.room.game.cameras[0].currentCameraPosition != this.savedCamPos)
                {
                    PlaceRandomlyAroundPlayer();
                    this.savedCamPos = this.room.game.cameras[0].currentCameraPosition;
                }
                if (!this.room.BeingViewed)
                {
                    this.Destroy();
                }
                this.lastRot = this.rot;
                this.rot += this.rotSpeed;
                this.rotSpeed = Mathf.Clamp(this.rotSpeed + Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) / 30f, -10f, 10f);
                this.lastYRot = this.yRot;
                this.yRot += this.yRotSpeed;
                this.yRotSpeed = Mathf.Clamp(this.yRotSpeed + Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) / 320f, -0.05f, 0.05f);
            }

            public void PlaceRandomlyAroundPlayer()
            {
                this.ResetMe();
                this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-100f, 100f, UnityEngine.Random.value), Mathf.Lerp(-100f, 100f, UnityEngine.Random.value));
                this.lastPos = this.pos;
            }

            Player player;
        }

    }
}