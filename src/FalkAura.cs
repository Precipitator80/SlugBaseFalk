using UnityEngine;
using System.Collections.Generic;

namespace SlugBaseFalk
{
    public class FalkAura
    {
        /// <summary>
        /// Creates an Aura and switches it on.
        /// </summary>
        /// <param name="player">The player to create the aura for.</param>
        public FalkAura(Player player, Color glowColour)
        {
            this.player = player;
            auraRoom = player.room;
            flakes = new List<PlayerFlake>();
            this.glowColour = glowColour;
            SwitchAuraState();
        }

        public bool active; // Boolean indicating whether the aura is on or off.
        private Player player; // The player the aura belongs to.
        private List<PlayerFlake> flakes; // List to allow for spawning of flakes around the player.
        private Room auraRoom; // Aura room reference to update the aura correctly when the player enters a new room.
        private Color glowColour; // The colour to use for the aura's glow.

        /// <summary>
        /// Switches the aura state and player glow correspondingly.
        /// </summary>
        public void SwitchAuraState()
        {
            // Switch the state of the aura and player glow.
            active = !active;
            player.glowing = active;

            PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
            if (playerGraphics.lightSource != null)
            {
                // Add the player's light to the room if not already contained.
                if (!player.room.lightSources.Contains(playerGraphics.lightSource))
                {
                    playerGraphics.player.room.AddObject(playerGraphics.lightSource);
                }

                // Adjust the light's colour so that it is correct when switched off.
                playerGraphics.lightSource.color = active ? glowColour : Color.black;
            }
        }

        /// <summary>
        /// Update method to spawn flakes and keep the correct aura colour.
        /// </summary>
        public void Update()
        {
            if (active)
            {
                if (!auraRoom.BeingViewed || flakes.Count == 0)
                {
                    for (int i = 0; i < flakes.Count; i++)
                    {
                        flakes[i].Destroy();
                    }
                    flakes = new List<PlayerFlake>();
                    for (int i = 0; i < 10; i++)
                    {
                        PlayerFlake playerFlake = new PlayerFlake(player, this);
                        flakes.Add(playerFlake);
                        player.room.AddObject(playerFlake);
                        playerFlake.active = true;
                        playerFlake.PlaceRandomlyAroundPlayer();
                        playerFlake.savedCamPos = player.room.game.cameras[0].currentCameraPosition;
                        playerFlake.reset = false;
                    }
                    auraRoom = player.room;
                }

                // Set the player glow to the correct colour when the aura is active.
                PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
                if (playerGraphics.lightSource != null)
                {
                    playerGraphics.lightSource.color = glowColour;
                }
            }
        }
    }
}