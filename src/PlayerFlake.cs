using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SlugBaseFalk
{
    class PlayerFlake : CosmeticSprite
    {
        Player player;
        FalkAura falkAura;
        public PlayerFlake(Player player, FalkAura falkAura)
        {
            this.player = player;
            this.falkAura = falkAura;
            this.savedCamPos = -1;
            this.ResetMe();
        }

        public void PlaceRandomlyAroundPlayer()
        {
            this.ResetMe();
            this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-100f, 100f, Random.value), Mathf.Lerp(-100f, 100f, Random.value));
            this.lastPos = this.pos;
        }

        public override void Update(bool eu)
        {
            if (!this.active && !falkAura.auraActive)
            {
                this.savedCamPos = -1;
                return;
            }
            base.Update(eu);
            this.vel *= 0.82f;
            this.vel.y = this.vel.y - 0.25f;
            this.vel += RWCustom.Custom.DegToVec(180f + Mathf.Lerp(-45f, 45f, Random.value)) * 0.1f;
            this.vel += RWCustom.Custom.DegToVec(this.rot + this.velRotAdd + this.yRot) * Mathf.Lerp(0.1f, 0.25f, Random.value);
            if (this.room.GetTile(this.pos).Solid && this.room.GetTile(this.lastPos).Solid)
            {
                this.reset = true;
            }
            if (this.reset)
            {
                float radius = 75f;
                this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-radius, radius, Random.value), Mathf.Lerp(-radius, radius, Random.value));
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
            this.rotSpeed = Mathf.Clamp(this.rotSpeed + Mathf.Lerp(-1f, 1f, Random.value) / 30f, -10f, 10f);
            this.lastYRot = this.yRot;
            this.yRot += this.yRotSpeed;
            this.yRotSpeed = Mathf.Clamp(this.yRotSpeed + Mathf.Lerp(-1f, 1f, Random.value) / 320f, -0.05f, 0.05f);
        }

        public void ResetMe()
        {
            this.velRotAdd = Random.value * 360f;
            this.vel = Custom.RNV();
            this.scale = Random.value;
            this.rot = Random.value * 360f;
            this.lastRot = this.rot;
            this.rotSpeed = Mathf.Lerp(2f, 10f, Random.value) * ((Random.value < 0.5f) ? (-1f) : 1f);
            this.yRot = Random.value * 3.1415927f;
            this.lastYRot = this.yRot;
            this.yRotSpeed = Mathf.Lerp(0.02f, 0.05f, Random.value) * ((Random.value < 0.5f) ? (-1f) : 1f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Pebble" + Random.Range(1, 15).ToString(), true);
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].isVisible = this.active && !this.reset;
            if (!this.active)
            {
                return;
            }
            float num = Mathf.InverseLerp(-1f, 1f, Vector2.Dot(Custom.DegToVec(45f), Custom.DegToVec(Mathf.Lerp(this.lastYRot, this.yRot, timeStacker) * 57.29578f + Mathf.Lerp(this.lastRot, this.rot, timeStacker))));
            float ghostMode = rCam.ghostMode;
            Color color = Custom.HSL2RGB(0.08611111f, 0.65f, Mathf.Lerp(0.53f, 0f, ghostMode));
            Color color2 = Custom.HSL2RGB(0.08611111f, Mathf.Lerp(1f, 0.65f, ghostMode), Mathf.Lerp(1f, 0.53f, ghostMode));
            sLeaser.sprites[0].color = Color.Lerp(color, color2, num);
            sLeaser.sprites[0].x = Mathf.Lerp(this.lastPos.x, this.pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(this.lastPos.y, this.pos.y, timeStacker) - camPos.y;
            sLeaser.sprites[0].scaleX = Mathf.Lerp(0.25f, 0.45f, this.scale) * Mathf.Sin(Mathf.Lerp(this.lastYRot, this.yRot, timeStacker) * 3.1415927f);
            sLeaser.sprites[0].scaleY = Mathf.Lerp(0.35f, 0.65f, this.scale);
            sLeaser.sprites[0].rotation = Mathf.Lerp(this.lastRot, this.rot, timeStacker);
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private float scale;
        private float rot;
        private float lastRot;
        private float yRot;
        private float lastYRot;
        private float rotSpeed;
        private float yRotSpeed;
        private float velRotAdd;
        public int savedCamPos;
        public bool reset;
        public bool active;
    }
}