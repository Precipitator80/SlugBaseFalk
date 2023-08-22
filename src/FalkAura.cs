using UnityEngine;
using System.Collections.Generic;

namespace SlugBaseFalk
{
    class FalkAura
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
}