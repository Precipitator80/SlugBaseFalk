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
            savedCamPos = -1;
            ResetMe();
        }

        public void PlaceRandomlyAroundPlayer()
        {
            ResetMe();
            pos = player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-100f, 100f, Random.value), Mathf.Lerp(-100f, 100f, Random.value));
            lastPos = pos;
        }

        public override void Update(bool eu)
        {
            if (!active && !falkAura.active)
            {
                savedCamPos = -1;
                return;
            }
            base.Update(eu);
            vel *= 0.82f;
            vel.y = vel.y - 0.25f;
            vel += RWCustom.Custom.DegToVec(180f + Mathf.Lerp(-45f, 45f, Random.value)) * 0.1f;
            vel += RWCustom.Custom.DegToVec(rot + velRotAdd + yRot) * Mathf.Lerp(0.1f, 0.25f, Random.value);
            if (room.GetTile(pos).Solid && room.GetTile(lastPos).Solid)
            {
                reset = true;
            }
            if (reset)
            {
                float radius = 75f;
                pos = player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-radius, radius, Random.value), Mathf.Lerp(-radius, radius, Random.value));
                lastPos = pos;
                ResetMe();
                reset = false;
                vel *= 0f;
                active = falkAura.active && !player.inShortcut;
                return;
            }
            if (pos.x < room.game.cameras[0].pos.x - 20f)
            {
                reset = true;
            }
            if (pos.x > room.game.cameras[0].pos.x + 1366f + 20f)
            {
                reset = true;
            }
            if (pos.y < room.game.cameras[0].pos.y - 200f)
            {
                reset = true;
            }
            if (pos.y > room.game.cameras[0].pos.y + 768f + 200f)
            {
                reset = true;
            }
            if (room.game.cameras[0].currentCameraPosition != savedCamPos)
            {
                PlaceRandomlyAroundPlayer();
                savedCamPos = room.game.cameras[0].currentCameraPosition;
            }
            if (!room.BeingViewed)
            {
                Destroy();
            }
            lastRot = rot;
            rot += rotSpeed;
            rotSpeed = Mathf.Clamp(rotSpeed + Mathf.Lerp(-1f, 1f, Random.value) / 30f, -10f, 10f);
            lastYRot = yRot;
            yRot += yRotSpeed;
            yRotSpeed = Mathf.Clamp(yRotSpeed + Mathf.Lerp(-1f, 1f, Random.value) / 320f, -0.05f, 0.05f);
        }

        public void ResetMe()
        {
            velRotAdd = Random.value * 360f;
            vel = Custom.RNV();
            scale = Random.value;
            rot = Random.value * 360f;
            lastRot = rot;
            rotSpeed = Mathf.Lerp(2f, 10f, Random.value) * ((Random.value < 0.5f) ? (-1f) : 1f);
            yRot = Random.value * 3.1415927f;
            lastYRot = yRot;
            yRotSpeed = Mathf.Lerp(0.02f, 0.05f, Random.value) * ((Random.value < 0.5f) ? (-1f) : 1f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Pebble" + Random.Range(1, 15).ToString(), true);
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].isVisible = active && !reset;
            if (!active)
            {
                return;
            }
            float num = Mathf.InverseLerp(-1f, 1f, Vector2.Dot(Custom.DegToVec(45f), Custom.DegToVec(Mathf.Lerp(lastYRot, yRot, timeStacker) * 57.29578f + Mathf.Lerp(lastRot, rot, timeStacker))));
            float ghostMode = rCam.ghostMode;
            Color color = Custom.HSL2RGB(0.08611111f, 0.65f, Mathf.Lerp(0.53f, 0f, ghostMode));
            Color color2 = Custom.HSL2RGB(0.08611111f, Mathf.Lerp(1f, 0.65f, ghostMode), Mathf.Lerp(1f, 0.53f, ghostMode));
            sLeaser.sprites[0].color = Color.Lerp(color, color2, num);
            sLeaser.sprites[0].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[0].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
            sLeaser.sprites[0].scaleX = Mathf.Lerp(0.25f, 0.45f, scale) * Mathf.Sin(Mathf.Lerp(lastYRot, yRot, timeStacker) * 3.1415927f);
            sLeaser.sprites[0].scaleY = Mathf.Lerp(0.35f, 0.65f, scale);
            sLeaser.sprites[0].rotation = Mathf.Lerp(lastRot, rot, timeStacker);
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