using System;
using System.Runtime.CompilerServices;
using SlugBase.DataTypes;
using UnityEngine;

namespace SlugBaseFalk
{
    public static class PlayerExtension
    {
        private static readonly ConditionalWeakTable<Player, FalkPlayerData> _cwt = new();

        public static FalkPlayerData Falk(this Player player) => _cwt.GetValue(player, _ => new FalkPlayerData(player));

        public static Color? GetColor(this PlayerGraphics pg, PlayerColor color) => color.GetColor(pg);

        public static Color? GetColor(this Player player, PlayerColor color) => (player.graphicsModule as PlayerGraphics)?.GetColor(color);

        public static Player Get(this WeakReference<Player> weakRef)
        {
            weakRef.TryGetTarget(out var result);
            return result;
        }

        public static PlayerGraphics PlayerGraphics(this Player player) => (PlayerGraphics)player.graphicsModule;

        public static TailSegment[] Tail(this Player player) => player.PlayerGraphics().tail;

        public static bool IsFalk(this Player player) => player.Falk().IsFalk;

        public static bool IsFalk(this Player player, out FalkPlayerData falk)
        {
            falk = player.Falk();
            return falk.IsFalk;
        }
    }
}