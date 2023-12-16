namespace SlugBaseFalk
{
    public class FalkShield
    {
        /// <summary>
        /// Constructor to get or initialise a threat tracker for the player to use for the anxiety mechanic.
        /// </summary>
        /// <param name="player">The player to give a shield to.</param>
        public FalkShield(Player player)
        {
            if (player.playerState.playerNumber == 0)
            {
                if (player.abstractCreature.world.game.manager.fallbackThreatDetermination == null)
                {
                    player.abstractCreature.world.game.manager.fallbackThreatDetermination = new ThreatDetermination(player.playerState.playerNumber);
                }
                threatTracker = player.abstractCreature.world.game.manager.fallbackThreatDetermination;
            }
            else
            {
                threatTracker = new ThreatDetermination(player.playerState.playerNumber);
            }
        }

        private bool active = false; // Boolean indicating whether the shield is on or off.
        ThreatDetermination threatTracker; // Threat tracker to use for the anxiety mechanic.

        /// <summary>
        /// Property to get the current threat.
        /// </summary>
        private float Threat
        {
            get
            {
                return threatTracker.currentMusicAgnosticThreat;
            }
        }

        /// <summary>
        /// Update method to give the shield functionality.
        /// </summary>
        public void Update()
        {

        }

        /// <summary>
        /// Method to have shield graphics.
        /// </summary>
        /// <param name="sLeaser">The current room's sprite leaser.</param>
        public void DisruptorDrawSprites(RoomCamera.SpriteLeaser sLeaser)
        {
            // Get the shield sprite.
            FSprite disruptor = sLeaser.sprites[sLeaser.sprites.Length - 1];

            // Make the shield follow the player and update its size depending on whether the shield is active or not.
            disruptor.x = sLeaser.sprites[9].x;
            disruptor.y = sLeaser.sprites[9].y;
            disruptor.rotation = sLeaser.sprites[9].rotation;
            disruptor.scale = active ? 8f : 0.01f;
        }
    }
}